#!/bin/bash

# Club Management Platform - Infrastructure Startup Script
# Starts only infrastructure services (PostgreSQL, Redis, MinIO) for local development

set -e  # Exit on any error

echo "🏊 Club Management Platform - Infrastructure Services"
echo "=================================================="

# Show usage if help requested
if [[ "$1" == "--help" ]] || [[ "$1" == "-h" ]]; then
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "This script starts only the infrastructure services (PostgreSQL, Redis, MinIO)"
    echo "allowing you to run the API and Frontend locally with 'dotnet run'."
    echo ""
    echo "Options:"
    echo "  start, up           Start infrastructure services (default)"
    echo "  stop, down          Stop infrastructure services" 
    echo "  restart             Restart infrastructure services"
    echo "  status, ps          Show service status"
    echo "  logs [service]      Show logs (optionally for specific service)"
    echo "  reset               Stop services and remove volumes (WARNING: deletes data)"
    echo "  -h, --help          Show this help message"
    echo ""
    echo "Infrastructure Services:"
    echo "  🗄️  PostgreSQL - Database server"
    echo "  📦 Redis       - Caching and session storage"
    echo "  💾 MinIO       - S3-compatible file storage"
    echo ""
    echo "Examples:"
    echo "  $0                  # Start infrastructure services"
    echo "  $0 start            # Start infrastructure services"
    echo "  $0 stop             # Stop infrastructure services"
    echo "  $0 logs postgres    # Show PostgreSQL logs"
    echo "  $0 status           # Show service status"
    echo ""
    echo "After starting infrastructure, run your applications locally:"
    echo "  API:    cd src/Api/ClubManagement.Api && dotnet run"
    echo "  Client: cd src/Client/ClubManagement.Client && dotnet run"
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

# Check if .env file exists in docker folder
if [ ! -f "docker/.env" ]; then
    echo "❌ docker/.env file not found. Please run './scripts/setup.sh' first."
    exit 1
fi

# Load environment variables
echo "📖 Loading environment variables..."
set -a
source docker/.env
set +a

# Define services
SERVICES="postgres redis minio"

# Function to start services
start_services() {
    echo "🚀 Starting infrastructure services..."
    echo "   🗄️  PostgreSQL on port ${POSTGRES_PORT:-4004}"
    echo "   📦 Redis on port ${REDIS_PORT:-4007}"
    echo "   💾 MinIO on port ${MINIO_ENDPOINT:-localhost:4005} (console: ${MINIO_CONSOLE_PORT:-4006})"
    echo ""
    
    cd docker
    docker-compose up -d $SERVICES
    cd ..
    
    echo "⏳ Waiting for services to be ready..."
    
    # Wait for PostgreSQL
    echo "   Checking PostgreSQL..."
    timeout=60
    while ! (cd docker && docker-compose exec postgres pg_isready -U ${POSTGRES_USER:-clubadmin} -d ${POSTGRES_DB:-clubmanagement}) &>/dev/null; do
        timeout=$((timeout - 1))
        if [ $timeout -eq 0 ]; then
            echo "   ❌ PostgreSQL failed to start within 60 seconds"
            show_logs "postgres"
            exit 1
        fi
        sleep 1
    done
    echo "   ✅ PostgreSQL is ready"
    
    # Wait for Redis
    echo "   Checking Redis..."
    timeout=30
    while ! (cd docker && docker-compose exec redis redis-cli ping) &>/dev/null; do
        timeout=$((timeout - 1))
        if [ $timeout -eq 0 ]; then
            echo "   ❌ Redis failed to start within 30 seconds"
            show_logs "redis"
            exit 1
        fi
        sleep 1
    done
    echo "   ✅ Redis is ready"
    
    # Wait for MinIO
    echo "   Checking MinIO..."
    timeout=30
    minio_port=$(echo ${MINIO_ENDPOINT:-localhost:4005} | cut -d':' -f2)
    while ! curl -s http://localhost:${minio_port}/minio/health/live &>/dev/null; do
        timeout=$((timeout - 1))
        if [ $timeout -eq 0 ]; then
            echo "   ❌ MinIO failed to start within 30 seconds"
            show_logs "minio"
            exit 1
        fi
        sleep 1
    done
    echo "   ✅ MinIO is ready"
    
    # Setup MinIO bucket if not exists
    echo "🪣 Ensuring MinIO bucket exists..."
    cd docker
    if docker-compose exec minio mc alias set local http://localhost:9000 ${MINIO_ACCESS_KEY:-minioadmin} ${MINIO_SECRET_KEY:-minioadmin} &>/dev/null; then
        docker-compose exec minio mc mb local/${MINIO_BUCKET_NAME:-clubmanagement} --ignore-existing &>/dev/null || true
        docker-compose exec minio mc policy set public local/${MINIO_BUCKET_NAME:-clubmanagement} &>/dev/null || true
        echo "   ✅ MinIO bucket '${MINIO_BUCKET_NAME:-clubmanagement}' ready"
    else
        echo "   ⚠️  Could not configure MinIO bucket (MinIO may still be starting)"
    fi
    cd ..
    
    echo ""
    echo "🎉 Infrastructure services are ready!"
    echo ""
    echo "📋 Service Access:"
    echo "   🗄️  PostgreSQL: localhost:${POSTGRES_PORT:-4004}"
    echo "      Database: ${POSTGRES_DB:-clubmanagement}"
    echo "      Username: ${POSTGRES_USER:-clubadmin}"
    echo ""
    echo "   📦 Redis: localhost:${REDIS_PORT:-4007}"
    if [ -n "${REDIS_PASSWORD}" ]; then
        echo "      Password: ${REDIS_PASSWORD}"
    else
        echo "      No password required"
    fi
    echo ""
    echo "   💾 MinIO: http://localhost:${minio_port:-4005}"
    echo "      Console: http://localhost:${MINIO_CONSOLE_PORT:-4006}"
    echo "      Access Key: ${MINIO_ACCESS_KEY:-minioadmin}"
    echo "      Secret Key: ${MINIO_SECRET_KEY:-minioadmin}"
    echo "      Bucket: ${MINIO_BUCKET_NAME:-clubmanagement}"
    echo ""
    echo "🚀 Ready to start your applications:"
    echo "   API:    cd src/Api/ClubManagement.Api && dotnet run"
    echo "   Client: cd src/Client/ClubManagement.Client && dotnet run"
    echo ""
}

# Function to stop services
stop_services() {
    echo "🛑 Stopping infrastructure services..."
    cd docker
    docker-compose stop $SERVICES
    cd ..
    echo "✅ Infrastructure services stopped"
}

# Function to show service status
show_status() {
    echo "📊 Infrastructure Service Status:"
    cd docker
    docker-compose ps $SERVICES
    cd ..
}

# Function to show logs
show_logs() {
    local service=$1
    cd docker
    if [ -n "$service" ]; then
        if [[ "$SERVICES" =~ $service ]]; then
            echo "📋 Showing logs for $service:"
            docker-compose logs -f --tail=50 $service
        else
            echo "❌ Invalid service: $service"
            echo "Available services: $SERVICES"
            exit 1
        fi
    else
        echo "📋 Showing logs for all infrastructure services:"
        docker-compose logs -f --tail=20 $SERVICES
    fi
    cd ..
}

# Function to restart services
restart_services() {
    echo "🔄 Restarting infrastructure services..."
    stop_services
    sleep 2
    start_services
}

# Function to reset services (remove volumes)
reset_services() {
    echo "⚠️  WARNING: This will delete all data in PostgreSQL, Redis, and MinIO!"
    echo "Are you sure you want to continue? (yes/no)"
    read -r response
    if [[ "$response" == "yes" ]]; then
        echo "🗑️  Stopping and removing infrastructure services with data..."
        cd docker
        docker-compose down $SERVICES
        docker-compose down -v  # Remove volumes
        cd ..
        echo "✅ Infrastructure services reset (all data deleted)"
        echo ""
        echo "💡 To start fresh, run: $0 start"
    else
        echo "❌ Reset cancelled"
    fi
}

# Parse command
command=${1:-start}

case $command in
    "start"|"up"|"")
        start_services
        ;;
    "stop"|"down")
        stop_services
        ;;
    "restart")
        restart_services
        ;;
    "status"|"ps")
        show_status
        ;;
    "logs")
        show_logs $2
        ;;
    "reset")
        reset_services
        ;;
    *)
        echo "❌ Unknown command: $command"
        echo "Run '$0 --help' for usage information"
        exit 1
        ;;
esac