#!/bin/bash

# Quick deployment script for updates
# Run this on the server after uploading new code

set -e

PROJECT_PATH="/home/huseyinadm/ecommerce"

echo "=== E-Commerce Quick Update ===="
echo ""

cd $PROJECT_PATH

echo "Step 1: Pulling latest changes..."
git pull origin main

echo ""
echo "Step 2: Stopping containers..."
docker-compose -f docker-compose.prod.yml down

echo ""
echo "Step 3: Rebuilding and starting containers..."
docker-compose -f docker-compose.prod.yml up -d --build

echo ""
echo "Step 4: Checking container status..."
sleep 10
docker-compose -f docker-compose.prod.yml ps

echo ""
echo "Step 5: Showing recent logs..."
docker-compose -f docker-compose.prod.yml logs --tail=50

echo ""
echo "=== Update Complete! ==="
echo "Frontend: http://31.186.24.78:3000"
echo "API: http://31.186.24.78:5000"
echo ""
echo "To view logs: docker-compose -f docker-compose.prod.yml logs -f"
