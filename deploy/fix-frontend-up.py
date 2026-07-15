#!/usr/bin/env python3
import sys
import io
import paramiko

if hasattr(sys.stdout, "buffer"):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
for p in ["Passwd1122%!d", "Passwd1122FFGG"]:
    try:
        c.connect("31.186.24.78", username="huseyinadm", password=p, timeout=25)
        break
    except Exception as e:
        print(f"ssh fail: {type(e).__name__}", flush=True)
else:
    sys.exit("ssh fail")

cmd = r"""
set -e
cd /home/huseyinadm/eticaret
echo '--- conflict containers ---'
docker ps -a --format '{{.ID}} {{.Names}} {{.Status}}' | grep -i frontend || true
docker rm -f ecommerce-frontend-prod 2>/dev/null || true
ids=$(docker ps -aq --filter 'name=ecommerce-frontend' || true)
if [ -n "$ids" ]; then docker rm -f $ids; fi
ids2=$(docker ps -aq --filter 'name=frontend-prod' || true)
if [ -n "$ids2" ]; then docker rm -f $ids2; fi
docker-compose -f docker-compose.prod.yml up -d --no-deps --force-recreate frontend
sleep 8
docker ps --format '{{.Names}} {{.Status}}' | grep -E 'frontend|api' || true
curl -fsS -o /dev/null -w 'frontend_http=%{http_code}\n' http://localhost:3000/ || echo curl-fail
echo HEAD=$(git rev-parse --short HEAD)
echo OK
"""

_, stdout, stderr = c.exec_command(cmd, get_pty=True, timeout=180)
data = stdout.read().decode("utf-8", errors="replace")
# strip spinner-ish chars for windows console safety
print("".join(ch if ord(ch) < 0x2800 or ord(ch) > 0x28FF else "." for ch in data), flush=True)
err = stderr.read().decode("utf-8", errors="replace")
if err.strip():
    print(err, flush=True)
c.close()
