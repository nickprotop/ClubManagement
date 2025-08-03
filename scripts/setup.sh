#!/bin/bash

# Club Management Platform Setup Script
# This script sets up the complete development environment

set -e  # Exit on any error

echo "🏊 Club Management Platform Setup"
echo "================================="

# Show usage if help requested
if [[ "$1" == "--help" ]] || [[ "$1" == "-h" ]]; then
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -q, --quick    Quick setup with defaults (no interactive prompts)"
    echo "  -h, --help     Show this help message"
    echo ""
    echo "Interactive Setup (default):"
    echo "  - Prompts for database, Redis, MinIO passwords"
    echo "  - Uses existing .env values as defaults if present"
    echo "  - Generates secure JWT key automatically"
    echo ""
    echo "Quick Setup:"
    echo "  - Uses default values from .env.sample"
    echo "  - Uses existing .env values if present"
    echo "  - Good for development/testing"
    echo ""
    exit 0
fi

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

# Check if .NET 9 is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET 9 is not installed. Please install .NET 9 SDK first."
    exit 1
fi

echo "✅ Prerequisites check passed"

# Check for quick setup flag
QUICK_SETUP=false
if [[ "$1" == "--quick" ]] || [[ "$1" == "-q" ]]; then
    QUICK_SETUP=true
    echo "🚀 Quick setup mode enabled - using defaults"
fi

# Function to prompt for configuration values
prompt_config() {
    local var_name=$1
    local prompt_text=$2
    local default_value=$3
    local current_value

    # Get current value from existing .env if it exists
    if [ -f "docker/.env" ]; then
        current_value=$(grep "^${var_name}=" docker/.env 2>/dev/null | cut -d'=' -f2- | tr -d '"')
    fi

    # Use current value as default if it exists, otherwise use provided default
    local effective_default="${current_value:-$default_value}"
    
    # In quick setup mode, just use the default
    if [ "$QUICK_SETUP" = true ]; then
        echo "$effective_default"
        return
    fi
    
    # Interactive prompt with explicit stdin
    local user_input
    while true; do
        printf "🔧 %s [%s]: " "$prompt_text" "$effective_default" >&2
        if read -r user_input </dev/tty 2>/dev/null; then
            break
        else
            echo "Error reading input, using default: $effective_default" >&2
            user_input="$effective_default"
            break
        fi
    done
    
    # Use default if user didn't enter anything
    if [ -z "$user_input" ]; then
        user_input="$effective_default"
    fi
    
    echo "$user_input"
}

# Interactive configuration setup
echo "🔧 Club Management Platform Configuration"
echo "========================================"
echo ""

# Check if .env exists in docker folder and load current values
if [ -f "docker/.env" ]; then
    echo "📖 Found existing docker/.env file, using current values as defaults..."
    set -a
    source docker/.env
    set +a
    echo ""
else
    echo "📝 Setting up configuration for the first time..."
    echo ""
fi

if [ "$QUICK_SETUP" = true ]; then
    echo "🚀 Using quick setup mode - skipping interactive prompts"
else
    echo "Let's configure your Club Management Platform:"
    echo "💡 Press Enter to use default values shown in brackets"
fi
echo ""

# Database Configuration
echo "🗄️  DATABASE CONFIGURATION"
echo "-------------------------"
POSTGRES_DB=$(prompt_config "POSTGRES_DB" "Database name" "clubmanagement")
POSTGRES_USER=$(prompt_config "POSTGRES_USER" "Database username" "clubadmin")
POSTGRES_PASSWORD=$(prompt_config "POSTGRES_PASSWORD" "Database password" "clubpassword")
POSTGRES_HOST=$(prompt_config "POSTGRES_HOST" "Database host" "localhost")
POSTGRES_PORT=$(prompt_config "POSTGRES_PORT" "Database port" "4004")
echo ""

# Redis Configuration  
echo "📦 REDIS CONFIGURATION"
echo "----------------------"
REDIS_HOST=$(prompt_config "REDIS_HOST" "Redis host" "localhost")
REDIS_PORT=$(prompt_config "REDIS_PORT" "Redis port" "4007")
REDIS_PASSWORD=$(prompt_config "REDIS_PASSWORD" "Redis password (leave empty for no auth)" "")
echo ""

# MinIO Configuration
echo "💾 MINIO CONFIGURATION" 
echo "----------------------"
MINIO_ACCESS_KEY=$(prompt_config "MINIO_ACCESS_KEY" "MinIO access key" "minioadmin")
MINIO_SECRET_KEY=$(prompt_config "MINIO_SECRET_KEY" "MinIO secret key" "minioadmin")
MINIO_ENDPOINT=$(prompt_config "MINIO_ENDPOINT" "MinIO endpoint" "localhost:4005")
MINIO_BUCKET_NAME=$(prompt_config "MINIO_BUCKET_NAME" "MinIO bucket name" "clubmanagement")
echo ""

# JWT Configuration
echo "🔐 JWT CONFIGURATION"
echo "--------------------"
# Generate a secure JWT key if none exists
current_jwt=$(grep "^JWT_SECRET_KEY=" .env 2>/dev/null | cut -d'=' -f2- | tr -d '"')
if [ -z "$current_jwt" ] || [ "$current_jwt" = "YourSuperSecretKeyThatShouldBeAtLeast256BitsLongForHmacSha256Algorithm" ]; then
    echo "🔑 Generating secure JWT secret key..."
    JWT_SECRET_KEY=$(openssl rand -base64 48 | tr -d "=+/" | cut -c1-64)
    echo "✅ Generated new secure JWT key"
else
    JWT_SECRET_KEY="$current_jwt"
    echo "✅ Using existing JWT key"
fi

JWT_ISSUER=$(prompt_config "JWT_ISSUER" "JWT issuer" "ClubManagement")
JWT_AUDIENCE=$(prompt_config "JWT_AUDIENCE" "JWT audience" "ClubManagement")
echo ""

# Application URLs
echo "🌐 APPLICATION CONFIGURATION"
echo "----------------------------"
API_BASE_URL=$(prompt_config "API_BASE_URL" "API base URL" "http://localhost:4000")
CLIENT_BASE_URL=$(prompt_config "CLIENT_BASE_URL" "Client base URL" "http://localhost:4002")
ENVIRONMENT=$(prompt_config "ENVIRONMENT" "Environment" "Development")
echo ""

# Stripe Configuration (optional)
echo "💳 STRIPE CONFIGURATION (Optional - can be set later)"
echo "----------------------------------------------------"
echo "ℹ️  You can skip Stripe configuration now and add it later to .env file"
STRIPE_PUBLISHABLE_KEY=$(prompt_config "STRIPE_PUBLISHABLE_KEY" "Stripe publishable key (optional)" "")
STRIPE_SECRET_KEY=$(prompt_config "STRIPE_SECRET_KEY" "Stripe secret key (optional)" "")
STRIPE_WEBHOOK_SECRET=$(prompt_config "STRIPE_WEBHOOK_SECRET" "Stripe webhook secret (optional)" "")
echo ""

# Docker internal configuration (auto-generated)
DOCKER_POSTGRES_HOST="postgres"
DOCKER_REDIS_HOST="redis" 
DOCKER_MINIO_HOST="minio"
MINIO_USE_SSL="false"
JWT_ACCESS_TOKEN_EXPIRATION_MINUTES="60"
JWT_REFRESH_TOKEN_EXPIRATION_DAYS="7"
MINIO_CONSOLE_PORT="4006"
CERT_PASSWORD="password"

# Create .env file in docker folder
echo "📝 Writing configuration to docker/.env file..."
cat > docker/.env << EOF
# Database Configuration
POSTGRES_HOST=$POSTGRES_HOST
POSTGRES_PORT=$POSTGRES_PORT
POSTGRES_DB=$POSTGRES_DB
POSTGRES_USER=$POSTGRES_USER
POSTGRES_PASSWORD=$POSTGRES_PASSWORD

# JWT Configuration
JWT_SECRET_KEY=$JWT_SECRET_KEY
JWT_ISSUER=$JWT_ISSUER
JWT_AUDIENCE=$JWT_AUDIENCE
JWT_ACCESS_TOKEN_EXPIRATION_MINUTES=$JWT_ACCESS_TOKEN_EXPIRATION_MINUTES
JWT_REFRESH_TOKEN_EXPIRATION_DAYS=$JWT_REFRESH_TOKEN_EXPIRATION_DAYS

# Redis Configuration
REDIS_HOST=$REDIS_HOST
REDIS_PORT=$REDIS_PORT
REDIS_PASSWORD=$REDIS_PASSWORD

# MinIO Configuration
MINIO_ENDPOINT=$MINIO_ENDPOINT
MINIO_ACCESS_KEY=$MINIO_ACCESS_KEY
MINIO_SECRET_KEY=$MINIO_SECRET_KEY
MINIO_USE_SSL=$MINIO_USE_SSL
MINIO_BUCKET_NAME=$MINIO_BUCKET_NAME
MINIO_CONSOLE_PORT=$MINIO_CONSOLE_PORT

# Stripe Configuration (Replace with your actual keys)
STRIPE_PUBLISHABLE_KEY=$STRIPE_PUBLISHABLE_KEY
STRIPE_SECRET_KEY=$STRIPE_SECRET_KEY
STRIPE_WEBHOOK_SECRET=$STRIPE_WEBHOOK_SECRET

# Application Configuration
API_BASE_URL=$API_BASE_URL
CLIENT_BASE_URL=$CLIENT_BASE_URL
ENVIRONMENT=$ENVIRONMENT

# Docker Internal Network Configuration (for container communication)
DOCKER_POSTGRES_HOST=$DOCKER_POSTGRES_HOST
DOCKER_REDIS_HOST=$DOCKER_REDIS_HOST
DOCKER_MINIO_HOST=$DOCKER_MINIO_HOST

# SSL Certificate Configuration
CERT_PASSWORD=$CERT_PASSWORD
EOF

echo "✅ Configuration saved to docker/.env file"
echo ""

# Load the newly created/updated environment variables
echo "📖 Loading environment variables..."
set -a
source docker/.env
set +a

# Generate config.json from template
echo "📝 Generating configuration files..."

# Determine deployment mode and Docker flag
DEPLOYMENT_MODE="development"
IS_DOCKER="false"

# Local development configuration
API_CONFIG_PATH="src/Api/ClubManagement.Api/config.json"

if [ -f "src/Api/ClubManagement.Api/config.sample.json" ]; then
    echo "   Creating API config.json..."
    
    # Read template and replace placeholders
    sed "s|{{DB_HOST}}|${POSTGRES_HOST:-localhost}|g; \
         s|{{DB_PORT}}|${POSTGRES_PORT:-4004}|g; \
         s|{{DB_NAME}}|${POSTGRES_DB:-clubmanagement}|g; \
         s|{{DB_USER}}|${POSTGRES_USER:-clubadmin}|g; \
         s|{{DB_PASSWORD}}|${POSTGRES_PASSWORD:-clubpassword}|g; \
         s|{{JWT_SECRET_KEY}}|${JWT_SECRET_KEY:-YourSuperSecretKeyThatShouldBeAtLeast256BitsLongForHmacSha256Algorithm}|g; \
         s|{{JWT_ISSUER}}|${JWT_ISSUER:-ClubManagement}|g; \
         s|{{JWT_AUDIENCE}}|${JWT_AUDIENCE:-ClubManagement}|g; \
         s|{{JWT_ACCESS_TOKEN_EXPIRATION_MINUTES}}|${JWT_ACCESS_TOKEN_EXPIRATION_MINUTES:-60}|g; \
         s|{{JWT_REFRESH_TOKEN_EXPIRATION_DAYS}}|${JWT_REFRESH_TOKEN_EXPIRATION_DAYS:-7}|g; \
         s|{{REDIS_HOST}}|${REDIS_HOST:-localhost}|g; \
         s|{{REDIS_PORT}}|${REDIS_PORT:-4007}|g; \
         s|{{REDIS_PASSWORD}}|${REDIS_PASSWORD:-}|g; \
         s|{{MINIO_ENDPOINT}}|${MINIO_ENDPOINT:-localhost:4005}|g; \
         s|{{MINIO_ACCESS_KEY}}|${MINIO_ACCESS_KEY:-minioadmin}|g; \
         s|{{MINIO_SECRET_KEY}}|${MINIO_SECRET_KEY:-minioadmin}|g; \
         s|{{MINIO_USE_SSL}}|${MINIO_USE_SSL:-false}|g; \
         s|{{MINIO_BUCKET_NAME}}|${MINIO_BUCKET_NAME:-clubmanagement}|g; \
         s|{{STRIPE_PUBLISHABLE_KEY}}|${STRIPE_PUBLISHABLE_KEY:-}|g; \
         s|{{STRIPE_SECRET_KEY}}|${STRIPE_SECRET_KEY:-}|g; \
         s|{{STRIPE_WEBHOOK_SECRET}}|${STRIPE_WEBHOOK_SECRET:-}|g; \
         s|{{ENVIRONMENT}}|${ENVIRONMENT:-Development}|g; \
         s|{{API_BASE_URL}}|${API_BASE_URL:-http://localhost:4000}|g; \
         s|{{CLIENT_BASE_URL}}|${CLIENT_BASE_URL:-http://localhost:4002}|g; \
         s|{{DEPLOYMENT_MODE}}|${DEPLOYMENT_MODE}|g; \
         s|{{IS_DOCKER}}|${IS_DOCKER}|g" \
         src/Api/ClubManagement.Api/config.sample.json > "$API_CONFIG_PATH"
    
    echo "   ✅ API config.json created"
else
    echo "   ⚠️  config.sample.json not found, skipping config generation"
fi

echo "🔧 Building .NET projects..."
dotnet restore
dotnet build

echo "🐳 Starting Docker services..."
echo "   - PostgreSQL on port ${POSTGRES_PORT:-4004}"
echo "   - Redis on port ${REDIS_PORT:-4007}" 
echo "   - MinIO on port ${MINIO_PORT:-4005} (console: ${MINIO_CONSOLE_PORT:-4006})"

# Start infrastructure services first (using docker folder)
cd docker && docker-compose up -d postgres redis minio && cd ..

echo "⏳ Waiting for services to be ready..."

# Wait for PostgreSQL
echo "   Waiting for PostgreSQL..."
timeout=60
while ! (cd docker && docker-compose exec postgres pg_isready -U ${POSTGRES_USER:-clubadmin} -d ${POSTGRES_DB:-clubmanagement}) &>/dev/null; do
    timeout=$((timeout - 1))
    if [ $timeout -eq 0 ]; then
        echo "❌ PostgreSQL failed to start within 60 seconds"
        exit 1
    fi
    sleep 1
done
echo "   ✅ PostgreSQL is ready"

# Wait for Redis
echo "   Waiting for Redis..."
timeout=30
while ! (cd docker && docker-compose exec redis redis-cli ping) &>/dev/null; do
    timeout=$((timeout - 1))
    if [ $timeout -eq 0 ]; then
        echo "❌ Redis failed to start within 30 seconds"
        exit 1
    fi
    sleep 1
done
echo "   ✅ Redis is ready"

# Wait for MinIO
echo "   Waiting for MinIO..."
timeout=30
while ! curl -s http://localhost:${MINIO_PORT:-4005}/minio/health/live &>/dev/null; do
    timeout=$((timeout - 1))
    if [ $timeout -eq 0 ]; then
        echo "❌ MinIO failed to start within 30 seconds"
        exit 1
    fi
    sleep 1
done
echo "   ✅ MinIO is ready"

# Create MinIO bucket
echo "🪣 Setting up MinIO bucket..."
cd docker
docker-compose exec minio mc alias set local http://localhost:9000 ${MINIO_ACCESS_KEY:-minioadmin} ${MINIO_SECRET_KEY:-minioadmin}
docker-compose exec minio mc mb local/${MINIO_BUCKET_NAME:-clubmanagement} --ignore-existing
docker-compose exec minio mc policy set public local/${MINIO_BUCKET_NAME:-clubmanagement}
cd ..
echo "   ✅ MinIO bucket '${MINIO_BUCKET_NAME:-clubmanagement}' created"

echo "🗃️ Database container is ready (clean PostgreSQL instance)"
echo "💡 Run database migrations after starting the API to initialize tables"

# Generate development certificates for HTTPS
if [ ! -f "$HOME/.aspnet/https/aspnetapp.pfx" ]; then
    echo "🔐 Generating development HTTPS certificates..."
    dotnet dev-certs https -ep ~/.aspnet/https/aspnetapp.pfx -p ${CERT_PASSWORD:-password}
    dotnet dev-certs https --trust
    echo "   ✅ HTTPS certificates generated"
fi

echo ""
echo "🎉 Setup completed successfully!"
echo ""
echo "📋 Service Information:"
echo "   🌐 API: http://localhost:4000 | https://localhost:4001"
echo "   🎨 Client: http://localhost:4002 | https://localhost:4003"
echo "   🗄️  PostgreSQL: localhost:${POSTGRES_PORT:-4004}"
echo "   📦 Redis: localhost:${REDIS_PORT:-4007}"
echo "   💾 MinIO: http://localhost:${MINIO_PORT:-4005} (Console: http://localhost:${MINIO_CONSOLE_PORT:-4006})"
echo ""
echo "🚀 To start the applications:"
echo "   1. Start API: cd src/Api/ClubManagement.Api && dotnet run"
echo "   2. Run database migrations on first startup to initialize tables"
echo "   3. Start Client: cd src/Client/ClubManagement.Client && dotnet run"
echo "   Or use Docker: cd docker && docker-compose up api client"
echo ""
echo "🛑 To stop services: cd docker && docker-compose down"
echo "🗑️  To reset data: cd docker && docker-compose down -v"
echo "🐳 To run full Docker: cd docker && docker-compose up -d"