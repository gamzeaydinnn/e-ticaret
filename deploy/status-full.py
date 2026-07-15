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

cmd = """
cd /home/huseyinadm/eticaret
echo "GIT:" $(git log -1 --oneline)
echo "UPLOADS:" $(find /srv/ecommerce/uploads -type f | wc -l)
echo "URUN:" $(docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P ECom1234 -C -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM ECommerceDb.dbo.Products;" 2>/dev/null | tr -d '[:space:]')
curl -fsS http://localhost:5000/health 2>/dev/null | head -c 200 || echo "health: bekleniyor"
echo ""
docker-compose -f docker-compose.prod.yml ps
"""
_, o, _ = c.exec_command(cmd, timeout=30)
print(o.read().decode("utf-8", errors="replace"))
c.close()
