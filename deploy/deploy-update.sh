#!/bin/bash
set -e

echo "=== Updating E-Commerce Production Server ==="
echo "Step 1: Pulling latest code from GitHub..."
cd ~/ecommerce
git pull origin main

echo "Step 2: Stopping current containers..."
docker-compose -f docker-compose.prod.yml down

echo "Step 3: Rebuilding frontend with new API configuration..."
docker-compose -f docker-compose.prod.yml build frontend --no-cache

echo "Step 4: Rebuilding API with new environment variables..."
docker-compose -f docker-compose.prod.yml build api --no-cache

echo "Step 5: Starting containers..."
docker-compose -f docker-compose.prod.yml up -d

echo "Step 6: Waiting for services to be ready..."
sleep 10

echo "Step 7: Testing API endpoint..."
curl -s http://localhost:5000/api/categories | head -50

echo ""
echo "=== Deployment Complete ==="
echo "Frontend: http://31.186.24.78:3000"
echo "API: http://31.186.24.78:5000"
