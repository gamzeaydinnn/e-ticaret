#!/usr/bin/env python3
import io, sys
import paramiko

if hasattr(sys.stdout, "buffer"):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

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

REMOTE = r"""
cd /home/huseyinadm/eticaret
nohup bash deploy/safe-deploy.sh > /home/huseyinadm/deploy.log 2>&1 &
echo "PID=$!"
sleep 5
echo "--- LOG ---"
head -25 /home/huseyinadm/deploy.log 2>/dev/null || echo "log yok"
echo "--- PROCESS ---"
pgrep -af "safe-deploy|docker-compose" | grep -v pgrep || echo "process yok"
"""

_, stdout, _ = c.exec_command(REMOTE, timeout=30)
print(stdout.read().decode("utf-8", errors="replace"))
c.close()
