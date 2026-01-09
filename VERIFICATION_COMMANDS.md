# Frontend Fix Verification - Run These Commands on Server

Run these commands on **31.186.24.78** to verify the fix is working:

## Wait for Containers to Be Healthy
```bash
# Give containers 10 seconds to start, then check status
sleep 10
docker-compose -f docker-compose.prod.yml ps
```

Expected output: All containers should show `Up` status. Frontend may show `(health: starting)` initially.

---

## Test 1: API Direct Access
```bash
curl -s http://localhost:5000/api/categories | jq . | head -20
```

**Expected**: JSON array of categories with proper data (7+ items)

**NOT Expected**: Empty response or error message

---

## Test 2: Frontend Can Access API
```bash
docker exec ecommerce-frontend-prod curl -s http://ecommerce-api-prod:5000/api/categories | head -c 50
```

**Expected**: `[{"id":3,"name":"Et ve Et Ürünleri"`...

**NOT Expected**: Connection error or 404

---

## Test 3: Nginx Proxy Is Working
```bash
curl -s http://localhost:3000/api/categories | head -c 50
```

**Expected**: Same JSON data as Test 1 (Nginx successfully proxying)

**NOT Expected**: HTML page or 404 error

---

## Test 4: Frontend HTML Is Served
```bash
curl -s http://localhost:3000 | head -c 300
```

**Expected**: HTML starting with `<!DOCTYPE html>` and `<meta charset="utf-8">`

**NOT Expected**: Nginx error page or blank response

---

## Test 5: Check Browser Console (CRITICAL)

Open **http://31.186.24.78:3000** in your browser and:

1. **Press F12** to open Developer Tools
2. **Go to Console tab**
3. **Look for errors** - should be CLEAN with no red text

You should **NOT** see:
- ❌ `Failed to load resource: the server responded with a status of 404`
- ❌ `GET http://localhost:3000/api/api/products 404` (double /api/)
- ❌ `Üründler yüklenemedi, mock data kullanılıyor`
- ❌ `CORS` errors
- ❌ `Cannot read property of undefined`

You **SHOULD** see:
- ✅ Homepage loads without errors
- ✅ Products displayed in product list
- ✅ Categories visible in sidebar/navigation
- ✅ Network tab shows `/api/categories`, `/api/products` (NOT `/api/api/...`)
- ✅ All network requests return 200

---

## Test 6: Check Network Tab in Browser

1. **Open F12** Developer Tools
2. **Go to Network tab**
3. **Refresh the page** (Ctrl+R)
4. **Look for API requests**

Filter by `api` or look for these requests:
- `GET /api/categories` → Should be **200** ✅
- `GET /api/products` → Should be **200** ✅
- `GET /api/banners` → Should be **200** ✅

**NOT**:
- ❌ `/api/api/categories` → Would be **404**
- ❌ `/api/api/products` → Would be **404**

---

## Test 7: Admin Panel (Optional)

If you want to test admin functionality:

1. Open http://31.186.24.78:3000/admin
2. Login with:
   - Username: `admin`
   - Password: `admin123`
3. Dashboard should load without errors
4. Check Console (F12) for any 404 errors

---

## Quick Summary Check

Run this one command to see all 4 critical tests at once:

```bash
echo "=== API Direct ===" && \
curl -s http://localhost:5000/api/categories | head -c 40 && echo "" && \
echo "=== Frontend→API ===" && \
docker exec ecommerce-frontend-prod curl -s http://ecommerce-api-prod:5000/api/categories | head -c 40 && echo "" && \
echo "=== Nginx Proxy ===" && \
curl -s http://localhost:3000/api/categories | head -c 40 && echo "" && \
echo "=== Frontend HTML ===" && \
curl -s http://localhost:3000 | grep -o "<!DOCTYPE" && echo "...OK"
```

---

## If Something is Wrong

### Containers not starting?
```bash
docker-compose -f docker-compose.prod.yml logs --tail=50
```

### Frontend container unhealthy?
```bash
docker-compose -f docker-compose.prod.yml logs frontend --tail=30
```

### Need to rebuild?
```bash
docker-compose -f docker-compose.prod.yml down
docker image rm ecommerce-frontend:latest
docker-compose -f docker-compose.prod.yml build frontend --no-cache
docker-compose -f docker-compose.prod.yml up -d
```

---

## Success Indicators

✅ **All tests pass** if you see:
- Test 1: JSON categories data
- Test 2: JSON data from inside frontend container
- Test 3: JSON data through Nginx proxy
- Test 4: HTML page loads
- Test 5: Browser console is clean, products visible
- Test 6: Network tab shows `/api/*` endpoints (not `/api/api/*`)
- Test 7: Admin login works (if tested)

When all tests pass, the fix is **COMPLETE** ✅
