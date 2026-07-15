#!/usr/bin/env python3
import io, sys, time
import paramiko

if hasattr(sys.stdout, "buffer"):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

def connect():
    c = paramiko.SSHClient()
    c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    for pwd in ["Passwd1122%!d", "Passwd1122FFGG"]:
        try:
            c.connect("31.186.24.78", username="huseyinadm", password=pwd, timeout=20)
            return c
        except paramiko.AuthenticationException:
            pass
    sys.exit("auth fail")

def run(c, cmd):
    _, o, _ = c.exec_command(cmd, timeout=30)
    return o.read().decode("utf-8", errors="replace").strip()

c = connect()
for i in range(12):
    build = run(c, "pgrep -f 'docker-compose.*build' >/dev/null && echo BUILDING || echo IDLE")
    api_age = run(c, "docker inspect ecommerce-api-prod --format '{{.Created}}' 2>/dev/null || echo unknown")
    fe_age = run(c, "docker inspect ecommerce-frontend-prod --format '{{.Created}}' 2>/dev/null || echo unknown")
    uploads = run(c, "find /srv/ecommerce/uploads -type f | wc -l")
    print(f"[{i+1}/12] build={build} uploads={uploads}")
    print(f"  api created: {api_age}")
    print(f"  frontend created: {fe_age}")
    if build == "IDLE":
        health = run(c, "curl -fsS http://localhost:5000/health 2>/dev/null || echo fail")
        print(f"  health: {health}")
        print(run(c, "cd /home/huseyinadm/eticaret && docker-compose -f docker-compose.prod.yml ps"))
        break
    time.sleep(30)
c.close()
