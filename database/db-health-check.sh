#!/bin/bash

# Database Health Check
# Comprehensive health check for ClubManagement database system

# Source database configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/db-config.sh"

# Health check results
CHECKS_PASSED=0
CHECKS_FAILED=0
WARNINGS=0

# Function to perform a check
perform_check() {
    local check_name="$1"
    local check_command="$2"
    local expected_result="$3"
    local is_warning_only="${4:-false}"
    
    echo -ne "${BLUE}🔍 $check_name...${NC} "
    
    if eval "$check_command" &>/dev/null; then
        if [[ "$is_warning_only" == "true" ]]; then
            echo -e "${YELLOW}⚠️  WARNING${NC}"
            ((WARNINGS++))
        else
            echo -e "${GREEN}✅ PASS${NC}"
            ((CHECKS_PASSED++))
        fi
    else
        if [[ "$is_warning_only" == "true" ]]; then
            echo -e "${BLUE}ℹ️  OK${NC}"
        else
            echo -e "${RED}❌ FAIL${NC}"
            ((CHECKS_FAILED++))
        fi
    fi
}

# Function to check database connectivity
check_connectivity() {
    echo -e "${BLUE}🔌 Database Connectivity${NC}"
    echo "========================"
    
    # Check Docker container
    if command -v docker &> /dev/null; then
        perform_check "Docker PostgreSQL container running" \
            "docker ps | grep -q '$DOCKER_POSTGRES_CONTAINER'" \
            "container_found"
    fi
    
    # Check database connection
    perform_check "Database connection" \
        "test_connection" \
        "connection_ok"
    
    # Check PostgreSQL version
    if test_connection &>/dev/null; then
        version=$(execute_sql "SELECT version();" postgres 2>/dev/null | grep PostgreSQL | head -1)
        if [[ -n "$version" ]]; then
            echo -e "${GREEN}✅ PostgreSQL Version: ${version}${NC}"
        fi
    fi
    
    echo ""
}

# Function to check database structure
check_database_structure() {
    echo -e "${BLUE}🗃️  Database Structure${NC}"
    echo "======================"
    
    # Check catalog database
    perform_check "Catalog database exists" \
        "execute_sql \"SELECT 1 FROM pg_database WHERE datname = '$CATALOG_DB_NAME';\" postgres | grep -q '1'" \
        "catalog_found"
    
    # Count tenant databases
    tenant_count=$(execute_sql "SELECT count(*) FROM pg_database WHERE datname LIKE 'clubmanagement_%' AND datname != '$CATALOG_DB_NAME';" postgres 2>/dev/null | grep -oE '[0-9]+' | head -1)
    
    if [[ "$tenant_count" -gt 0 ]]; then
        echo -e "${GREEN}✅ Tenant databases found: $tenant_count${NC}"
    else
        echo -e "${YELLOW}⚠️  No tenant databases found${NC}"
        ((WARNINGS++))
    fi
    
    # Check for demo database specifically
    perform_check "Demo database exists" \
        "execute_sql \"SELECT 1 FROM pg_database WHERE datname = 'clubmanagement_demo_club';\" postgres | grep -q '1'" \
        "demo_found" \
        "true"
    
    echo ""
}

# Function to check database content
check_database_content() {
    echo -e "${BLUE}📊 Database Content${NC}"
    echo "=================="
    
    # Check if catalog database has tenant records
    if execute_sql "SELECT 1 FROM pg_database WHERE datname = '$CATALOG_DB_NAME';" postgres | grep -q "1"; then
        tenant_records=$(execute_sql "SELECT count(*) FROM \"Tenants\";" "$CATALOG_DB_NAME" 2>/dev/null | grep -oE '[0-9]+' | head -1)
        
        if [[ "$tenant_records" -gt 0 ]]; then
            echo -e "${GREEN}✅ Catalog has $tenant_records tenant(s)${NC}"
        else
            echo -e "${YELLOW}⚠️  No tenants found in catalog${NC}"
            ((WARNINGS++))
        fi
    fi
    
    # Check demo database content if it exists
    if execute_sql "SELECT 1 FROM pg_database WHERE datname = 'clubmanagement_demo_club';" postgres | grep -q "1"; then
        echo -e "${BLUE}🧪 Demo database content:${NC}"
        
        # Check key tables
        users=$(execute_sql "SELECT count(*) FROM \"Users\";" "clubmanagement_demo_club" 2>/dev/null | grep -oE '[0-9]+' | head -1)
        hardware=$(execute_sql "SELECT count(*) FROM \"Hardware\";" "clubmanagement_demo_club" 2>/dev/null | grep -oE '[0-9]+' | head -1)
        events=$(execute_sql "SELECT count(*) FROM \"Events\";" "clubmanagement_demo_club" 2>/dev/null | grep -oE '[0-9]+' | head -1)
        
        [[ "$users" -gt 0 ]] && echo -e "  ${GREEN}✅ Users: $users${NC}" || echo -e "  ${YELLOW}⚠️  Users: $users${NC}"
        [[ "$hardware" -gt 0 ]] && echo -e "  ${GREEN}✅ Hardware: $hardware${NC}" || echo -e "  ${YELLOW}⚠️  Hardware: $hardware${NC}"
        [[ "$events" -gt 0 ]] && echo -e "  ${GREEN}✅ Events: $events${NC}" || echo -e "  ${YELLOW}⚠️  Events: $events${NC}"
    fi
    
    echo ""
}

# Function to check database performance
check_database_performance() {
    echo -e "${BLUE}⚡ Database Performance${NC}"
    echo "======================"
    
    # Check database sizes
    echo -e "${BLUE}📊 Database sizes:${NC}"
    execute_sql "SELECT 
        datname as \"Database\",
        pg_size_pretty(pg_database_size(datname)) as \"Size\",
        (SELECT count(*) FROM pg_stat_activity WHERE datname = d.datname) as \"Connections\"
    FROM pg_database d 
    WHERE datname LIKE 'clubmanagement%' 
    ORDER BY pg_database_size(datname) DESC;" postgres 2>/dev/null
    
    # Check for long-running queries
    long_queries=$(execute_sql "SELECT count(*) FROM pg_stat_activity WHERE state = 'active' AND query_start < now() - interval '1 minute';" postgres 2>/dev/null | grep -oE '[0-9]+' | head -1)
    
    if [[ "$long_queries" -eq 0 ]]; then
        echo -e "${GREEN}✅ No long-running queries${NC}"
    else
        echo -e "${YELLOW}⚠️  $long_queries long-running queries detected${NC}"
        ((WARNINGS++))
    fi
    
    # Check connection count
    total_connections=$(execute_sql "SELECT count(*) FROM pg_stat_activity;" postgres 2>/dev/null | grep -oE '[0-9]+' | head -1)
    echo -e "${BLUE}🔌 Total active connections: $total_connections${NC}"
    
    echo ""
}

# Function to check schema integrity
check_schema_integrity() {
    echo -e "${BLUE}🔧 Schema Integrity${NC}"
    echo "=================="
    
    # Check if demo database has required tables
    if execute_sql "SELECT 1 FROM pg_database WHERE datname = 'clubmanagement_demo_club';" postgres | grep -q "1"; then
        required_tables=("Users" "Members" "Hardware" "HardwareTypes" "Events" "Facilities" "HardwareAssignments")
        
        for table in "${required_tables[@]}"; do
            perform_check "Table '$table' exists" \
                "execute_sql \"SELECT 1 FROM information_schema.tables WHERE table_name = '$table';\" 'clubmanagement_demo_club' | grep -q '1'" \
                "table_found"
        done
        
        # Check for foreign key constraints
        fk_count=$(execute_sql "SELECT count(*) FROM information_schema.table_constraints WHERE constraint_type = 'FOREIGN KEY';" "clubmanagement_demo_club" 2>/dev/null | grep -oE '[0-9]+' | head -1)
        echo -e "${BLUE}🔗 Foreign key constraints: $fk_count${NC}"
    fi
    
    echo ""
}

# Main health check execution
echo -e "${BLUE}🏥 ClubManagement Database Health Check${NC}"
echo "======================================="
echo ""

show_config
echo ""

# Run all health checks
check_connectivity
check_database_structure
check_database_content
check_database_performance
check_schema_integrity

# Health check summary
echo -e "${BLUE}📋 Health Check Summary${NC}"
echo "======================"
echo -e "✅ Checks Passed: ${GREEN}$CHECKS_PASSED${NC}"
echo -e "❌ Checks Failed: ${RED}$CHECKS_FAILED${NC}"
echo -e "⚠️  Warnings: ${YELLOW}$WARNINGS${NC}"

echo ""

# Overall health status
if [[ $CHECKS_FAILED -eq 0 ]]; then
    if [[ $WARNINGS -eq 0 ]]; then
        echo -e "${GREEN}🎉 Database system is healthy!${NC}"
        exit 0
    else
        echo -e "${YELLOW}⚠️  Database system has warnings but is functional${NC}"
        exit 1
    fi
else
    echo -e "${RED}❌ Database system has critical issues that need attention${NC}"
    echo ""
    echo -e "${BLUE}💡 Recommended actions:${NC}"
    echo "• Check database server status"
    echo "• Verify connection settings in config.json"
    echo "• Run './reset-demo-data.sh' to recreate demo data"
    echo "• Check application logs for errors"
    exit 2
fi