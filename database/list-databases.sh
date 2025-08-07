#!/bin/bash

# List all ClubManagement databases
# Shows catalog database and all tenant databases

# Source database configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/db-config.sh"

echo -e "${BLUE}🗃️  ClubManagement Databases${NC}"
echo "================================"
echo ""

# Test connection first
if ! test_connection; then
    echo -e "${RED}❌ Cannot connect to database server${NC}"
    exit 1
fi

echo -e "${BLUE}📊 All databases:${NC}"
execute_sql "SELECT datname as \"Database Name\", 
    pg_size_pretty(pg_database_size(datname)) as \"Size\",
    (SELECT count(*) FROM pg_stat_activity WHERE datname = d.datname AND state = 'active') as \"Active Connections\"
FROM pg_database d 
WHERE datname LIKE 'clubmanagement%' 
ORDER BY datname;" postgres

echo ""
echo -e "${BLUE}🔍 Database breakdown:${NC}"

# Check for catalog database
echo -n "Catalog Database: "
if execute_sql "SELECT 1 FROM pg_database WHERE datname = '$CATALOG_DB_NAME';" postgres | grep -q "1"; then
    echo -e "${GREEN}✅ $CATALOG_DB_NAME (exists)${NC}"
else
    echo -e "${YELLOW}⚠️  $CATALOG_DB_NAME (missing)${NC}"
fi

# Count tenant databases
TENANT_COUNT=$(execute_sql "SELECT count(*) FROM pg_database WHERE datname LIKE 'clubmanagement_%' AND datname != '$CATALOG_DB_NAME';" postgres | grep -oE '[0-9]+' | head -1)

echo -e "Tenant Databases: ${GREEN}$TENANT_COUNT found${NC}"

if [[ "$TENANT_COUNT" -gt 0 ]]; then
    echo ""
    echo -e "${BLUE}📋 Tenant databases:${NC}"
    execute_sql "SELECT 
        datname as \"Database Name\",
        CASE 
            WHEN datname LIKE '%_demo_%' THEN '🧪 Demo'
            ELSE '🏢 Production'
        END as \"Type\",
        pg_size_pretty(pg_database_size(datname)) as \"Size\"
    FROM pg_database 
    WHERE datname LIKE 'clubmanagement_%' AND datname != '$CATALOG_DB_NAME'
    ORDER BY datname;" postgres
fi

echo ""
echo -e "${BLUE}💡 Tips:${NC}"
echo "• Use './drop-all-databases.sh' to clean all databases"
echo "• Use './backup-database.sh <database>' to backup a specific database"  
echo "• Use './restore-database.sh <database> <backup-file>' to restore a database"