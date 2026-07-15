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

_, o, _ = c.exec_command(
    "pgrep -f safe-deploy.sh >/dev/null && echo CALISIYOR || echo BITTI; "
    "tail -15 /home/huseyinadm/deploy.log; "
    "cd /home/huseyinadm/eticaret && git log -1 --oneline; "
    "find /srv/ecommerce/uploads -type f | wc -l; "
    "docker-compose -f /home/huseyinadm/eticaret/docker-compose.prod.yml ps --format '{{.Name}} {{.Status}}' 2>/dev/null | head -3",
    timeout=30,
)
print(o.read().decode("utf-8", errors="replace"))
c.close()
