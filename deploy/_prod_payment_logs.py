#!/usr/bin/env python3
import io
import sys
import paramiko

if hasattr(sys.stdout, "buffer"):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

HOST = "31.186.24.78"
USER = "huseyinadm"
PASSWORDS = ["Passwd1122%!d", "Passwd1122FFGG"]

FILTER = (
    "posnet|payment|odeme|3d|auth|provizyon|capture|capt|reverse|refund|return|"
    "finans|financial|0058|0211|0229|0411|error|exception|fail|callback|order"
)


def connect():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    for pwd in PASSWORDS:
        try:
            client.connect(HOST, username=USER, password=pwd, timeout=20)
            return client
        except paramiko.AuthenticationException:
            continue
    raise RuntimeError("SSH auth failed")


def main():
    c = connect()
    cmd = (
        "docker logs --since 15m ecommerce-api-prod 2>&1 "
        f"| grep -iE '{FILTER}' | tail -300"
    )
    _, o, e = c.exec_command(cmd, timeout=90)
    out = (o.read() + e.read()).decode("utf-8", errors="replace")
    print(out)
    c.close()


if __name__ == "__main__":
    main()
