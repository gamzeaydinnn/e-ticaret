# Deploy kategori fix to production server
$sshKey = "C:\Users\GAMZE\.ssh\id_rsa"
$server = "31.186.24.78"
$user = "huseyinadm"

Write-Host "🚀 Rebuilding frontend Docker image..." -ForegroundColor Cyan

# Pull latest changes
Write-Host "`n[1/3] Pulling latest changes..." -ForegroundColor Yellow
plink -ssh $user@$server -i $sshKey "cd /home/$user/eticaret && git pull origin main"

# Rebuild frontend
Write-Host "`n[2/3] Rebuilding frontend container..." -ForegroundColor Yellow
plink -ssh $user@$server -i $sshKey "cd /home/$user/eticaret && docker-compose -f docker-compose.prod.yml up -d --build frontend"

# Check status
Write-Host "`n[3/3] Checking container status..." -ForegroundColor Yellow
plink -ssh $user@$server -i $sshKey "docker ps | grep ecommerce"

Write-Host "`n✅ Deployment complete! Visit https://golkoygurme.com.tr to verify" -ForegroundColor Green
