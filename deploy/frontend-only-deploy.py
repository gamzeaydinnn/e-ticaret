#!/usr/bin/env python3
"""Sadece frontend image rebuild + recreate (API/DB dokunulmaz)."""
import sys
import paramiko

HOST = "31.186.24.78"
USER = "huseyinadm"
PROJECT = "/home/huseyinadm/eticaret"
PASSWORDS = ["Passwd1122%!d", "Passwd1122FFGG"]
EXPECTED_SHORT = "cad2133"


def out(msg):
    print(msg, flush=True)


def main():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    connected = False
    for pwd in PASSWORDS:
        try:
            client.connect(
                HOST,
                username=USER,
                password=pwd,
                timeout=30,
                banner_timeout=30,
                auth_timeout=30,
            )
            connected = True
            break
        except Exception as exc:
            out(f"SSH fail: {type(exc).__name__}")

    if not connected:
        sys.exit("SSH baglantisi basarisiz")

    out(f"SSH OK -> frontend-only deploy ({EXPECTED_SHORT})")

    remote = f"""
set -euo pipefail
cd {PROJECT}
echo "=== FRONTEND-ONLY DEPLOY ==="
git fetch origin main
git reset --hard origin/main
echo "HEAD: $(git rev-parse --short HEAD)"
docker-compose -f docker-compose.prod.yml build frontend
docker-compose -f docker-compose.prod.yml up -d --no-deps --force-recreate frontend
sleep 8
docker-compose -f docker-compose.prod.yml ps frontend
curl -fsS -o /dev/null -w "frontend_http=%{{http_code}}\\n" http://localhost:3000/ || true
echo "OK: frontend-only deploy tamamlandi"
"""

    _, stdout, stderr = client.exec_command(remote, get_pty=True, timeout=1200)
    while True:
        line = stdout.readline()
        if not line:
            break
        print(line, end="", flush=True)

    err = stderr.read().decode("utf-8", errors="replace")
    if err.strip():
        print(err, flush=True)

    code = stdout.channel.recv_exit_status()
    head = ""
    try:
        _, so, _ = client.exec_command(
            f"cd {PROJECT} && git rev-parse --short HEAD", timeout=30
        )
        head = so.read().decode("utf-8", errors="replace").strip()
    except Exception:
        pass
    client.close()

    out(f"Sunucu HEAD: {head or '?'}")
    if code != 0:
        sys.exit(code)
    if head and not head.startswith(EXPECTED_SHORT[:7]):
        out(f"UYARI: beklenen {EXPECTED_SHORT}, sunucu {head}")
    out(f"Basarili: frontend-only ({head or EXPECTED_SHORT})")


if __name__ == "__main__":
    main()
