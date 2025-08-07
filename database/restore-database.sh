#!/bin/bash

# Restore ClubManagement database from backup
# Restores a database from a SQL backup file

# Source database configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/db-config.sh"

# Function to restore database
restore_database() {
    local database="$1"
    local backup_file="$2"
    
    echo -e "${BLUE}🔄 Restoring database: $database${NC}"
    echo -e "${BLUE}📁 From backup: $backup_file${NC}"
    
    # Check if backup file exists
    if [[ ! -f "$backup_file" ]]; then
        echo -e "${RED}❌ Backup file not found: $backup_file${NC}"
        return 1
    fi
    
    # Check if it's a compressed file
    if [[ "$backup_file" =~ \.gz$ ]]; then
        echo -e "${BLUE}📦 Detected compressed backup, decompressing...${NC}"
        temp_file="/tmp/$(basename "$backup_file" .gz)"
        gunzip -c "$backup_file" > "$temp_file"
        backup_file="$temp_file"
    fi
    
    # Terminate existing connections to the database if it exists
    if execute_sql "SELECT 1 FROM pg_database WHERE datname = '$database';" postgres | grep -q "1"; then
        echo -e "${YELLOW}🔌 Terminating existing connections to $database...${NC}"
        execute_sql "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$database';" postgres > /dev/null
    fi
    
    # Restore database
    echo -e "${BLUE}🚀 Starting restore process...${NC}"
    
    if command -v docker &> /dev/null && docker ps | grep -q "$DOCKER_POSTGRES_CONTAINER"; then
        # Restore via Docker
        if docker exec -i "$DOCKER_POSTGRES_CONTAINER" psql -U "$DB_USER" -d postgres < "$backup_file"; then
            echo -e "${GREEN}✅ Database restored successfully${NC}"
        else
            echo -e "${RED}❌ Restore failed${NC}"
            # Clean up temp file if created
            [[ -n "$temp_file" ]] && rm -f "$temp_file"
            return 1
        fi
    elif command -v psql &> /dev/null; then
        # Restore via direct psql
        if PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres < "$backup_file"; then
            echo -e "${GREEN}✅ Database restored successfully${NC}"
        else
            echo -e "${RED}❌ Restore failed${NC}"
            # Clean up temp file if created
            [[ -n "$temp_file" ]] && rm -f "$temp_file"
            return 1
        fi
    else
        echo -e "${RED}❌ Neither docker nor psql available${NC}"
        # Clean up temp file if created
        [[ -n "$temp_file" ]] && rm -f "$temp_file"
        return 1
    fi
    
    # Clean up temp file if created
    [[ -n "$temp_file" ]] && rm -f "$temp_file"
    
    # Verify restoration
    if execute_sql "SELECT 1 FROM pg_database WHERE datname = '$database';" postgres | grep -q "1"; then
        echo -e "${GREEN}✅ Database verification successful${NC}"
        
        # Show database size
        size=$(execute_sql "SELECT pg_size_pretty(pg_database_size('$database'));" postgres | grep -v "pg_size_pretty" | tr -d ' ')
        echo -e "${BLUE}📊 Database size: $size${NC}"
    else
        echo -e "${RED}❌ Database verification failed${NC}"
        return 1
    fi
}

# Function to show usage
show_usage() {
    echo "Usage: $0 <database_name> <backup_file>"
    echo ""
    echo "Arguments:"
    echo "  database_name    Name of the database to restore"
    echo "  backup_file      Path to the SQL backup file (.sql or .sql.gz)"
    echo ""
    echo "Examples:"
    echo "  $0 clubmanagement_catalog ./backups/clubmanagement_catalog_20240807_143022.sql"
    echo "  $0 clubmanagement_demo_club /path/to/backup.sql.gz"
    echo ""
    echo "Note: If the database already exists, it will be dropped and recreated"
}

# Function to list available backups
list_backups() {
    local backup_dir="$SCRIPT_DIR/backups"
    
    echo -e "${BLUE}📋 Available backup files:${NC}"
    if [[ -d "$backup_dir" ]]; then
        find "$backup_dir" -name "*.sql" -o -name "*.sql.gz" | sort | while read -r backup; do
            size=$(du -h "$backup" | cut -f1)
            echo "  📁 $(basename "$backup") ($size)"
        done
    else
        echo "  No backup directory found"
    fi
}

echo -e "${BLUE}🔄 ClubManagement Database Restore${NC}"
echo "==================================="
echo ""

# Test connection first
if ! test_connection; then
    echo -e "${RED}❌ Cannot connect to database server${NC}"
    exit 1
fi

# Parse arguments
if [[ $# -lt 2 ]] || [[ "$1" == "--help" ]] || [[ "$1" == "-h" ]]; then
    show_usage
    echo ""
    list_backups
    exit 0
fi

database="$1"
backup_file="$2"

# Convert relative path to absolute if needed
if [[ ! "$backup_file" =~ ^/ ]]; then
    backup_file="$(pwd)/$backup_file"
fi

echo -e "${BLUE}🎯 Restore Configuration:${NC}"
echo -e "Database: ${GREEN}$database${NC}"
echo -e "Backup File: ${GREEN}$backup_file${NC}"
echo ""

# Confirmation prompt
echo -e "${YELLOW}⚠️  WARNING: This will replace the existing '$database' database!${NC}"
echo -e "${YELLOW}   All current data in '$database' will be lost!${NC}"
echo ""
read -p "Are you sure you want to continue? Type 'YES' to confirm: " confirmation

if [[ "$confirmation" != "YES" ]]; then
    echo -e "${BLUE}ℹ️  Operation cancelled${NC}"
    exit 0
fi

echo ""
restore_database "$database" "$backup_file"

echo ""
echo -e "${BLUE}💡 Next steps:${NC}"
echo "• Verify the restored data by running the application"
echo "• Check that all tenant databases are properly restored"
echo "• Run any necessary database migrations if schema has changed"