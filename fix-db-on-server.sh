#!/bin/bash
# Fix database initialization issue on production server

echo "Dropping existing ECommerceDb database..."
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P ECom1234 -Q "DROP DATABASE IF EXISTS ECommerceDb;"

if [ $? -eq 0 ]; then
    echo "✓ Database dropped successfully"
else
    echo "✗ Failed to drop database"
    exit 1
fi

echo ""
echo "Restarting API container..."
docker restart ecommerce-api-prod

if [ $? -eq 0 ]; then
    echo "✓ API container restarted"
else
    echo "✗ Failed to restart API container"
    exit 1
fi

echo ""
echo "Waiting for API to initialize (30 seconds)..."
sleep 30

echo ""
echo "Checking container status..."
docker ps | grep ecommerce-api-prod

echo ""
echo "Last 50 lines of API logs:"
docker logs --tail=50 ecommerce-api-prod

echo ""
echo "✓ Fix complete! Check the logs above for any errors."
