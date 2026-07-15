#!/usr/bin/env python3
import io, sys
import paramiko

if hasattr(sys.stdout, "buffer"):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect("31.186.24.78", username="huseyinadm", password="Passwd1122%!d", timeout=20)

endpoints = [
    "/api/admin/reports/sales?period=daily",
    "/api/admin/reports/stock/low",
    "/api/admin/reports/inventory/movements?from=2026-07-01&to=2026-07-08",
    "/api/admin/reports/erp/sync-status?from=2026-07-01&to=2026-07-08",
]
for ep in endpoints:
    cmd = f'curl -s -w "\\nHTTP:%{{http_code}}" "http://localhost:5000{ep}" | tail -c 500'
    _, o, _ = c.exec_command(cmd, timeout=30)
    print(f"=== {ep} ===")
    print(o.read().decode("utf-8", errors="replace"))
    print()
c.close()
