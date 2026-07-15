#!/usr/bin/env python3
"""Sunucuda safe-deploy.sh yukler ve arka planda calistirir."""
import io
import os
import subprocess
import sys
import time

import paramiko

if hasattr(sys.stdout, "buffer"):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

HOST = "31.186.24.78"
USER = "huseyinadm"
PASSWORDS = ["Passwd1122%!d", "Passwd1122FFGG"]
PROJECT = "/home/huseyinadm/eticaret"
LOG = "/home/huseyinadm/deploy.log"
SCRIPT_LOCAL = os.path.join(os.path.dirname(__file__), "safe-deploy.sh")


def connect():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    for pwd in PASSWORDS:
        try:
            client.connect(HOST, username=USER, password=pwd, timeout=30)
            return client
        except paramiko.AuthenticationException:
            continue
    sys.exit("SSH baglantisi basarisiz")


def run(client, cmd, timeout=120):
    _, stdout, stderr = client.exec_command(cmd, timeout=timeout)
    out = stdout.read().decode("utf-8", errors="replace")
    err = stderr.read().decode("utf-8", errors="replace")
    return (out + err).strip()


def target_commit():
    return subprocess.check_output(
        ["git", "rev-parse", "HEAD"], cwd=os.path.dirname(SCRIPT_LOCAL), text=True
    ).strip()


def main():
    expected = target_commit()
    expected_short = expected[:7]
    print(f"Hedef commit: {expected_short} ({expected})")

    client = connect()
    print(f"SSH OK -> {HOST}")

    with open(SCRIPT_LOCAL, "rb") as f:
        content = f.read().replace(b"\r\n", b"\n")

    sftp = client.open_sftp()
    sftp.putfo(io.BytesIO(content), f"{PROJECT}/deploy/safe-deploy.sh")
    sftp.chmod(f"{PROJECT}/deploy/safe-deploy.sh", 0o755)
    sftp.close()

    server_head = run(
        client,
        f"cd {PROJECT} && git fetch origin main 2>&1 && git rev-parse origin/main",
        timeout=180,
    ).splitlines()
    server_origin = server_head[-1].strip() if server_head else ""
    print(f"Sunucu origin/main: {server_origin[:7] if server_origin else '?'}")

    if server_origin != expected:
        print("UYARI: GitHub ile yerel commit farkli; sunucu origin/main deploy edilecek.")

    running = run(
        client,
        "pgrep -af '[b]ash .*safe-deploy.sh' >/dev/null && echo RUNNING || echo IDLE",
    ).strip()

    if running == "RUNNING":
        print("Onceki deploy calisiyor, bitmesi bekleniyor...")
        for _ in range(40):
            time.sleep(30)
            running = run(
                client,
                "pgrep -af '[b]ash .*safe-deploy.sh' >/dev/null && echo RUNNING || echo IDLE",
            ).strip()
            if running == "IDLE":
                break
        else:
            print("Onceki deploy cok uzun surdu, yeniden baslatiliyor...")
            run(client, "pkill -f '[b]ash .*safe-deploy.sh' || true")

    head_now = run(client, f"cd {PROJECT} && git rev-parse HEAD").strip()
    if head_now == expected and running == "IDLE":
        print("Sunucu zaten hedef commit'te; yine de rebuild yapiliyor.")

    run(client, f": > {LOG}")
    run(client, f"nohup bash {PROJECT}/deploy/safe-deploy.sh > {LOG} 2>&1 &")
    print("Guvenli deploy baslatildi (yeni log).")

    print("\n--- Oncesi ---")
    print(run(client, f"cd {PROJECT} && git log -1 --oneline && find /srv/ecommerce/uploads -type f | wc -l"))

    success = False
    for i in range(40):
        time.sleep(30)
        running = run(
            client,
            "pgrep -af '[b]ash .*safe-deploy.sh' >/dev/null && echo RUNNING || echo IDLE",
        ).strip()
        tail = run(client, f"tail -12 {LOG} 2>/dev/null || echo '(log bos)'")
        uploads = run(client, "find /srv/ecommerce/uploads -type f | wc -l").strip()
        head = run(client, f"cd {PROJECT} && git rev-parse HEAD 2>/dev/null").strip()
        print(f"\n[{i + 1}/40] deploy={running} head={head[:7] if head else '?'} uploads={uploads}")
        print(tail)

        log_tail = run(client, f"tail -5 {LOG}")
        done = "OK: Guvenli deploy tamamlandi" in log_tail
        if running == "IDLE" and done and head == expected:
            success = True
            break
        if running == "IDLE" and done and head != expected:
            print(f"HATA: Deploy bitti ama commit eslesmedi ({head[:7]} != {expected_short})")
            break
        if running == "IDLE" and i > 3 and not done:
            print("HATA: Deploy durdu ama basari mesaji yok.")
            print(run(client, f"tail -40 {LOG}"))
            break

    print("\n--- Son durum ---")
    print(run(client, f"cd {PROJECT} && git log -1 --oneline && docker-compose -f docker-compose.prod.yml ps"))
    print("Uploads:", run(client, "find /srv/ecommerce/uploads -type f | wc -l").strip())
    print(run(client, "curl -fsS http://localhost:5000/health 2>/dev/null || echo health-bekleniyor"))
    client.close()

    if not success:
        sys.exit(1)
    print(f"\nBasarili: {expected_short} production'da.")


if __name__ == "__main__":
    main()
