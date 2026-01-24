#!/bin/bash
# Frontend Deployment Script for Production Server
# Run this script on the production server (31.186.24.78) as root

set -e

echo "=========================================="
echo "Frontend Deployment Script"
echo "=========================================="

# Navigate to project directory
cd /home/eticaret

echo "1. Pulling latest code from GitHub..."
git pull origin main

echo ""
echo "2. Building frontend..."
cd frontend
npm run build

echo ""
echo "3. Backing up current build..."
if [ -d "/var/www/eticaret" ]; then
  mkdir -p /var/www/eticaret-backup-$(date +%Y%m%d_%H%M%S)
  cp -r /var/www/eticaret/* /var/www/eticaret-backup-$(date +%Y%m%d_%H%M%S)/ || true
fi

echo ""
echo "4. Copying new build to web root..."
mkdir -p /var/www/eticaret
cp -r build/* /var/www/eticaret/

echo ""
echo "5. Setting file permissions..."
chmod -R 755 /var/www/eticaret/
chown -R nginx:nginx /var/www/eticaret/

echo ""
echo "6. Restarting nginx..."
systemctl restart nginx

echo ""
echo "=========================================="
echo "Frontend Deployment Complete!"
echo "=========================================="
echo ""
echo "Frontend URL: http://31.186.24.78:3000"
echo ""
echo "Latest commit: $(git log --oneline -1)"
echo ""
echo "Verify deployment:"
echo "  curl http://31.186.24.78:3000"
echo ""
