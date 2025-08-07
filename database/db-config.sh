#!/bin/bash

# Database Configuration Reader
# Reads database configuration from config.json or fallback to environment defaults

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to extract value from config.json
get_config_value() {
    local key="$1"
    local config_file="src/Api/ClubManagement.Api/config.json"
    
    if [[ -f "$config_file" ]]; then
        # Extract value from config.json using jq if available, otherwise grep/sed
        if command -v jq &> /dev/null; then
            jq -r "$key // empty" "$config_file" 2>/dev/null
        else
            # Fallback to grep/sed for simple key extraction
            grep "\"$(echo $key | cut -d'.' -f2)\":" "$config_file" | sed 's/.*": *"\([^"]*\)".*/\1/'
        fi
    fi
}

# Set database configuration with fallbacks
export DB_HOST=$(get_config_value '.Database.Host')
export DB_PORT=$(get_config_value '.Database.Port')
export DB_NAME=$(get_config_value '.Database.Database')
export DB_USER=$(get_config_value '.Database.Username')
export DB_PASSWORD=$(get_config_value '.Database.Password')

# Fallback to environment defaults if config.json values are empty
export DB_HOST=${DB_HOST:-${POSTGRES_HOST:-localhost}}
export DB_PORT=${DB_PORT:-${POSTGRES_PORT:-4004}}
export DB_NAME=${DB_NAME:-${POSTGRES_DB:-clubmanagement}}
export DB_USER=${DB_USER:-${POSTGRES_USER:-clubadmin}}
export DB_PASSWORD=${DB_PASSWORD:-${POSTGRES_PASSWORD:-clubpassword}}

# Additional derived values
export DB_CONNECTION_STRING="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"
export CATALOG_DB_NAME="clubmanagement_catalog"
export CATALOG_CONNECTION_STRING="Host=${DB_HOST};Port=${DB_PORT};Database=${CATALOG_DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"

# Docker container name (adjust if your container has a different name)
export DOCKER_POSTGRES_CONTAINER="clubmanagement-postgres"

# Function to test database connection
test_connection() {
    echo -e "${BLUE}Testing database connection...${NC}"
    
    if command -v docker &> /dev/null && docker ps | grep -q "$DOCKER_POSTGRES_CONTAINER"; then
        # Test via Docker
        if docker exec -i "$DOCKER_POSTGRES_CONTAINER" psql -U "$DB_USER" -d postgres -c "SELECT 1;" &>/dev/null; then
            echo -e "${GREEN}✅ Database connection successful${NC}"
            return 0
        else
            echo -e "${RED}❌ Database connection failed${NC}"
            return 1
        fi
    elif command -v psql &> /dev/null; then
        # Test via direct psql
        if PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres -c "SELECT 1;" &>/dev/null; then
            echo -e "${GREEN}✅ Database connection successful${NC}"
            return 0
        else
            echo -e "${RED}❌ Database connection failed${NC}"
            return 1
        fi
    else
        echo -e "${YELLOW}⚠️  Cannot test connection: Neither docker nor psql available${NC}"
        return 2
    fi
}

# Function to show current configuration
show_config() {
    echo -e "${BLUE}📋 Current Database Configuration:${NC}"
    echo -e "Host: ${GREEN}$DB_HOST${NC}"
    echo -e "Port: ${GREEN}$DB_PORT${NC}"
    echo -e "Database: ${GREEN}$DB_NAME${NC}"
    echo -e "Username: ${GREEN}$DB_USER${NC}"
    echo -e "Password: ${GREEN}[HIDDEN]${NC}"
    echo -e "Container: ${GREEN}$DOCKER_POSTGRES_CONTAINER${NC}"
    echo ""
    echo -e "${BLUE}📋 Database Names:${NC}"
    echo -e "Catalog Database: ${GREEN}$CATALOG_DB_NAME${NC}"
    echo -e "Main Database: ${GREEN}$DB_NAME${NC}"
    echo ""
}

# Execute psql command via Docker or direct connection
execute_sql() {
    local sql="$1"
    local database="${2:-postgres}"
    
    if command -v docker &> /dev/null && docker ps | grep -q "$DOCKER_POSTGRES_CONTAINER"; then
        docker exec -i "$DOCKER_POSTGRES_CONTAINER" psql -U "$DB_USER" -d "$database" -c "$sql"
    elif command -v psql &> /dev/null; then
        PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$database" -c "$sql"
    else
        echo -e "${RED}❌ Neither docker nor psql available${NC}"
        return 1
    fi
}

# Execute psql from file via Docker or direct connection
execute_sql_file() {
    local file="$1"
    local database="${2:-postgres}"
    
    if [[ ! -f "$file" ]]; then
        echo -e "${RED}❌ SQL file not found: $file${NC}"
        return 1
    fi
    
    if command -v docker &> /dev/null && docker ps | grep -q "$DOCKER_POSTGRES_CONTAINER"; then
        docker exec -i "$DOCKER_POSTGRES_CONTAINER" psql -U "$DB_USER" -d "$database" < "$file"
    elif command -v psql &> /dev/null; then
        PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$database" < "$file"
    else
        echo -e "${RED}❌ Neither docker nor psql available${NC}"
        return 1
    fi
}

# If script is run directly, show configuration and test connection
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    show_config
    test_connection
fi