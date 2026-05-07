#!/bin/bash
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Categories' ORDER BY ORDINAL_POSITION; SELECT Id, Name FROM Categories ORDER BY Name;"
