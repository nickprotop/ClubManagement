# 🐳 Docker Configuration

This folder contains all Docker-related configuration files for the Club Management Platform.

## 📁 Files Overview

### **Core Files**
- `docker-compose.yml` - Main Docker Compose configuration
- `Dockerfile.api` - Backend API container build
- `Dockerfile.client` - Frontend client container build  
- `nginx.conf` - Nginx configuration for client container

### **Environment-Specific**
- `docker-compose.override.yml` - Development overrides (auto-loaded)
- `docker-compose.prod.yml` - Production configuration

## 🚀 Usage

### **Development (Infrastructure Only)**
Start just the backend services for local development:
```bash
cd docker
docker-compose up -d postgres redis minio
```

### **Development (Full Stack)**
Run everything in containers:
```bash
cd docker
docker-compose up -d
```

### **Production Deployment**
```bash
cd docker
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## 🔧 Configuration

### **Environment Variables**
The Docker Compose files use environment variables from the `.env` file in this directory:

```bash
# Database
POSTGRES_HOST=localhost
POSTGRES_PORT=4004
POSTGRES_DB=clubmanagement
POSTGRES_USER=clubadmin
POSTGRES_PASSWORD=your_secure_password

# Redis  
REDIS_HOST=localhost
REDIS_PORT=4007
REDIS_PASSWORD=your_redis_password

# MinIO
MINIO_ENDPOINT=localhost:4005
MINIO_ACCESS_KEY=your_access_key
MINIO_SECRET_KEY=your_secret_key
```

### **Port Mapping**
- **PostgreSQL**: `4004:5432`
- **Redis**: `4007:6379`
- **MinIO**: `4005:9000` (API), `4006:9001` (Console)
- **API**: `4000:80` (HTTP), `4001:443` (HTTPS)
- **Client**: `4002:80` (HTTP), `4003:443` (HTTPS)

## 🏗️ Container Architecture

```
┌─────────────────┐    ┌─────────────────┐
│   Client        │    │   API           │
│   (Nginx)       │◄──►│   (.NET 9)      │
│   Port 4002/3   │    │   Port 4000/1   │
└─────────────────┘    └─────────────────┘
         │                       │
         └───────────────────────┼─────────────────┐
                                 │                 │
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   PostgreSQL    │    │   Redis         │    │   MinIO         │
│   Port 4004     │    │   Port 4007     │    │   Port 4005/6   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### **Internal Network**
All containers communicate via Docker internal network:
- API → PostgreSQL: `postgres:5432`
- API → Redis: `redis:6379`  
- API → MinIO: `minio:9000`

## 🔒 Security Features

### **Development**
- Non-root user for API container
- Health checks for all services
- Resource limits on containers
- Secure network isolation

### **Production** (`docker-compose.prod.yml`)
- SSL/TLS certificate mounting
- Resource limits and reservations
- Persistent data volumes  
- Production-optimized settings
- Password-protected Redis
- Enhanced PostgreSQL configuration

## 📊 Health Checks

All services include health checks:
```bash
# Check service health
cd docker
docker-compose ps

# View logs
docker-compose logs api
docker-compose logs client
docker-compose logs postgres
```

## 🛠️ Maintenance Commands

### **Start Services**
```bash
cd docker
docker-compose up -d
```

### **Stop Services**
```bash
cd docker
docker-compose down
```

### **Reset Data (Caution!)**
```bash
cd docker
docker-compose down -v  # Removes all data volumes
```

### **View Logs**
```bash
cd docker
docker-compose logs -f api     # Follow API logs
docker-compose logs -f client  # Follow client logs
```

### **Update Containers**
```bash
cd docker
docker-compose pull           # Pull latest images
docker-compose up -d --build  # Rebuild and restart
```

## 🔧 Customization

### **Adding SSL Certificates**
For production, place SSL certificates in `docker/ssl/`:
```
docker/ssl/
├── cert.pem
├── key.pem
└── ca.pem
```

### **Database Backups**
Production setup includes backup volume mapping:
```bash
cd docker
docker-compose exec postgres pg_dump -U clubadmin clubmanagement > backups/backup_$(date +%Y%m%d_%H%M%S).sql
```

### **Custom Configuration**
Create additional override files for specific environments:
```bash
# Staging environment
docker-compose -f docker-compose.yml -f docker-compose.staging.yml up -d
```

## 🚨 Troubleshooting

### **Container Won't Start**
```bash
cd docker
docker-compose logs [service-name]
docker-compose ps
```

### **Network Issues**
```bash
cd docker
docker network ls
docker network inspect docker_clubmanagement-network
```

### **Volume Issues**
```bash
cd docker
docker volume ls
docker volume inspect docker_postgres_data
```

### **Reset Everything**
```bash
cd docker
docker-compose down -v --rmi all
docker system prune -a
```