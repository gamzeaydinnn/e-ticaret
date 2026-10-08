#!/usr/bin/env python3
"""Sunucuda mikro-vpn config yolunu duzelt ve stacki yeniden baslat."""
import io
import sys
import time

import paramiko

if hasattr(sys.stdout, "buffer"):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

HOST = "31.186.24.78"
USER = "huseyinadm"
PASSWORDS = ["Passwd1122%!d", "Passwd1122FFGG"]
PROJECT = "/home/huseyinadm/eticaret"
SECRET_OVPN = "/home/huseyinadm/secrets/mikro.ovpn"


def connect():
    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    for pwd in PASSWORDS:
        try:
            client.connect(HOST, username=USER, password=pwd, timeout=30)
            return client
        except paramiko.AuthenticationException:
            continue
    sys.exit("SSH baglantisi basarisiz")


def run(client, cmd, timeout=180):
    _, stdout, stderr = client.exec_command(cmd, timeout=timeout)
    out = stdout.read().decode("utf-8", errors="replace")
    err = stderr.read().decode("utf-8", errors="replace")
    return (out + err).strip()


def section(title):
    print(f"\n========== {title} ==========")


def main():
    client = connect()
    print(f"SSH OK -> {HOST}")

    section("1) validate secret ovpn")
    out = run(
        client,
        f"test -f {SECRET_OVPN} && test -s {SECRET_OVPN} && wc -c {SECRET_OVPN} && "
        f"grep -E '^(client|dev |remote |<ca>|<cert>|<key>|<tls-crypt)' {SECRET_OVPN} | head -20",
    )
    print(out)
    if "No such file" in out or not out:
        sys.exit("Gecerli mikro.ovpn bulunamadi")

    section("2) remove bogus vpn.ovpn directory mount")
    print(
        run(
            client,
            f"cd {PROJECT} && "
            "if [ -d vpn.ovpn ]; then "
            "  echo 'vpn.ovpn bir dizin; yedekleniyor'; "
            "  mv vpn.ovpn vpn.ovpn.dir.bak.$(date +%Y%m%d%H%M%S); "
            "elif [ -f vpn.ovpn ] && [ ! -s vpn.ovpn ]; then "
            "  echo 'vpn.ovpn bos dosya; yedekleniyor'; "
            "  mv vpn.ovpn vpn.ovpn.empty.bak.$(date +%Y%m%d%H%M%S); "
            "else "
            "  ls -la vpn.ovpn 2>&1 || echo 'vpn.ovpn yok'; "
            "fi",
        )
    )

    section("3) fix VPN_CONFIG_PATH in .env")
    # Write a small remote script to avoid PowerShell/python quoting hell
    remote_fix = r"""
set -e
cd /home/huseyinadm/eticaret
cp -a .env ".env.bak.vpnfix.$(date +%Y%m%d%H%M%S)"
if grep -q '^VPN_CONFIG_PATH=' .env; then
  sed -i 's|^VPN_CONFIG_PATH=.*|VPN_CONFIG_PATH=/home/huseyinadm/secrets/mikro.ovpn|' .env
else
  printf '\nVPN_CONFIG_PATH=/home/huseyinadm/secrets/mikro.ovpn\n' >> .env
fi
grep '^VPN_CONFIG_PATH=' .env
"""
    sftp = client.open_sftp()
    with sftp.file("/tmp/fix-vpn-env.sh", "w") as f:
        f.write(remote_fix)
    sftp.chmod("/tmp/fix-vpn-env.sh", 0o755)
    sftp.close()
    print(run(client, "bash /tmp/fix-vpn-env.sh"))

    section("4) recreate mikro-vpn + relays")
    print(
        run(
            client,
            f"cd {PROJECT} && docker-compose -f docker-compose.prod.yml up -d --force-recreate mikro-vpn 2>&1",
            timeout=240,
        )
    )
    print("Waiting 15s for gluetun boot...")
    time.sleep(15)
    print(
        run(
            client,
            f"cd {PROJECT} && docker-compose -f docker-compose.prod.yml up -d --force-recreate "
            "mikro-api-relay mikro-sql-relay 2>&1",
            timeout=240,
        )
    )

    print("Waiting 40s for OpenVPN handshake...")
    time.sleep(40)

    section("5) status")
    print(
        run(
            client,
            'docker ps -a --filter name=mikro --format "table {{.Names}}\\t{{.Status}}\\t{{.Image}}"',
        )
    )
    print(
        run(
            client,
            'docker inspect mikro-vpn --format '
            '"status={{.State.Status}} health={{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}" 2>&1',
        )
    )

    section("6) logs")
    print(run(client, "docker logs --tail 80 mikro-vpn 2>&1"))

    section("7) openvpn status")
    print(
        run(
            client,
            "docker exec mikro-vpn wget -q -O- http://127.0.0.1:8000/v1/openvpn/status 2>&1 | head -c 3000",
        )
    )

    section("8) routes / config mount")
    print(
        run(
            client,
            "docker exec mikro-vpn sh -c "
            "'ls -la /gluetun/custom.conf; echo ---; head -5 /gluetun/custom.conf; echo ---; "
            "ip addr show tun0 2>/dev/null || ip addr show tun1 2>/dev/null || echo no-tun; "
            "echo ---; ip route | head -20' 2>&1",
        )
    )

    section("9) relay from host network ns of vpn")
    print(
        run(
            client,
            "docker exec mikro-vpn sh -c "
            "'wget -q -O- --timeout=8 http://127.0.0.1:8084/Api/APIMethods/HealthCheck 2>&1 | head -c 500; echo; "
            "nc -z -w 5 127.0.0.1 1433 && echo local_sql_relay=open || echo local_sql_relay=closed; "
            "nc -z -w 5 10.0.0.3 1433 && echo target_sql=open || echo target_sql=closed; "
            "nc -z -w 5 10.0.0.3 8084 && echo target_api=open || echo target_api=closed' 2>&1",
        )
    )

    section("10) api health")
    # give API a moment; may need restart if DNS cached failed
    print(run(client, "curl -sS --max-time 20 http://localhost:5000/health 2>&1 | head -c 3500"))

    # If health still bad because api resolved mikro-vpn while it was restarting,
    # bounce api briefly so it reconnects.
    health = run(client, "curl -sS --max-time 20 http://localhost:5000/health 2>&1")
    if '"status":"Healthy"' not in health and "Healthy" not in health:
        section("11) restart api to refresh mikro connectivity")
        print(
            run(
                client,
                f"cd {PROJECT} && docker-compose -f docker-compose.prod.yml restart api 2>&1",
                timeout=180,
            )
        )
        time.sleep(20)
        print(run(client, "curl -sS --max-time 25 http://localhost:5000/health 2>&1 | head -c 3500"))

    client.close()
    print("\nDone.")


if __name__ == "__main__":
    main()
