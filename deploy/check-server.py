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

checks = [
    "pgrep -af 'docker-compose|docker build' | head -5 || echo 'build yok'",
    "tail -20 /home/huseyinadm/deploy.log 2>/dev/null || echo 'log yok'",
    "cd /home/huseyinadm/eticaret && docker-compose -f docker-compose.prod.yml ps",
    "find /srv/ecommerce/uploads -type f | wc -l",
]
for cmd in checks:
    print("===", cmd[:60], "===")
    _, o, _ = c.exec_command(cmd, timeout=30)
    print(o.read().decode("utf-8", errors="replace"))
c.close()
