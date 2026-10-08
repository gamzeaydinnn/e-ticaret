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


def main():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    for pwd in PASSWORDS:
        try:
            client.connect(HOST, username=USER, password=pwd, timeout=30)
            break
        except Exception as exc:
            print(type(exc).__name__)
    else:
        sys.exit("SSH fail")

    print("SSH OK")
    remote = f"""
set -euo pipefail
cd {PROJECT}
echo HEAD=$(git rev-parse --short HEAD)
# build may already be done; ensure latest code then build+recreate
git fetch origin main
git reset --hard origin/main
echo HEAD2=$(git rev-parse --short HEAD)
docker-compose -f docker-compose.prod.yml build frontend
docker ps -aq --filter name=frontend | xargs -r docker rm -f || true
docker-compose -f docker-compose.prod.yml up -d --no-deps --force-recreate frontend
sleep 8
docker-compose -f docker-compose.prod.yml ps frontend
echo CHECK:
for p in /markalar /kategoriler /kurumsal; do
  line=$(curl -sI "http://localhost:3000$p" | tr -d '\\r' | grep -iE 'HTTP/|Location' | paste -sd ' ' -)
  echo "$p => $line"
done
# footer string in built JS
grep -Rola 'Kategoriler' /var/lib/docker/overlay2 2>/dev/null | head -1 || true
docker exec ecommerce-frontend-prod sh -c "grep -l Kategoriler /usr/share/nginx/html/static/js/*.js | head -1"
docker exec ecommerce-frontend-prod sh -c "grep -c Markalar /usr/share/nginx/html/static/js/*.js || true"
echo DONE
"""
    _, stdout, _ = client.exec_command(remote, get_pty=True, timeout=900)
    while True:
        line = stdout.readline()
        if not line:
            break
        print(line.encode("ascii", "ignore").decode("ascii"), end="")
    code = stdout.channel.recv_exit_status()
    client.close()
    print("exit", code)
    sys.exit(code)


if __name__ == "__main__":
    main()
