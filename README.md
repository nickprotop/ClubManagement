# 🏊 Club Management Platform

A comprehensive, multi-tenant sport club management SAAS platform built with .NET 9, Blazor WASM, and modern web technologies.

## 🚀 Features

### 🏢 **Multi-Tenant Architecture**
- **Schema-per-tenant** isolation for complete data separation
- **Custom domains** and branding per tenant
- **Flexible subscription plans** with usage limits

### 👥 **Member Management**
- Member registration and profile management
- Membership tiers and renewal tracking
- Emergency contacts and medical information
- Custom fields per tenant

### 🏟️ **Dynamic Facility Management**
- **Custom facility types** with configurable properties
- Real-time availability and booking system
- Operating hours and capacity management
- Maintenance scheduling

### 🔧 **Hardware Management**
- **Custom equipment categories** with dynamic attributes
- Member equipment assignments with tracking
- Maintenance logs and replacement planning
- Usage history and analytics

### 📅 **Event & Class Scheduling**
- Recurring classes and one-time events
- Staff can register members for classes
- Capacity management and waitlists
- Instructor assignments

### 💳 **Payment Processing**
- **Stripe integration** for subscriptions and one-time payments
- Automated billing and invoice generation
- Refund processing and payment tracking

### 📱 **Modern UI/UX**
- **Blazor WASM** with **MudBlazor** Material Design components
- **Progressive Web App (PWA)** ready
- Responsive design for all devices
- Real-time updates and notifications

## 🛠️ Technology Stack

### **Backend (.NET 9)**
- **ASP.NET Core Web API** with JWT authentication
- **Entity Framework Core** with PostgreSQL
- **Clean Architecture** (Domain, Application, Infrastructure)
- **MediatR** for CQRS pattern
- **FluentValidation** for input validation

### **Frontend (Blazor WASM)**
- **MudBlazor** UI component library
- **Progressive Web App** capabilities
- **Real-time** communication ready
- **Responsive** Material Design

### **Infrastructure**
- **PostgreSQL** with schema-per-tenant multi-tenancy
- **Redis** for caching and session management
- **MinIO** for file storage (S3-compatible)
- **Docker** containerization with docker-compose

### **External Services**
- **Stripe** for payment processing
- **Email/SMS** notifications (configurable)

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Blazor WASM   │    │   ASP.NET API   │    │   PostgreSQL    │
│   (Port 4002/3) │◄──►│   (Port 4000/1) │◄──►│   (Port 4004)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       │
┌─────────────────┐    ┌─────────────────┐              │
│      Redis      │    │      MinIO      │              │
│   (Port 4007)   │    │   (Port 4005/6) │              │
└─────────────────┘    └─────────────────┘              │
                                                         │
                              Schema per Tenant ─────────┘
                              ├── tenant1_schema
                              ├── tenant2_schema
                              └── demo_club (demo)
```

## 🚦 Quick Start

### **Prerequisites**
- **.NET 9 SDK** 
- **Docker & Docker Compose**
- **Git**

### **1. Clone and Setup**
```bash
git clone <repository-url>
cd ClubManagement

# Interactive setup (recommended)
./scripts/setup.sh

# OR quick setup with defaults (for development)
./scripts/setup.sh --quick
```

### **Setup Options**

**🔧 Interactive Setup (Default)**
- Prompts for database, Redis, MinIO passwords
- Uses existing `.env` values as defaults if present  
- Generates secure JWT key automatically
- Best for production or when you need custom configuration

**🚀 Quick Setup (`--quick`)**
- Uses default values from `.env.sample`
- Uses existing `.env` values if present
- No interactive prompts
- Perfect for development and testing

The setup script will:
- ✅ Check prerequisites (.NET 9, Docker, Docker Compose)
- 🔧 **Interactive configuration** for passwords and settings
- ⚙️ **Generate `config.json`** from template using `.env` values
- 🐳 Start Docker services (PostgreSQL, Redis, MinIO)
- 📊 Initialize database with demo data
- 🔐 Generate HTTPS certificates

### **1.1. Security Configuration (Optional but Recommended)**
For production or enhanced security, generate a secure JWT key:
```bash
# Generate a cryptographically secure JWT secret
./scripts/generate-jwt-secret.sh

# This will optionally update your .env file automatically
```

### **2. Start Development**

**Option A: Local Development** (Recommended)
```bash
# Start infrastructure only (PostgreSQL, Redis, MinIO)
./scripts/start-infra.sh

# Run API locally (Terminal 1)
cd src/Api/ClubManagement.Api
dotnet run

# Run Client locally (Terminal 2)  
cd src/Client/ClubManagement.Client
dotnet run
```

**Option B: Full Docker**
```bash
# Start everything in containers
docker-compose up -d
```

### **3. Access the Platform**
- 🌐 **API**: https://localhost:4001
- 🎨 **Client**: https://localhost:4003
- 🗄️ **PostgreSQL**: localhost:4004
- 📦 **Redis**: localhost:4007
- 💾 **MinIO Console**: http://localhost:4006

### **4. Demo Access**
- **Domain**: `demo.localhost`
- **Admin Email**: `admin@demo.localhost`
- **Schema**: `demo_club`

## 📁 Project Structure

```
ClubManagement/
├── src/
│   ├── Shared/                    # Shared models and DTOs
│   ├── Domain/                    # Domain entities and interfaces
│   ├── Application/               # Business logic and CQRS
│   ├── Infrastructure/            # Data access and external services
│   ├── Api/                       # ASP.NET Core Web API
│   │   └── config.sample.json     # Configuration template
│   └── Client/                    # Blazor WASM frontend
├── scripts/
│   ├── setup.sh                   # Main setup script
│   ├── generate-docker-config.sh  # Docker configuration generator
│   └── init-db.sql               # Database initialization
├── docker-compose.yml             # Docker services configuration
├── .env.sample                    # Environment variables template
└── README.md
```

## ⚙️ Configuration

### **Configuration Workflow**
1. **📋 Template**: `.env.sample` contains all required environment variables
2. **📝 Generation**: `setup.sh` copies `.env.sample` → `.env` (first run only)
3. **✏️ Customization**: Edit `.env` with your specific values
4. **⚙️ Build**: Script generates `config.json` from template using `.env` values
5. **🚀 Runtime**: Application loads `config.json` for all configuration

### **Environment Variables (.env)**
```bash
# Database Configuration
POSTGRES_HOST=localhost
POSTGRES_PORT=4004
POSTGRES_DB=clubmanagement
POSTGRES_USER=clubadmin
POSTGRES_PASSWORD=clubpassword

# JWT Authentication
JWT_SECRET_KEY=YourSuperSecretKey...
JWT_ISSUER=ClubManagement
JWT_AUDIENCE=ClubManagement

# External Services
REDIS_HOST=localhost
REDIS_PORT=4007
MINIO_ENDPOINT=localhost:4005
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...

# Application URLs
API_BASE_URL=https://localhost:4001
CLIENT_BASE_URL=https://localhost:4003
```

### **Configuration Files**
- **`.env.sample`** → Template with default values
- **`.env`** → Your actual configuration (git-ignored)
- **`config.sample.json`** → Template with placeholders like `{{DB_HOST}}`
- **`config.json`** → Generated configuration (git-ignored)

## 🐳 Docker Deployment

### **Development**
```bash
docker-compose up -d
```

### **Production**
1. Update `.env` with production values
2. Update Stripe keys and JWT secrets
3. Configure custom domain and SSL certificates
4. Deploy with:
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## 🗄️ Database Schema

### **Multi-Tenant Design**
- **Public Schema**: Tenant management and authentication
- **Tenant Schemas**: Isolated data per tenant (e.g., `demo_club`, `tenant_abc`)

### **Key Tables per Tenant**
- `users` - User accounts and profiles
- `members` - Club member information
- `facility_types` - Custom facility categories
- `facilities` - Facility instances with dynamic properties
- `hardware_types` - Equipment categories  
- `hardware` - Equipment instances with assignments
- `events` - Classes and events
- `payments` - Stripe payment records

## 🔧 Development

### **Adding New Features**
1. **Domain**: Add entities to `src/Domain/`
2. **Application**: Add commands/queries to `src/Application/`
3. **Infrastructure**: Add data access to `src/Infrastructure/`
4. **API**: Add controllers to `src/Api/Controllers/`
5. **Client**: Add pages/components to `src/Client/`

### **Database Migrations**
```bash
# Add migration
dotnet ef migrations add MigrationName --project src/Infrastructure/ClubManagement.Infrastructure --startup-project src/Api/ClubManagement.Api

# Update database
dotnet ef database update --project src/Infrastructure/ClubManagement.Infrastructure --startup-project src/Api/ClubManagement.Api
```

### **Testing**
```bash
# Build and test
dotnet build
dotnet test
```

## 🔒 Security Features

- **JWT Authentication** with refresh tokens
- **Multi-tenant data isolation** via database schemas
- **CORS protection** with configurable origins
- **Input validation** with FluentValidation
- **SQL injection protection** with parameterized queries
- **HTTPS enforcement** in production

## 📊 Monitoring & Health Checks

All services include health check endpoints:
- **API**: `/health`
- **PostgreSQL**: Built-in health checks
- **Redis**: `redis-cli ping`
- **MinIO**: `/minio/health/live`

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- 📧 **Email**: support@clubmanagement.com
- 💬 **GitHub Issues**: For bug reports and feature requests
- 📖 **Documentation**: Check the `/docs` folder for detailed guides

---

Built with ❤️ using .NET 9, Blazor WASM, and MudBlazor