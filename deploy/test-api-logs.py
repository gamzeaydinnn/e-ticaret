#!/usr/bin/env python3
import io, sys
import paramiko

if hasattr(sys.stdout, "buffer"):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

c = paramiko.SSHClient()
c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
c.connect("31.186.24.78", username="huseyinadm", password="Passwd1122%!d", timeout=20)
_, o, _ = c.exec_command(
    "docker logs ecommerce-api-prod --tail 80 2>&1 | grep -iE 'report|exception|error|fail' | tail -30",
    timeout=30,
)
print(o.read().decode("utf-8", errors="replace"))
c.close()
