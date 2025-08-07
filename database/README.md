# ClubManagement Database Scripts

This directory contains useful database management scripts for the ClubManagement platform. These scripts automatically read configuration from your `config.json` file or fall back to environment defaults.

## 📋 Available Scripts

| Script | Purpose | Usage |
|--------|---------|-------|
| `db-config.sh` | Database configuration reader | Sources database settings from config |
| `list-databases.sh` | List all ClubManagement databases | `./list-databases.sh` |
| `drop-all-databases.sh` | Drop all ClubManagement databases | `./drop-all-databases.sh [--force]` |
| `backup-database.sh` | Backup databases | `./backup-database.sh [database\|--all]` |
| `restore-database.sh` | Restore from backup | `./restore-database.sh <database> <backup-file>` |
| `reset-demo-data.sh` | Reset demo data | `./reset-demo-data.sh [--force]` |
| `db-health-check.sh` | Comprehensive health check | `./db-health-check.sh` |

## 🚀 Quick Start

```bash
# Make all scripts executable
chmod +x *.sh

# Check database health
./db-health-check.sh

# List all databases
./list-databases.sh

# Reset demo data (most common operation)
./reset-demo-data.sh
```

## 📊 Database Architecture

The ClubManagement platform uses a **database-per-tenant** architecture:

- **Catalog Database** (`clubmanagement_catalog`): Contains tenant registry and shared infrastructure
- **Tenant Databases** (`clubmanagement_<tenant>`): Each tenant gets an isolated database
- **Demo Database** (`clubmanagement_demo_club`): Demo tenant with sample data

## 🔧 Configuration

Scripts automatically read database configuration from:

1. **Primary**: `src/Api/ClubManagement.Api/config.json` (generated from environment)
2. **Fallback**: Environment variables with defaults:
   - `DB_HOST` (default: `localhost`)
   - `DB_PORT` (default: `4004`)
   - `DB_USER` (default: `clubadmin`)
   - `DB_PASSWORD` (default: `clubpassword`)
   - Docker container: `clubmanagement-postgres`

## 📖 Script Details

### `db-config.sh`
Core configuration script that other scripts source. Provides:
- Database connection parameters
- Connection testing functions
- SQL execution wrappers for Docker/direct psql

### `list-databases.sh`
Shows all ClubManagement databases with:
- Database sizes
- Active connections
- Database type (Demo vs Production)
- Tenant database breakdown

### `drop-all-databases.sh` ⚠️
**DESTRUCTIVE**: Drops ALL ClubManagement databases
- Terminates active connections
- Drops catalog and all tenant databases
- Requires confirmation (unless `--force`)
- Use for complete cleanup

### `backup-database.sh`
Creates timestamped SQL backups:
```bash
# Backup single database
./backup-database.sh clubmanagement_demo_club

# Backup all databases
./backup-database.sh --all
```
- Stores backups in `./backups/` directory
- Uses `pg_dump` with `--clean --create` flags
- Shows file sizes and backup summary

### `restore-database.sh`
Restores databases from SQL backups:
```bash
./restore-database.sh clubmanagement_demo_club ./backups/clubmanagement_demo_club_20240807_143022.sql
```
- Supports compressed backups (`.sql.gz`)
- Terminates existing connections
- Verifies restoration success

### `reset-demo-data.sh`
Complete demo data reset:
- Drops demo tenant database
- Builds and runs API to trigger seeding
- Creates fresh demo data with proper status coordination
- Shows demo credentials after completion

### `db-health-check.sh`
Comprehensive health monitoring:
- **Connectivity**: Database server and connection tests
- **Structure**: Catalog/tenant database verification
- **Content**: Data presence and counts
- **Performance**: Database sizes, connections, long queries
- **Schema**: Table existence and foreign keys
- Color-coded results with summary

## 🔒 Security Notes

- Scripts never display passwords in output (marked as `[HIDDEN]`)
- Use `PGPASSWORD` environment variable for authentication
- Support both Docker and direct PostgreSQL connections
- Require explicit confirmation for destructive operations

## 🧪 Development Workflow

**Common development tasks:**

```bash
# 1. Fresh development start
./drop-all-databases.sh --force
./reset-demo-data.sh --force

# 2. Backup before major changes
./backup-database.sh --all

# 3. Check system health
./db-health-check.sh

# 4. Quick database overview
./list-databases.sh
```

## 🎯 Hardware Status Coordination

After recent updates, the demo data properly implements the two-tier hardware coordination system:

- **Manual Hardware Status**: Staff-controlled (`Available`, `Unavailable`, `Maintenance`, etc.)
- **Assignment Tracking**: Automatic via assignment records
- **Status Locking**: Hardware status cannot be changed when actively assigned
- **Clear Separation**: No confusion between operational status and usage tracking

Demo includes sample hardware with proper status coordination between manual status and assignment tracking.

## 🔍 Troubleshooting

**Connection issues:**
```bash
# Check Docker container
docker ps | grep postgres

# Test direct connection
./db-config.sh

# Verify configuration
cat src/Api/ClubManagement.Api/config.json
```

**Missing databases:**
```bash
# Check if infrastructure is running
./scripts/start-infra.sh

# Reset everything
./reset-demo-data.sh
```

**Backup/Restore issues:**
- Ensure sufficient disk space for backups
- Check file permissions on backup directory
- Verify PostgreSQL tools are available (pg_dump/psql)

## 📁 Directory Structure

```
database/
├── README.md                 # This file
├── db-config.sh              # Configuration reader
├── list-databases.sh         # Database listing
├── drop-all-databases.sh     # Database cleanup
├── backup-database.sh        # Backup utility
├── restore-database.sh       # Restore utility
├── reset-demo-data.sh        # Demo data reset
├── db-health-check.sh        # Health monitoring
└── backups/                  # Backup storage (auto-created)
```

## 🤝 Contributing

When adding new database scripts:
1. Source `db-config.sh` for consistent configuration
2. Use the established color scheme for output
3. Include proper error handling and confirmation prompts
4. Update this README with new script documentation
5. Test with both Docker and direct PostgreSQL connections