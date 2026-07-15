#!/usr/bin/env python3
"""SSH uzerinden guvenli production deploy calistirir."""
import sys
import io

import paramiko

# Windows konsolunda unicode hatalarini onle
if hasattr(sys.stdout, "buffer"):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "buffer"):
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8", errors="replace")

HOST = "31.186.24.78"
USER = "huseyinadm"
PASSWORDS = ["Passwd1122%!d", "Passwd1122FFGG"]
PROJECT_CANDIDATES = ["/home/huseyinadm/eticaret", "/home/huseyinadm/ecommerce"]


def connect():
    for pwd in PASSWORDS:
        client = paramiko.SSHClient()
        client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
        try:
            client.connect(
                HOST,
                username=USER,
                password=pwd,
                timeout=30,
                banner_timeout=30,
                auth_timeout=30,
            )
            print(f"SSH bağlantısı OK ({USER}@{HOST})")
            return client
        except paramiko.AuthenticationException:
            print(f"Kimlik doğrulama başarısız (...{pwd[-4:]})")
        except Exception as exc:
            print(f"Bağlantı hatası: {exc}")
    return None


def run(client, cmd, timeout=900):
    print(f"\n>>> {cmd[:120]}{'...' if len(cmd) > 120 else ''}")
    stdin, stdout, stderr = client.exec_command(cmd, get_pty=True, timeout=timeout)
    while True:
        line = stdout.readline()
        if not line:
            break
        print(line, end="")
    err = stderr.read().decode(errors="replace")
    if err.strip():
        print(err, file=sys.stderr)
    code = stdout.channel.recv_exit_status()
    return code


def find_project(client):
    stdin, stdout, _ = client.exec_command(
        "for d in "
        + " ".join(PROJECT_CANDIDATES)
        + '; do [ -d "$d" ] && echo "$d" && break; done',
        timeout=30,
    )
    project = stdout.read().decode().strip().split("\n")[0].strip()
    return project or None


def upload_script(client, project, local_script):
    with open(local_script, "rb") as f:
        content = f.read().replace(b"\r\n", b"\n")
    sftp = client.open_sftp()
    try:
        sftp.stat(f"{project}/deploy")
    except FileNotFoundError:
        sftp.mkdir(f"{project}/deploy")
    remote = f"{project}/deploy/safe-deploy.sh"
    with sftp.file(remote, "wb") as remote_file:
        remote_file.write(content)
    sftp.chmod(remote, 0o755)
    sftp.close()
    print(f"Yüklendi: {remote}")


def main():
    import os

    client = connect()
    if not client:
        sys.exit(1)

    project = find_project(client)
    if not project:
        print("Proje dizini bulunamadı!")
        client.close()
        sys.exit(1)
    print(f"Proje dizini: {project}")

    script_path = os.path.join(os.path.dirname(__file__), "safe-deploy.sh")
    upload_script(client, project, script_path)

    run(
        client,
        f"""
cd {project}
echo '=== SUNUCU DURUM (deploy öncesi) ==='
git log -1 --oneline 2>/dev/null || true
docker-compose -f docker-compose.prod.yml ps 2>/dev/null || true
UPLOADS="${{UPLOADS_HOST_PATH:-/srv/ecommerce/uploads}}"
echo "Uploads: $UPLOADS ($(find "$UPLOADS" -type f 2>/dev/null | wc -l) dosya)"
""",
        timeout=60,
    )

    code = run(client, f"cd {project} && bash deploy/safe-deploy.sh", timeout=1800)
    client.close()
    sys.exit(code)


if __name__ == "__main__":
    main()
