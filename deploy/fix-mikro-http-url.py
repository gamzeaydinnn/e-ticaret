#!/usr/bin/env python3
import io, sys, time
import paramiko

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
HOST = "31.186.24.78"
USER = "huseyinadm"
PASSWORDS = ["Passwd1122%!d", "Passwd1122FFGG"]
PROJECT = "/home/huseyinadm/eticaret"

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
for p in PASSWORDS:
    try:
        c.connect(HOST, username=USER, password=p, timeout=30)
        break
    except paramiko.AuthenticationException:
        continue

def run(cmd, t=240):
    _, o, e = c.exec_command(cmd, timeout=t)
    return (o.read() + e.read()).decode("utf-8", "replace").strip()

script = r"""
set -e
cd /home/huseyinadm/eticaret
cp -a docker-compose.prod.yml "docker-compose.prod.yml.bak.httpfix.$(date +%Y%m%d%H%M%S)"
# Force HTTP ApiUrl for mikro-vpn relay
sed -i 's|MikroSettings__ApiUrl=https://mikro-vpn|MikroSettings__ApiUrl=http://mikro-vpn|g' docker-compose.prod.yml
grep -n 'MikroSettings__ApiUrl' docker-compose.prod.yml
docker-compose -f docker-compose.prod.yml up -d api
"""
sftp = c.open_sftp()
with sftp.file("/tmp/fix-mikro-http.sh", "w") as f:
    f.write(script)
sftp.chmod("/tmp/fix-mikro-http.sh", 0o755)
sftp.close()

print(run("bash /tmp/fix-mikro-http.sh"))
print("waiting 25s...")
time.sleep(25)
print("\nhealth:")
print(run("curl -sS --max-time 30 http://localhost:5000/health"))
print("\napi env check:")
print(run("docker exec ecommerce-api-prod printenv | grep MikroSettings__ApiUrl || true"))
c.close()
