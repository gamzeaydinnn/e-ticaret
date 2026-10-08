#!/usr/bin/env python3
import sys
import paramiko

HOST = "31.186.24.78"
USER = "huseyinadm"
PASSWORDS = ["Passwd1122%!d", "Passwd1122FFGG"]


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    for pwd in PASSWORDS:
        try:
            client.connect(HOST, username=USER, password=pwd, timeout=30, banner_timeout=60)
            break
        except paramiko.AuthenticationException:
            continue
    else:
        sys.exit("ssh fail")

    def run(cmd, timeout=90):
        _, stdout, stderr = client.exec_command(cmd, timeout=timeout)
        return (stdout.read() + stderr.read()).decode("utf-8", errors="replace")

    print("=== API LOGS ===", flush=True)
    print(run("docker logs --tail 80 ecommerce-api-prod 2>&1"), flush=True)
    print("=== HEALTH ===", flush=True)
    print(run("curl -sS -w '\\nHTTP:%{http_code}\\n' http://127.0.0.1:5000/health"), flush=True)
    print("=== PRODUCTS ===", flush=True)
    print(
        run(
            "curl -sS -o /tmp/p.json -w 'HTTP:%{http_code}\\n' "
            "'http://127.0.0.1:5000/api/products?page=1&size=1'; "
            "head -c 300 /tmp/p.json; echo"
        ),
        flush=True,
    )
    client.close()


if __name__ == "__main__":
    main()
