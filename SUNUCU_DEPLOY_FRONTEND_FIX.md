# Frontend API Fix - Server Deployment Instructions

## Quick Summary

A critical bug was found: **frontend API paths had double `/api/` prefix** (e.g., `/api/api/products`).

This has been fixed in all service files. You now need to:
1. Pull the latest code from GitHub
2. Rebuild the frontend Docker image
3. Restart containers
4. Verify fix works

## Server Commands

Run these commands on **31.186.24.78** as user **huseyinadm**:

### Step 1: Pull Latest Code
```bash
cd ~/ecommerce
git pull origin main
```

Expected output should show the 19 modified files.

### Step 2: Rebuild Frontend (Important - includes latest code)
```bash
docker-compose -f docker-compose.prod.yml build frontend --no-cache
```

This will:
- Pull latest code (Step 1 already did this, but Docker build ensures it's used)
- Install dependencies
- Build the React app with `REACT_APP_API_URL=https://golkoygurme.com.tr/api`
- Create new Docker image

Expected time: **5-10 minutes**

### Step 3: Restart All Containers
```bash
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d
```

Wait a few seconds for containers to start.

### Step 4: Verify Containers Are Running
```bash
docker-compose -f docker-compose.prod.yml ps
```

You should see:
- `ecommerce-api-prod` - UP
- `ecommerce-frontend-prod` - UP (health: healthy or starting)
- `ecommerce-sql-prod` - UP (health: healthy)

### Step 5: Verify the Fix Works

**Test 1: Check API is accessible**
```bash
curl -s http://localhost:5000/api/categories | head -c 100
```

Should return JSON data starting with `[{"id":`

**Test 2: Check frontend can reach API**
```bash
docker exec ecommerce-frontend-prod curl -s http://ecommerce-api-prod:5000/api/categories | head -c 100
```

Should return same JSON data as above.

**Test 3: Check Nginx proxy**
```bash
curl -s http://localhost:3000/api/categories | head -c 100
```

Should return JSON (Nginx proxying `/api` to backend).

**Test 4: Check HTML is served**
```bash
curl -s http://localhost:3000 | head -c 200
```

Should return HTML starting with `<!DOCTYPE html>`

### Step 6: Verify in Browser

Open http://localhost:3000 or http://31.186.24.78:3000 and check:

1. **Open Developer Tools** (F12)
2. **Go to Console tab** - look for errors
3. **You should NOT see**:
   - ❌ `404 Not Found` errors
   - ❌ `/api/api/*` paths in Network tab
   - ❌ "Mock data kullanılıyor" messages

4. **You should see**:
   - ✅ Products displayed on homepage
   - ✅ Categories in sidebar/navigation
   - ✅ Network requests to `/api/*` endpoints (not `/api/api/*`)
   - ✅ 200 responses in Network tab

### Step 7: Test Admin Panel

If you want to test admin functionality:

**Login to Admin Panel**:
- URL: http://31.186.24.78:3000/admin
- Username: `admin`
- Password: `admin123`

Check:
- ✅ Login works
- ✅ Dashboard loads
- ✅ Can fetch products/categories/orders
- ✅ No console errors

## Troubleshooting

### If frontend container is "unhealthy"
```bash
# Check logs
docker-compose -f docker-compose.prod.yml logs frontend --tail=50

# Restart just frontend
docker-compose -f docker-compose.prod.yml restart frontend
docker-compose -f docker-compose.prod.yml ps

# Wait 30 seconds, then check again
sleep 30
docker-compose -f docker-compose.prod.yml ps
```

### If you see `/api/api/*` errors still
The code might not have rebuilt correctly:
```bash
# Force rebuild without cache
docker-compose -f docker-compose.prod.yml down
docker image rm ecommerce-frontend:latest  # Remove old image
docker-compose -f docker-compose.prod.yml build frontend --no-cache
docker-compose -f docker-compose.prod.yml up -d
```

### If categories/products still don't show
Check that environment variable is set correctly:
```bash
# See the Nginx config
docker exec ecommerce-frontend-prod cat /etc/nginx/conf.d/default.conf | grep proxy_pass

# Should show: proxy_pass http://ecommerce-api-prod:5000;
```

## What Changed

**Fixed in 7 service files** (removed `/api/` prefix from all API endpoints):

1. `frontend/src/services/categoryServiceReal.js` (5 changes)
2. `frontend/src/services/reviewService.js` (2 changes)
3. `frontend/src/services/otpService.js` (1 change)
4. `frontend/src/services/favoriteService.js` (1 change)
5. `frontend/src/services/courierService.js` (1 change)
6. `frontend/src/services/campaignService.js` (1 change)
7. `frontend/src/services/adminService.js` (21 changes)

**Total**: 32 API path fixes

## Expected Result After Fix

- ✅ Frontend loads without errors
- ✅ Products and categories display correctly
- ✅ Admin panel works
- ✅ All API calls have correct paths
- ✅ No `404` errors in browser console
- ✅ Nginx reverse proxy working correctly

## Time Required

- Pull code: **10 seconds**
- Build frontend: **5-10 minutes**
- Restart containers: **10 seconds**
- Testing: **2-3 minutes**

**Total**: ~10-15 minutes

---

**Questions or Issues?** Check the logs:
```bash
docker-compose -f docker-compose.prod.yml logs -f
```

Press `Ctrl+C` to exit logs.
