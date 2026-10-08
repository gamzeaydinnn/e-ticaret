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

def run(cmd, t=180):
    _, o, e = c.exec_command(cmd, timeout=t)
    return (o.read() + e.read()).decode("utf-8", "replace").strip()

print("restart api...")
print(run(f"cd {PROJECT} && docker-compose -f docker-compose.prod.yml restart api"))
time.sleep(25)
print("\nhealth:")
print(run("curl -sS --max-time 30 http://localhost:5000/health"))
print("\nmikro http from sibling (docker run curl):")
print(run(
    "docker run --rm --network eticaret_ecommerce-network curlimages/curl:8.5.0 "
    "-k -sS --max-time 15 -w '\\nhttp_code=%{http_code}\\n' "
    "https://mikro-vpn:8084/Api/APIMethods/HealthCheck 2>&1 | head -c 1500"
))
print("\nmikro http plaintext:")
print(run(
    "docker run --rm --network eticaret_ecommerce-network curlimages/curl:8.5.0 "
    "-sS --max-time 10 -w '\\nhttp_code=%{http_code}\\n' "
    "http://mikro-vpn:8084/Api/APIMethods/HealthCheck 2>&1 | head -c 800"
))
print("\nmikro containers:")
print(run('docker ps --filter name=mikro --format "table {{.Names}}\\t{{.Status}}"'))
c.close()
