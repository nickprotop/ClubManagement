#!/bin/bash

# Reset demo data
# Drops demo tenant database and recreates it with fresh seeded data

# Source database configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/db-config.sh"

# Demo database configuration
DEMO_DB_NAME="clubmanagement_demo_club"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo -e "${BLUE}🧪 ClubManagement Demo Data Reset${NC}"
echo "=================================="
echo ""

# Test connection first
if ! test_connection; then
    echo -e "${RED}❌ Cannot connect to database server${NC}"
    exit 1
fi

echo -e "${BLUE}🎯 Reset Configuration:${NC}"
echo -e "Demo Database: ${GREEN}$DEMO_DB_NAME${NC}"
echo -e "Project Root: ${GREEN}$PROJECT_ROOT${NC}"
echo ""

# Check if demo database exists
if execute_sql "SELECT 1 FROM pg_database WHERE datname = '$DEMO_DB_NAME';" postgres | grep -q "1"; then
    echo -e "${BLUE}📋 Current demo database found${NC}"
    
    # Show database size before deletion
    size=$(execute_sql "SELECT pg_size_pretty(pg_database_size('$DEMO_DB_NAME'));" postgres | grep -v "pg_size_pretty" | tr -d ' ')
    echo -e "${BLUE}📊 Current size: $size${NC}"
else
    echo -e "${YELLOW}ℹ️  Demo database does not exist - will be created fresh${NC}"
fi

# Confirmation prompt unless --force flag is used
if [[ "$1" != "--force" ]] && [[ "$1" != "-f" ]]; then
    echo ""
    echo -e "${YELLOW}⚠️  This will reset the demo database and all demo data!${NC}"
    echo -e "${YELLOW}   Demo users, events, hardware, and assignments will be recreated${NC}"
    echo ""
    read -p "Continue? Type 'YES' to confirm: " confirmation
    
    if [[ "$confirmation" != "YES" ]]; then
        echo -e "${BLUE}ℹ️  Operation cancelled${NC}"
        exit 0
    fi
fi

echo ""
echo -e "${BLUE}🚀 Starting demo data reset...${NC}"

# Step 1: Drop demo database if it exists
if execute_sql "SELECT 1 FROM pg_database WHERE datname = '$DEMO_DB_NAME';" postgres | grep -q "1"; then
    echo -e "${YELLOW}🔌 Terminating connections to demo database...${NC}"
    execute_sql "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$DEMO_DB_NAME';" postgres > /dev/null
    
    echo -e "${RED}🗑️  Dropping demo database...${NC}"
    if execute_sql "DROP DATABASE IF EXISTS \"$DEMO_DB_NAME\";" postgres > /dev/null; then
        echo -e "${GREEN}✅ Demo database dropped${NC}"
    else
        echo -e "${RED}❌ Failed to drop demo database${NC}"
        exit 1
    fi
fi

# Step 2: Run the API to recreate databases with fresh seeded data
echo -e "${BLUE}🔄 Recreating databases with fresh demo data...${NC}"
echo -e "${YELLOW}ℹ️  Starting API briefly to trigger database creation and seeding...${NC}"

cd "$PROJECT_ROOT"

# Build the API project
echo -e "${BLUE}🔨 Building API project...${NC}"
if dotnet build src/Api/ClubManagement.Api --configuration Release --verbosity quiet; then
    echo -e "${GREEN}✅ Build successful${NC}"
else
    echo -e "${RED}❌ Build failed${NC}"
    exit 1
fi

# Start API temporarily to trigger seeding
echo -e "${BLUE}🚀 Starting API to trigger database seeding...${NC}"
echo -e "${YELLOW}ℹ️  This may take a few seconds...${NC}"

# Run API in background and capture PID
cd src/Api/ClubManagement.Api
timeout 30s dotnet run --configuration Release --verbosity quiet > /dev/null 2>&1 &
API_PID=$!

# Wait a moment for startup and seeding
sleep 15

# Kill the API process
if kill $API_PID 2>/dev/null; then
    echo -e "${GREEN}✅ API seeding completed${NC}"
else
    echo -e "${YELLOW}ℹ️  API process already finished${NC}"
fi

# Return to script directory
cd "$SCRIPT_DIR"

# Step 3: Verify demo database was created
echo -e "${BLUE}🔍 Verifying demo database creation...${NC}"

if execute_sql "SELECT 1 FROM pg_database WHERE datname = '$DEMO_DB_NAME';" postgres | grep -q "1"; then
    echo -e "${GREEN}✅ Demo database recreated successfully${NC}"
    
    # Show new database size
    size=$(execute_sql "SELECT pg_size_pretty(pg_database_size('$DEMO_DB_NAME'));" postgres | grep -v "pg_size_pretty" | tr -d ' ')
    echo -e "${BLUE}📊 New database size: $size${NC}"
    
    # Show demo data summary
    echo -e "${BLUE}📋 Demo data summary:${NC}"
    execute_sql "SELECT 'Users' as \"Data Type\", count(*) as \"Count\" FROM \"Users\"
        UNION ALL
        SELECT 'Members', count(*) FROM \"Members\"
        UNION ALL  
        SELECT 'Events', count(*) FROM \"Events\"
        UNION ALL
        SELECT 'Hardware', count(*) FROM \"Hardware\"
        UNION ALL
        SELECT 'Hardware Types', count(*) FROM \"HardwareTypes\"
        UNION ALL
        SELECT 'Facilities', count(*) FROM \"Facilities\";" "$DEMO_DB_NAME"
        
else
    echo -e "${RED}❌ Demo database was not created${NC}"
    echo -e "${YELLOW}ℹ️  Try starting the API manually to check for errors${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}🎉 Demo data reset completed successfully!${NC}"
echo ""
echo -e "${BLUE}🔑 Demo Login Credentials:${NC}"
echo -e "${GREEN}Domain: demo.localhost${NC}"
echo -e "${GREEN}Admin: admin@demo.localhost / Admin123!${NC}"
echo -e "${GREEN}Member: member@demo.localhost / Member123!${NC}"
echo -e "${GREEN}Coach: coach@demo.localhost / Coach123!${NC}"
echo ""
echo -e "${BLUE}💡 Next steps:${NC}"
echo "• Start the application: ./scripts/start-infra.sh && dotnet run (API) && dotnet run (Client)"
echo "• Access the demo at: http://demo.localhost:4002"
echo "• All hardware will have 'Available' status with proper assignment tracking"