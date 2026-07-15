#!/usr/bin/env python3
"""Sunucu durumu + zorunlu safe deploy (senkron, canli cikti)."""
import io
import os
import sys

import paramiko

if hasattr(sys.stdout, "buffer"):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace", line_buffering=True)
if hasattr(sys.stderr, "buffer"):
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8", errors="replace", line_buffering=True)

HOST = "31.186.24.78"
USER = "huseyinadm"
PASSWORDS = ["Passwd1122%!d", "Passwd1122FFGG"]
PROJECT = "/home/huseyinadm/eticaret"
LOG = "/home/huseyinadm/deploy.log"
SCRIPT = os.path.join(os.path.dirname(__file__), "safe-deploy.sh")


def connect():
    for pwd in PASSWORDS:
        c = paramiko.SSHClient()
        c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
        try:
            c.connect(HOST, username=USER, password=pwd, timeout=30, banner_timeout=60)
            print(f"SSH OK ({USER}@{HOST})", flush=True)
            return c
        except paramiko.AuthenticationException:
            continue
    sys.exit("SSH basarisiz")


def run_out(c, cmd, timeout=120):
    print(f"\n>>> {cmd}", flush=True)
    _, stdout, stderr = c.exec_command(cmd, timeout=timeout)
    out = stdout.read().decode("utf-8", errors="replace")
    err = stderr.read().decode("utf-8", errors="replace")
    if out.strip():
        print(out, end="" if out.endswith("\n") else "\n", flush=True)
    if err.strip():
        print(err, file=sys.stderr, flush=True)
    return out.strip()


def run_stream(c, cmd, timeout=2400):
    print(f"\n>>> {cmd}", flush=True)
    _, stdout, stderr = c.exec_command(cmd, get_pty=True, timeout=timeout)
    while True:
        line = stdout.readline()
        if not line:
            break
        print(line, end="", flush=True)
    err = stderr.read().decode("utf-8", errors="replace")
    if err.strip():
        print(err, file=sys.stderr, flush=True)
    return stdout.channel.recv_exit_status()


def main():
    c = connect()

    print("\n=== MEVCUT DURUM ===", flush=True)
    run_out(c, f"cd {PROJECT} && git log -1 --oneline")
    run_out(c, f"cd {PROJECT} && git fetch origin main 2>&1 && git log origin/main -1 --oneline")
    run_out(c, "pgrep -af 'safe-deploy|docker-compose.*build' || echo 'deploy process yok'")
    run_out(c, f"wc -l {LOG} 2>/dev/null; tail -5 {LOG} 2>/dev/null || echo 'log yok'")
    run_out(c, "find /srv/ecommerce/uploads -type f | wc -l")

    with open(SCRIPT, "rb") as f:
        body = f.read().replace(b"\r\n", b"\n")
    sftp = c.open_sftp()
    sftp.putfo(io.BytesIO(body), f"{PROJECT}/deploy/safe-deploy.sh")
    sftp.chmod(f"{PROJECT}/deploy/safe-deploy.sh", 0o755)
    sftp.close()
    print("safe-deploy.sh yuklendi", flush=True)

    run_out(c, "pkill -f '[b]ash .*safe-deploy.sh' || true")
    run_out(c, f": > {LOG}")

    print("\n=== DEPLOY BASLIYOR (5-15 dk) ===", flush=True)
    code = run_stream(c, f"cd {PROJECT} && bash deploy/safe-deploy.sh 2>&1 | tee -a {LOG}", timeout=2400)

    print("\n=== SONUC ===", flush=True)
    run_out(c, f"cd {PROJECT} && git log -1 --oneline")
    run_out(c, "find /srv/ecommerce/uploads -type f | wc -l")
    run_out(c, "curl -fsS http://localhost:5000/health || echo HEALTH_FAIL")
    run_out(c, f"cd {PROJECT} && docker-compose -f docker-compose.prod.yml ps")
    c.close()
    sys.exit(code)


if __name__ == "__main__":
    main()
