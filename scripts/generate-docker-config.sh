#!/bin/bash

# Generate Docker-specific config.json
# This script is called by docker-compose or can be run separately for Docker deployment

set -e

echo "🐳 Generating Docker configuration..."

# Load environment variables
if [ -f ".env" ]; then
    set -a
    source .env
    set +a
fi

# Docker deployment configuration
DEPLOYMENT_MODE="docker"
IS_DOCKER="true"

# Docker uses internal service names
DOCKER_DB_HOST="${DOCKER_POSTGRES_HOST:-postgres}"
DOCKER_REDIS_HOST_VAR="${DOCKER_REDIS_HOST:-redis}"
DOCKER_MINIO_HOST_VAR="${DOCKER_MINIO_HOST:-minio}"

API_CONFIG_PATH="src/Api/ClubManagement.Api/config.json"

if [ -f "src/Api/ClubManagement.Api/config.sample.json" ]; then
    echo "   Creating Docker API config.json..."
    
    # Read template and replace placeholders with Docker values
    sed "s|{{DB_HOST}}|${DOCKER_DB_HOST}|g; \
         s|{{DB_PORT}}|5432|g; \
         s|{{DB_NAME}}|${POSTGRES_DB:-clubmanagement}|g; \
         s|{{DB_USER}}|${POSTGRES_USER:-clubadmin}|g; \
         s|{{DB_PASSWORD}}|${POSTGRES_PASSWORD:-clubpassword}|g; \
         s|{{JWT_SECRET_KEY}}|${JWT_SECRET_KEY:-YourSuperSecretKeyThatShouldBeAtLeast256BitsLongForHmacSha256Algorithm}|g; \
         s|{{JWT_ISSUER}}|${JWT_ISSUER:-ClubManagement}|g; \
         s|{{JWT_AUDIENCE}}|${JWT_AUDIENCE:-ClubManagement}|g; \
         s|{{JWT_ACCESS_TOKEN_EXPIRATION_MINUTES}}|${JWT_ACCESS_TOKEN_EXPIRATION_MINUTES:-60}|g; \
         s|{{JWT_REFRESH_TOKEN_EXPIRATION_DAYS}}|${JWT_REFRESH_TOKEN_EXPIRATION_DAYS:-7}|g; \
         s|{{REDIS_HOST}}|${DOCKER_REDIS_HOST_VAR}|g; \
         s|{{REDIS_PORT}}|6379|g; \
         s|{{REDIS_PASSWORD}}|${REDIS_PASSWORD:-}|g; \
         s|{{MINIO_ENDPOINT}}|${DOCKER_MINIO_HOST_VAR}:9000|g; \
         s|{{MINIO_ACCESS_KEY}}|${MINIO_ACCESS_KEY:-minioadmin}|g; \
         s|{{MINIO_SECRET_KEY}}|${MINIO_SECRET_KEY:-minioadmin}|g; \
         s|{{MINIO_USE_SSL}}|${MINIO_USE_SSL:-false}|g; \
         s|{{MINIO_BUCKET_NAME}}|${MINIO_BUCKET_NAME:-clubmanagement}|g; \
         s|{{STRIPE_PUBLISHABLE_KEY}}|${STRIPE_PUBLISHABLE_KEY:-}|g; \
         s|{{STRIPE_SECRET_KEY}}|${STRIPE_SECRET_KEY:-}|g; \
         s|{{STRIPE_WEBHOOK_SECRET}}|${STRIPE_WEBHOOK_SECRET:-}|g; \
         s|{{ENVIRONMENT}}|${ENVIRONMENT:-Development}|g; \
         s|{{API_BASE_URL}}|http://api:80|g; \
         s|{{CLIENT_BASE_URL}}|http://client:80|g; \
         s|{{DEPLOYMENT_MODE}}|${DEPLOYMENT_MODE}|g; \
         s|{{IS_DOCKER}}|${IS_DOCKER}|g" \
         src/Api/ClubManagement.Api/config.sample.json > "$API_CONFIG_PATH"
    
    echo "   ✅ Docker API config.json created"
else
    echo "   ❌ config.sample.json not found!"
    exit 1
fi

echo "🐳 Docker configuration generated successfully!"