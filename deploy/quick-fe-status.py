#!/usr/bin/env python3
import paramiko

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
for pwd in ["Passwd1122%!d", "Passwd1122FFGG"]:
    try:
        c.connect("31.186.24.78", username="huseyinadm", password=pwd, timeout=20)
        break
    except paramiko.AuthenticationException:
        pass

cmds = r"""
cd /home/huseyinadm/eticaret
echo HEAD=$(git rev-parse --short HEAD)
echo ---
docker ps --format '{{.Names}} {{.Status}}' | grep -E 'frontend|api' || true
echo ---
pgrep -af 'docker build|npm run build|frontend-only|safe-deploy' | grep -v pgrep || echo 'aktif-build-yok'
echo ---
docker images --format '{{.Repository}}:{{.Tag}} {{.CreatedSince}}' | grep frontend | head -3
"""
_, o, e = c.exec_command(cmds, timeout=40)
print(o.read().decode("utf-8", errors="replace"))
err = e.read().decode("utf-8", errors="replace")
if err.strip():
    print(err)
c.close()
