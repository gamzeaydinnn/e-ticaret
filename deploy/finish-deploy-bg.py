#!/usr/bin/env python3
"""Deploy'u sunucuda arka planda baslatir, SSH donmasin."""
import io, sys
import paramiko

if hasattr(sys.stdout, "buffer"):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

REMOTE = r"""#!/bin/bash
set -e
LOG=/home/huseyinadm/deploy.log
cd /home/huseyinadm/eticaret
UPLOADS="${UPLOADS_HOST_PATH:-/srv/ecommerce/uploads}"
BEFORE=$(find "$UPLOADS" -type f 2>/dev/null | wc -l | tr -d ' ')
{
  echo "=== $(date) Deploy basladi ==="
  echo "Upload once: $BEFORE"
  echo "[build] api..."
  docker-compose -f docker-compose.prod.yml build --progress=plain api 2>&1
  echo "[build] frontend..."
  docker-compose -f docker-compose.prod.yml build --progress=plain frontend 2>&1
  echo "[restart] api frontend..."
  docker-compose -f docker-compose.prod.yml up -d --force-recreate api frontend 2>&1
  sleep 15
  AFTER=$(find "$UPLOADS" -type f 2>/dev/null | wc -l | tr -d ' ')
  echo "Upload sonra: $AFTER"
  docker-compose -f docker-compose.prod.yml ps
  curl -fsS http://localhost:5000/health && echo " health OK" || echo " health bekleniyor"
  echo "=== $(date) Deploy bitti exit=$? ==="
} >> "$LOG" 2>&1
"""

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
for pwd in ["Passwd1122%!d", "Passwd1122FFGG"]:
    try:
        c.connect("31.186.24.78", username="huseyinadm", password=pwd, timeout=20)
        break
    except paramiko.AuthenticationException:
        pass
else:
    sys.exit("auth fail")

sftp = c.open_sftp()
with sftp.file("/home/huseyinadm/run-deploy.sh", "w") as f:
    f.write(REMOTE.replace("\r\n", "\n"))
sftp.chmod("/home/huseyinadm/run-deploy.sh", 0o755)
sftp.close()

# Zaten calisiyorsa tekrar baslatma
_, o, _ = c.exec_command("pgrep -f run-deploy.sh >/dev/null && echo RUNNING || echo IDLE", timeout=10)
status = o.read().decode().strip()
if status == "RUNNING":
    print("Deploy zaten calisiyor, log takip ediliyor...")
else:
    c.exec_command("nohup bash /home/huseyinadm/run-deploy.sh >/dev/null 2>&1 &", timeout=10)
    print("Deploy arka planda baslatildi -> ~/deploy.log")

_, o, _ = c.exec_command("tail -15 /home/huseyinadm/deploy.log 2>/dev/null || echo '(log henuz bos)'", timeout=10)
print(o.read().decode("utf-8", errors="replace"))
c.close()
