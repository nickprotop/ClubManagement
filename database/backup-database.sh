#!/bin/bash

# Backup ClubManagement databases
# Creates timestamped backups of specified database or all databases

# Source database configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/db-config.sh"

# Create backups directory if it doesn't exist
BACKUP_DIR="$SCRIPT_DIR/backups"
mkdir -p "$BACKUP_DIR"

# Function to backup a single database
backup_database() {
    local database="$1"
    local timestamp=$(date +"%Y%m%d_%H%M%S")
    local backup_file="$BACKUP_DIR/${database}_${timestamp}.sql"
    
    echo -e "${BLUE}📦 Backing up database: $database${NC}"
    
    # Check if database exists
    if ! execute_sql "SELECT 1 FROM pg_database WHERE datname = '$database';" postgres | grep -q "1"; then
        echo -e "${RED}❌ Database '$database' does not exist${NC}"
        return 1
    fi
    
    # Create backup
    if command -v docker &> /dev/null && docker ps | grep -q "$DOCKER_POSTGRES_CONTAINER"; then
        # Backup via Docker
        if docker exec -i "$DOCKER_POSTGRES_CONTAINER" pg_dump -U "$DB_USER" -d "$database" --clean --create > "$backup_file"; then
            echo -e "${GREEN}✅ Backup created: $backup_file${NC}"
            echo -e "${BLUE}📊 File size: $(du -h "$backup_file" | cut -f1)${NC}"
        else
            echo -e "${RED}❌ Backup failed for $database${NC}"
            rm -f "$backup_file"
            return 1
        fi
    elif command -v pg_dump &> /dev/null; then
        # Backup via direct pg_dump
        if PGPASSWORD="$DB_PASSWORD" pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$database" --clean --create > "$backup_file"; then
            echo -e "${GREEN}✅ Backup created: $backup_file${NC}"
            echo -e "${BLUE}📊 File size: $(du -h "$backup_file" | cut -f1)${NC}"
        else
            echo -e "${RED}❌ Backup failed for $database${NC}"
            rm -f "$backup_file"
            return 1
        fi
    else
        echo -e "${RED}❌ Neither docker nor pg_dump available${NC}"
        return 1
    fi
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [database_name|--all]"
    echo ""
    echo "Options:"
    echo "  database_name    Backup specific database"
    echo "  --all, -a        Backup all ClubManagement databases"
    echo "  --help, -h       Show this help"
    echo ""
    echo "Examples:"
    echo "  $0 clubmanagement_catalog"
    echo "  $0 clubmanagement_demo_club"
    echo "  $0 --all"
}

echo -e "${BLUE}💾 ClubManagement Database Backup${NC}"
echo "=================================="
echo ""

# Test connection first
if ! test_connection; then
    echo -e "${RED}❌ Cannot connect to database server${NC}"
    exit 1
fi

# Parse arguments
if [[ $# -eq 0 ]] || [[ "$1" == "--help" ]] || [[ "$1" == "-h" ]]; then
    show_usage
    exit 0
fi

if [[ "$1" == "--all" ]] || [[ "$1" == "-a" ]]; then
    echo -e "${BLUE}🔄 Backing up all ClubManagement databases...${NC}"
    
    # Get all ClubManagement databases
    databases=$(execute_sql "SELECT datname FROM pg_database WHERE datname LIKE 'clubmanagement%' ORDER BY datname;" postgres | grep clubmanagement | tr -d ' ')
    
    if [[ -z "$databases" ]]; then
        echo -e "${YELLOW}⚠️  No ClubManagement databases found${NC}"
        exit 0
    fi
    
    echo -e "${BLUE}📋 Found databases:${NC}"
    for db in $databases; do
        echo "  • $db"
    done
    echo ""
    
    # Backup each database
    success_count=0
    total_count=0
    
    for database in $databases; do
        ((total_count++))
        if backup_database "$database"; then
            ((success_count++))
        fi
        echo ""
    done
    
    echo -e "${BLUE}📊 Backup Summary:${NC}"
    echo -e "✅ Successful: $success_count/$total_count"
    
    if [[ $success_count -eq $total_count ]]; then
        echo -e "${GREEN}🎉 All backups completed successfully!${NC}"
    else
        echo -e "${YELLOW}⚠️  Some backups failed - please check the output above${NC}"
    fi
    
else
    # Backup single database
    database="$1"
    backup_database "$database"
fi

echo ""
echo -e "${BLUE}📁 Backup location: $BACKUP_DIR${NC}"
echo -e "${BLUE}📋 Available backups:${NC}"
ls -lah "$BACKUP_DIR"/*.sql 2>/dev/null || echo "  No backups found"

echo ""
echo -e "${BLUE}💡 Tips:${NC}"
echo "• Use './restore-database.sh <database> <backup-file>' to restore"
echo "• Backup files include database creation statements"
echo "• Consider compressing large backups: gzip backup_file.sql"