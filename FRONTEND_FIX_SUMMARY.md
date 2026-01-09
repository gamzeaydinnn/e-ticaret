# CRITICAL BUG FIXED: Frontend API Double /api/ Prefix Issue

## Issue Identified

Your frontend was making requests to **`/api/api/products`** instead of **`/api/products`**, causing 404 errors.

Browser console showed:
```
Failed to load resource: the server responded with a status of 404
/api/api/products:1
```

## Root Cause Analysis

The problem was in how API calls were constructed:

### Before (BROKEN):
```
api.js baseURL = "https://golkoygurme.com.tr/api"
Service calls: api.get("/api/products")

Result: baseURL + endpoint = /api + /api/products = /api/api/products ❌
```

### After (FIXED):
```
api.js baseURL = "https://golkoygurme.com.tr/api"  (unchanged)
Service calls: api.get("/products")

Result: baseURL + endpoint = /api + /products = /api/products ✅
```

## Solution Implemented

Removed the `/api/` prefix from **all service file API endpoints** since the baseURL already contains `/api`.

### Files Modified (32 total changes):

1. **categoryServiceReal.js** - 5 changes
   - `getAll()` → `/categories` (was `/api/categories`)
   - `getBySlug()` → `/categories/{slug}` (was `/api/categories/{slug}`)
   - `getById()` → `/admin/categories/{id}` (was `/api/admin/categories/{id}`)
   - `update()` → `/admin/categories/{id}` (was `/api/admin/categories/{id}`)
   - `delete()` → `/admin/categories/{id}` (was `/api/admin/categories/{id}`)

2. **reviewService.js** - 2 changes
   - `getReviewsByProductId()` → `/products/{id}/reviews` (was `/api/products/{id}/reviews`)
   - `createReview()` → `/products/{id}/reviews` (was `/api/products/{id}/reviews`)

3. **otpService.js** - 1 change
   - `getSmsStatus()` → `/sms/status/{phone}` (was `/api/sms/status/{phone}`)

4. **favoriteService.js** - 1 change
   - `getFavorites()` → `/favorites` (was `/api/favorites`)

5. **courierService.js** - 1 change
   - `updateOrderStatus()` → `/courier/orders/{id}/status` (was `/api/courier/orders/{id}/status`)

6. **campaignService.js** - 1 change
   - `getBySlug()` → `/campaigns/{slug}` (was `/api/campaigns/{slug}`)

7. **adminService.js** - 21 changes
   - Dashboard: `/Admin/dashboard/stats` (was `/api/Admin/dashboard/stats`)
   - Users: `/admin/users` endpoints (were `/api/admin/users`)
   - Logs: `/admin/logs/*` endpoints (were `/api/admin/logs/*`)
   - Products: `/admin/products` endpoints (were `/api/admin/products`)
   - Orders: `/admin/orders` endpoints (were `/api/admin/orders`)
   - Reports: `/admin/reports/*` endpoints (were `/api/admin/reports/*`)
   - Coupons: `/admin/coupons` endpoints (were `/api/admin/coupons`)
   - Campaigns: `/admin/campaigns` endpoints (were `/api/admin/campaigns`)

## What Changed

**BEFORE**: Service files called endpoints with `/api/` prefix
```javascript
// categoryServiceReal.js
api.get("/api/categories")      // ❌ Wrong - causes /api/api/categories
api.get(`/api/admin/categories/${id}`)
```

**AFTER**: Service files call endpoints without `/api/` prefix
```javascript
// categoryServiceReal.js
api.get("/categories")          // ✅ Correct - becomes /api/categories
api.get(`/admin/categories/${id}`)
```

## How Nginx Proxy Works

Your Nginx config correctly proxies `/api` requests:
```nginx
location /api {
    proxy_pass http://ecommerce-api-prod:5000;
}
```

So when frontend calls `/api/products`, Nginx routes it to `http://ecommerce-api-prod:5000/api/products` ✅

But when frontend was calling `/api/api/products`, Nginx routed it to `http://ecommerce-api-prod:5000/api/api/products` which doesn't exist on the backend ❌

## Status

✅ **All files have been fixed locally**
✅ **Changes committed to Git**
✅ **Changes pushed to GitHub (main branch)**

## Next Steps for You

Run these commands on server **31.186.24.78**:

```bash
# 1. Pull latest code
cd ~/ecommerce
git pull origin main

# 2. Rebuild frontend with fixed code
docker-compose -f docker-compose.prod.yml build frontend --no-cache

# 3. Restart containers
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d

# 4. Verify
sleep 10
docker-compose -f docker-compose.prod.yml ps

# 5. Test in browser
# Open: http://31.186.24.78:3000
# Check: F12 → Console (should have NO errors)
#        F12 → Network (should see /api/products requests, NOT /api/api/products)
```

## Expected Result

After rebuild:
- ✅ Frontend homepage loads without errors
- ✅ Products and categories display correctly
- ✅ Browser console has NO 404 errors
- ✅ Network tab shows correct `/api/*` paths (not `/api/api/*`)
- ✅ Admin panel works correctly
- ✅ Container shows "healthy" status

## Verification Checklist

Before you run the rebuild, you'll want to verify these after:

1. **Containers Running**: `docker-compose -f docker-compose.prod.yml ps`
   - [ ] All 3 containers are UP
   - [ ] Frontend is healthy (not unhealthy)

2. **API Connectivity**:
   ```bash
   curl -s http://localhost:5000/api/categories | head -c 50
   ```
   - [ ] Returns JSON data

3. **Frontend Access**:
   ```bash
   curl -s http://localhost:3000 | head -c 100
   ```
   - [ ] Returns HTML (not 404)

4. **Browser Test** - Open http://31.186.24.78:3000
   - [ ] Page loads without errors
   - [ ] Products visible on homepage
   - [ ] Categories visible in navigation
   - [ ] Admin login works (admin/admin123)

5. **Browser Console** (F12 → Console):
   - [ ] NO "Failed to load resource: 404" errors
   - [ ] NO "/api/api/products" or similar double paths
   - [ ] NO "Mock data" messages

6. **Network Tab** (F12 → Network):
   - [ ] API calls to `/api/products` (single /api/)
   - [ ] All responses are 200 OK (not 404)

## Git Changes Summary

```
19 files changed, 3374 insertions(+), 50 deletions(-)
```

**Key Changes**:
- Fixed all `/api/` double-prefix issues in 7 service files
- Added deployment documentation
- Added troubleshooting guides

## No Backend Changes Required

This was a **frontend-only fix**. The backend API doesn't need any changes:
- ✅ Backend routes remain `/api/products`, `/api/categories`, etc.
- ✅ No database migrations needed
- ✅ No backend recompilation needed
- ✅ No backend restart needed

Only the frontend needs to be rebuilt with the corrected code.

## How Long?

**Total time on server**:
- `git pull`: ~10 seconds
- `docker build`: 5-10 minutes
- `docker down/up`: ~30 seconds
- Testing: 2-3 minutes

**Total**: ~10-15 minutes

---

## Questions?

Check these files for more details:
- `FRONTEND_API_FIX.md` - Detailed technical explanation
- `SUNUCU_DEPLOY_FRONTEND_FIX.md` - Step-by-step server commands
- `URUN_KATEGORI_POSTER_GORÜNMEME_COZUM.md` - Troubleshooting guide

Everything is ready. Just run the rebuild commands on the server and you'll be good to go! 🚀
