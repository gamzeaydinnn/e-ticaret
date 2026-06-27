#!/bin/bash

set -euo pipefail

echo "Production clean deploy basliyor..."

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

cd "${PROJECT_ROOT}"
echo "Deploy dizini: ${PROJECT_ROOT}"

mkdir -p logs
mkdir -p "${UPLOADS_HOST_PATH:-/srv/ecommerce/uploads}"

echo "Git durumu:"
git status --short || true

echo "Eski dangling image'lar temizleniyor..."
docker image prune -f >/dev/null 2>&1 || true

echo "Image'lar rebuild ediliyor..."
docker-compose -f docker-compose.prod.yml build

echo "Container'lar temiz sekilde yeniden kuruluyor..."
docker-compose -f docker-compose.prod.yml down --remove-orphans
docker-compose -f docker-compose.prod.yml up -d --force-recreate

echo "Servislerin ayaga kalkmasi bekleniyor..."
sleep 30

echo "Container durumlari:"
docker-compose -f docker-compose.prod.yml ps

echo "API health kontrolu:"
curl -fsS http://localhost:5000/health || true

echo "Ornek uploads kontrolu:"
find "${UPLOADS_HOST_PATH:-/srv/ecommerce/uploads}" -maxdepth 2 -type f | head -n 10 || true

echo "Backend loglari:"
docker logs ecommerce-api-prod --tail 50

echo "Frontend loglari:"
docker logs ecommerce-frontend-prod --tail 20

echo "Production clean deploy tamamlandi."
