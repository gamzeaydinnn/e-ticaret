#!/bin/bash

# E-Commerce Server Setup Script
# Run this script on the server (31.186.24.78) after uploading the project files

set -e  # Exit on error

REMOTE_PATH="/home/huseyinadm/ecommerce"
SERVER_DOMAIN="31.186.24.78"  # Change to your domain if available

echo "=== E-Commerce Server Setup ==="
echo "Server: $SERVER_DOMAIN"
echo "Project Path: $REMOTE_PATH"
echo ""

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

echo "=== Step 1: Updating System ==="
sudo apt update && sudo apt upgrade -y

echo ""
echo "=== Step 2: Installing Docker ==="
if command_exists docker; then
    echo "[OK] Docker is already installed"
    docker --version
else
    echo "Installing Docker..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo usermod -aG docker $USER
    echo "[OK] Docker installed successfully"
fi

echo ""
echo "=== Step 3: Installing Docker Compose ==="
if command_exists docker-compose; then
    echo "[OK] Docker Compose is already installed"
    docker-compose --version
else
    echo "Installing Docker Compose..."
    sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
    echo "[OK] Docker Compose installed successfully"
fi

echo ""
echo "=== Step 4: Installing Git ==="
if command_exists git; then
    echo "[OK] Git is already installed"
    git --version
else
    echo "Installing Git..."
    sudo apt install -y git
    echo "[OK] Git installed successfully"
fi

echo ""
echo "=== Step 5: Setting Up Firewall ==="
echo "Configuring UFW firewall..."
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw allow 3000/tcp  # Frontend
sudo ufw allow 5000/tcp  # API
sudo ufw --force enable
echo "[OK] Firewall configured"

echo ""
echo "=== Step 6: Creating Project Directory ==="
if [ ! -d "$REMOTE_PATH" ]; then
    sudo mkdir -p $REMOTE_PATH
    sudo chown -R $USER:$USER $REMOTE_PATH
    echo "[OK] Project directory created"
else
    echo "[OK] Project directory already exists"
fi

echo ""
echo "=== Step 7: Setting Up Environment ==="
cd $REMOTE_PATH

# Create .env file if it doesn't exist
if [ ! -f .env ]; then
    cat > .env << 'EOF'
# Database Configuration
DB_PASSWORD=ECom1234
DB_PORT=1435

# API Configuration
API_PORT=5000
ASPNETCORE_ENVIRONMENT=Production

# Frontend Configuration
FRONTEND_PORT=3000
REACT_APP_API_URL=http://31.186.24.78:5000

# JWT Configuration (Generate a strong secret!)
JWT_SECRET=YourVeryStrongSecretKeyMinimum32Characters!!!

# Domain Configuration
SERVER_DOMAIN=31.186.24.78
EOF
    echo "[OK] .env file created"
else
    echo "[OK] .env file already exists"
fi

echo ""
echo "=== Setup Complete! ==="
echo ""
echo "Next steps:"
echo "1. Upload your project files to: $REMOTE_PATH"
echo "2. Update configuration files (appsettings.json, .env)"
echo "3. Run: docker-compose -f docker-compose.prod.yml up -d --build"
echo "4. Check status: docker-compose -f docker-compose.prod.yml ps"
echo "5. View logs: docker-compose -f docker-compose.prod.yml logs -f"
echo ""
echo "Access your application:"
echo "  - Frontend: http://$SERVER_DOMAIN:3000"
echo "  - API: http://$SERVER_DOMAIN:5000"
echo ""

# Check if we need to reload group membership
if groups $USER | grep -q docker; then
    echo "[INFO] Docker group membership active"
else
    echo "[WARNING] Please log out and log back in for Docker group membership to take effect"
    echo "Or run: newgrp docker"
fi
