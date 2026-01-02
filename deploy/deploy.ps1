# E-Commerce Project Deployment Script
# Server: 31.186.24.78
# User: huseyinadm

$SERVER_IP = "31.186.24.78"
$SERVER_USER = "huseyinadm"
$SERVER_PASS = "Passwd1122FFGG"
$REMOTE_PATH = "/home/huseyinadm/ecommerce"
$PROJECT_PATH = "C:\Users\GAMZE\Desktop\eticaret"

Write-Host "=== E-Commerce Deployment Script ===" -ForegroundColor Cyan
Write-Host "Server: $SERVER_IP" -ForegroundColor Green
Write-Host "Remote Path: $REMOTE_PATH" -ForegroundColor Green
Write-Host ""

# Check if required tools are installed
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check if Git is installed
try {
    git --version | Out-Null
    Write-Host "[OK] Git is installed" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Git is not installed. Please install Git first." -ForegroundColor Red
    exit 1
}

# Check if Docker is available (for local testing)
try {
    docker --version | Out-Null
    Write-Host "[OK] Docker is installed" -ForegroundColor Green
} catch {
    Write-Host "[WARNING] Docker is not installed. You won't be able to test locally." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Step 1: Preparing files ===" -ForegroundColor Cyan

# Create deployment archive
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$archiveName = "ecommerce_$timestamp.zip"
$archivePath = Join-Path $PROJECT_PATH "deploy\$archiveName"

# Create deploy directory if it doesn't exist
$deployDir = Join-Path $PROJECT_PATH "deploy"
if (-not (Test-Path $deployDir)) {
    New-Item -ItemType Directory -Path $deployDir | Out-Null
}

# Files and directories to exclude
$excludePatterns = @(
    "node_modules",
    "bin",
    "obj",
    ".vs",
    ".git",
    "deploy",
    "*.user",
    "*.suo",
    "*.cache",
    "build",
    "packages"
)

Write-Host "Creating deployment archive: $archiveName" -ForegroundColor Yellow
Write-Host "This may take a few minutes..." -ForegroundColor Gray

# Create zip file excluding certain directories
$sourceDir = $PROJECT_PATH
$excludeArgs = $excludePatterns | ForEach-Object { "-x!$_\" }

# Use PowerShell's Compress-Archive
$filesToCompress = Get-ChildItem -Path $sourceDir -Recurse | Where-Object {
    $file = $_
    $exclude = $false
    foreach ($pattern in $excludePatterns) {
        if ($file.FullName -like "*\$pattern\*" -or $file.Name -like $pattern) {
            $exclude = $true
            break
        }
    }
    -not $exclude
}

# Note: For large projects, consider using 7-Zip or another tool
Write-Host "[INFO] Compressing files... This might take a while for large projects." -ForegroundColor Gray
Write-Host "[INFO] If this takes too long, consider using Git to clone directly on the server." -ForegroundColor Gray

Write-Host ""
Write-Host "=== Alternative Deployment Methods ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "METHOD 1: Using SCP (Secure Copy)" -ForegroundColor Yellow
Write-Host "If you have SCP/WinSCP installed:" -ForegroundColor Gray
Write-Host "  scp -r $PROJECT_PATH ${SERVER_USER}@${SERVER_IP}:${REMOTE_PATH}" -ForegroundColor White
Write-Host ""
Write-Host "METHOD 2: Using Git (RECOMMENDED)" -ForegroundColor Yellow
Write-Host "1. Push your code to GitHub/GitLab" -ForegroundColor Gray
Write-Host "2. SSH to server: ssh ${SERVER_USER}@${SERVER_IP}" -ForegroundColor White
Write-Host "3. Clone repository: git clone <your-repo-url> ${REMOTE_PATH}" -ForegroundColor White
Write-Host ""
Write-Host "METHOD 3: Using SFTP" -ForegroundColor Yellow
Write-Host "Use an SFTP client like FileZilla or WinSCP:" -ForegroundColor Gray
Write-Host "  Host: $SERVER_IP" -ForegroundColor White
Write-Host "  Username: $SERVER_USER" -ForegroundColor White
Write-Host "  Password: $SERVER_PASS" -ForegroundColor White
Write-Host "  Port: 22" -ForegroundColor White
Write-Host ""

Write-Host "=== Server Setup Commands ===" -ForegroundColor Cyan
Write-Host "After uploading files, SSH to the server and run:" -ForegroundColor Yellow
Write-Host ""

$setupCommands = @"
# SSH to server
ssh ${SERVER_USER}@${SERVER_IP}

# Once connected to the server, run these commands:

# Update system
sudo apt update && sudo apt upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker ${SERVER_USER}

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-`$(uname -s)-`$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Verify installations
docker --version
docker-compose --version

# Navigate to project directory
cd ${REMOTE_PATH}

# Build and start containers
docker-compose -f docker-compose.prod.yml up -d --build

# Check container status
docker-compose -f docker-compose.prod.yml ps

# View logs
docker-compose -f docker-compose.prod.yml logs -f

# Initialize database (if needed)
docker-compose -f docker-compose.prod.yml exec api dotnet ef database update
"@

Write-Host $setupCommands -ForegroundColor White
Write-Host ""

# Save setup commands to file
$setupScriptPath = Join-Path $deployDir "server-setup.sh"
$setupCommands | Out-File -FilePath $setupScriptPath -Encoding UTF8
Write-Host "[INFO] Server setup commands saved to: $setupScriptPath" -ForegroundColor Green

Write-Host ""
Write-Host "=== Quick Start Guide ===" -ForegroundColor Cyan
Write-Host "1. Upload project files to server using Git, SCP, or SFTP" -ForegroundColor Yellow
Write-Host "2. SSH to server: ssh ${SERVER_USER}@${SERVER_IP}" -ForegroundColor Yellow
Write-Host "3. Run server setup commands (saved in server-setup.sh)" -ForegroundColor Yellow
Write-Host "4. Access your application at: http://${SERVER_IP}:3000" -ForegroundColor Yellow
Write-Host ""

Read-Host "Press Enter to continue with file transfer options or Ctrl+C to exit"
