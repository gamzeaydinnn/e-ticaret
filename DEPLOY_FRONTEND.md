# Frontend Deployment Instructions

## Quick Deploy Commands

The frontend has been built and is ready to deploy. Run these commands on the production server:

### Option 1: Pull Latest and Rebuild on Server

```bash
# SSH into the server
ssh root@31.186.24.78

# Navigate to project directory
cd /home/eticaret

# Pull latest changes
git pull origin main

# Build frontend
cd frontend
npm run build

# Copy build to web root (if using separate web server)
# sudo cp -r build/* /var/www/eticaret/
```

### Option 2: Manual File Transfer (if needed)

The built files are in: `c:\Users\GAMZE\Desktop\eticaret\frontend\build`

Key files to transfer:
- `index.html` - Main HTML file
- `static/js/main.*.js` - Main JavaScript bundle
- `static/css/main.*.css` - Main CSS styles
- `static/` folder - All static assets

### Option 3: Using Git Direct (RECOMMENDED)

Since the code is already pushed to GitHub:

```bash
# On production server
cd /home/eticaret
git pull origin main

# Rebuild
cd frontend
npm install
npm run build

# Verify build
ls -la build/
```

## What Was Fixed

### Frontend Changes:
1. **PaymentPage.jsx**
   - Added proper UUID v4 generator function
   - Fixed `clientOrderId` to always generate valid GUID format
   - Format: `xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx`

2. **orderService.js**
   - Added GUID format validation
   - Only sends valid `clientOrderId` to backend
   - Sets to `null` if invalid format

### Why This Fixes the 400 Error:
- Backend expects `clientOrderId` as `Guid?` (nullable GUID)
- Previous format `"1234567890-abc123"` was not a valid GUID
- ASP.NET Core couldn't deserialize it, causing 400 error
- Now generates proper UUID v4 format that ASP.NET Core can parse

## Testing Checkout Flow

After deployment, test:
1. Add items to cart
2. Go to payment page
3. Fill in all required fields
4. Click "Confirm Payment"
5. Should now pass validation and proceed to POSNET 3D Secure

## Verify Deployment

```bash
# Check if build folder exists with new timestamp
ls -l /var/www/eticaret/
stat /var/www/eticaret/index.html
```

## Troubleshooting

If you see 404 errors after deployment:
- Check nginx config points to correct build directory
- Verify file permissions: `sudo chmod -R 755 /var/www/eticaret/`
- Restart nginx: `sudo systemctl restart nginx`

## Latest Git Commit

```
32cb91c fix: Validate clientOrderId as GUID format and use proper UUID v4 generator
```

Pull this commit and rebuild the frontend!
