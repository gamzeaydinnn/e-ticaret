#!/usr/bin/env python3
import io
import sys

import paramiko

if hasattr(sys.stdout, "buffer"):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

HOST = "31.186.24.78"
USER = "huseyinadm"
PASSWORDS = ["Passwd1122%!d", "Passwd1122FFGG"]
PROJECT = "/home/huseyinadm/eticaret"

REMOTE_CMD = r"""
set -e
cd /home/huseyinadm/eticaret
UPLOADS="${UPLOADS_HOST_PATH:-/srv/ecommerce/uploads}"
BEFORE=$(find "$UPLOADS" -type f 2>/dev/null | wc -l | tr -d ' ')
echo "=== Deploy devam (build + restart) ==="
echo "Upload dosya sayisi (once): $BEFORE"
echo "[1/3] Docker build api frontend..."
docker-compose -f docker-compose.prod.yml build api frontend mikro-api-relay mikro-sql-relay
echo "[2/3] Container restart (volume korunur)..."
docker-compose -f docker-compose.prod.yml up -d --force-recreate api frontend
echo "[3/3] Dogrulama..."
sleep 20
AFTER=$(find "$UPLOADS" -type f 2>/dev/null | wc -l | tr -d ' ')
echo "Upload dosya sayisi (sonra): $AFTER"
docker-compose -f docker-compose.prod.yml ps
curl -fsS http://localhost:5000/health && echo "API health OK" || echo "API health bekleniyor..."
echo "=== TAMAMLANDI ==="
"""


def main():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    for pwd in PASSWORDS:
        try:
            client.connect(HOST, username=USER, password=pwd, timeout=30)
            print("SSH OK")
            break
        except paramiko.AuthenticationException:
            continue
    else:
        sys.exit("SSH auth failed")

    stdin, stdout, stderr = client.exec_command(REMOTE_CMD, get_pty=True, timeout=1800)
    for line in iter(stdout.readline, ""):
        print(line, end="")
    err = stderr.read().decode("utf-8", errors="replace")
    if err.strip():
        print(err)
    code = stdout.channel.recv_exit_status()
    client.close()
    sys.exit(code)


if __name__ == "__main__":
    main()
