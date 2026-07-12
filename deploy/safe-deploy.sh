#!/bin/bash
# Guvenli production deploy - urun gorselleri, uploads ve veritabani korunur.
# Kullanim: cd /home/huseyinadm/eticaret && bash deploy/safe-deploy.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${PROJECT_ROOT}"

BACKUP_DIR="${HOME}/backups"
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"

read_env() {
  local key="$1"
  local default="${2:-}"
  if [ -f .env ]; then
    local val
    val="$(grep -E "^${key}=" .env | tail -1 | cut -d= -f2- | sed 's/^["'\'' ]//; s/["'\'' ]$//')"
    if [ -n "${val}" ]; then
      echo "${val}"
      return
    fi
  fi
  echo "${default}"
}

UPLOADS_PATH="$(read_env UPLOADS_HOST_PATH "/srv/ecommerce/uploads")"
DB_PASSWORD="$(read_env DB_PASSWORD "ECom1234")"
APP_DB_NAME="$(read_env APP_DB_NAME "ECommerceDb")"

echo "=============================================="
echo "  Guvenli Deploy - ${TIMESTAMP}"
echo "  Proje: ${PROJECT_ROOT}"
echo "  Uploads: ${UPLOADS_PATH}"
echo "=============================================="

mkdir -p "${BACKUP_DIR}"
mkdir -p "${UPLOADS_PATH}"
mkdir -p logs

echo ""
echo "[1/7] Mevcut durum kaydi..."
UPLOAD_COUNT_BEFORE="$(find "${UPLOADS_PATH}" -type f 2>/dev/null | wc -l | tr -d ' ')"
echo "  -> Upload dosya sayisi: ${UPLOAD_COUNT_BEFORE}"

PRODUCT_COUNT=""
if docker ps --format '{{.Names}}' 2>/dev/null | grep -q '^ecommerce-sql-prod$'; then
  PRODUCT_COUNT="$(docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "${DB_PASSWORD}" -C -h -1 -W \
    -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM [${APP_DB_NAME}].dbo.Products;" 2>/dev/null | tr -d '[:space:]' || true)"
  echo "  -> Veritabani urun sayisi: ${PRODUCT_COUNT:-bilinmiyor}"
fi

echo ""
echo "[2/7] Uploads yedegi aliniyor..."
if [ -d "${UPLOADS_PATH}" ] && [ "${UPLOAD_COUNT_BEFORE}" -gt 0 ]; then
  UPLOAD_BACKUP="${BACKUP_DIR}/uploads_${TIMESTAMP}.tar.gz"
  tar -czf "${UPLOAD_BACKUP}" -C "$(dirname "${UPLOADS_PATH}")" "$(basename "${UPLOADS_PATH}")"
  echo "  -> Yedek: ${UPLOAD_BACKUP} ($(du -h "${UPLOAD_BACKUP}" | cut -f1))"
else
  echo "  -> Upload klasoru bos veya yok, yedek atlandi."
fi

echo ""
echo "[3/7] Veritabani yedegi aliniyor..."
if docker ps --format '{{.Names}}' 2>/dev/null | grep -q '^ecommerce-sql-prod$'; then
  DB_BACKUP="/backups/pre_deploy_${TIMESTAMP}.bak"
  if docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "${DB_PASSWORD}" -C \
    -Q "BACKUP DATABASE [${APP_DB_NAME}] TO DISK='${DB_BACKUP}' WITH INIT, COMPRESSION;"; then
    echo "  -> DB yedegi: ${DB_BACKUP}"
  else
    echo "  ! DB yedegi alinamadi (devam ediliyor - sqlserver-data volume korunuyor)"
  fi
else
  echo "  -> SQL container calismiyor, DB yedegi atlandi."
fi

echo ""
echo "[4/7] Sunucu yapilandirmasi korunarak kod guncelleniyor..."
PRESERVE_FILES=(.env vpn.ovpn)
PRESERVE_TMP="${BACKUP_DIR}/preserve_${TIMESTAMP}"
mkdir -p "${PRESERVE_TMP}"
for f in "${PRESERVE_FILES[@]}"; do
  if [ -f "${f}" ]; then
    cp "${f}" "${PRESERVE_TMP}/${f}"
    echo "  -> Korundu: ${f}"
  fi
done

git fetch origin main
git reset --hard origin/main
echo "  -> Aktif commit: $(git log -1 --oneline)"
# Proje icindeki eski uploads/ klasorune dokunma (gercek veri /srv/ecommerce/uploads'ta)
git clean -fd -e uploads -e logs -e backups || true

for f in "${PRESERVE_FILES[@]}"; do
  if [ -f "${PRESERVE_TMP}/${f}" ]; then
    cp "${PRESERVE_TMP}/${f}" "${f}"
    echo "  -> Geri yuklendi: ${f}"
  fi
done

echo ""
echo "[5/7] Docker image'lari yeniden olusturuluyor (volume'lar DOKUNULMAZ)..."
docker-compose -f docker-compose.prod.yml build api frontend mikro-api-relay mikro-sql-relay

echo ""
echo "[6/7] Container'lar yeniden baslatiliyor (down -v KULLANILMAZ)..."
docker-compose -f docker-compose.prod.yml down --remove-orphans
docker-compose -f docker-compose.prod.yml up -d

echo ""
echo "[7/7] Dogrulama..."
sleep 15

UPLOAD_COUNT_AFTER="$(find "${UPLOADS_PATH}" -type f 2>/dev/null | wc -l | tr -d ' ')"
echo "  -> Upload dosya sayisi (sonra): ${UPLOAD_COUNT_AFTER}"

if [ "${UPLOAD_COUNT_BEFORE}" -gt 0 ] && [ "${UPLOAD_COUNT_AFTER}" -lt "${UPLOAD_COUNT_BEFORE}" ]; then
  echo ""
  echo "  HATA: Upload dosya sayisi azaldi (${UPLOAD_COUNT_BEFORE} -> ${UPLOAD_COUNT_AFTER})"
  echo "  Yedegi geri yuklemek icin:"
  echo "    tar -xzf ${BACKUP_DIR}/uploads_${TIMESTAMP}.tar.gz -C $(dirname "${UPLOADS_PATH}")"
  exit 1
fi

PRODUCT_COUNT_AFTER=""
if docker ps --format '{{.Names}}' 2>/dev/null | grep -q '^ecommerce-sql-prod$'; then
  PRODUCT_COUNT_AFTER="$(docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "${DB_PASSWORD}" -C -h -1 -W \
    -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM [${APP_DB_NAME}].dbo.Products;" 2>/dev/null | tr -d '[:space:]' || true)"
  echo "  -> Veritabani urun sayisi (sonra): ${PRODUCT_COUNT_AFTER:-bilinmiyor}"

  if [ -n "${PRODUCT_COUNT}" ] && [ -n "${PRODUCT_COUNT_AFTER}" ] && [ "${PRODUCT_COUNT_AFTER}" -lt "${PRODUCT_COUNT}" ]; then
    echo ""
    echo "  HATA: Urun sayisi azaldi (${PRODUCT_COUNT} -> ${PRODUCT_COUNT_AFTER})"
    exit 1
  fi
fi

docker-compose -f docker-compose.prod.yml ps

echo ""
echo "  API health:"
curl -fsS http://localhost:5000/health && echo "" || echo "  (health endpoint henuz hazir degil)"

echo ""
echo "=============================================="
echo "  OK: Guvenli deploy tamamlandi"
echo "  Yedekler: ${BACKUP_DIR}/"
echo "=============================================="
