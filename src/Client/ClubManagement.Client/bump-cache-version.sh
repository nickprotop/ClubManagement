#!/bin/bash

# Service Worker Cache Version Bumper
# Usage: ./bump-cache-version.sh [major|minor|patch] [-y]
# Default: patch
# Flags:
#   -y  : Auto-accept (skip confirmation)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVICE_WORKER_FILE="$SCRIPT_DIR/wwwroot/service-worker.js"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

print_success() {
    echo -e "${GREEN}✅${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_error() {
    echo -e "${RED}❌${NC} $1"
}

# Parse arguments
AUTO_ACCEPT=false
BUMP_TYPE=""

for arg in "$@"; do
    case $arg in
        -y|--yes)
            AUTO_ACCEPT=true
            ;;
        major|minor|patch)
            BUMP_TYPE="$arg"
            ;;
        -*)
            print_error "Unknown flag: $arg"
            echo "Usage: $0 [major|minor|patch] [-y]"
            echo "Flags:"
            echo "  -y, --yes       Auto-accept (skip confirmation)"
            exit 1
            ;;
        *)
            if [[ -z "$BUMP_TYPE" ]]; then
                BUMP_TYPE="$arg"
            else
                print_error "Unknown argument: $arg"
                exit 1
            fi
            ;;
    esac
done

# Validate service worker file exists
if [[ ! -f "$SERVICE_WORKER_FILE" ]]; then
    print_error "Service worker file not found: $SERVICE_WORKER_FILE"
    exit 1
fi

# Get current version from service worker
CURRENT_VERSION=$(grep "const CACHE_VERSION = " "$SERVICE_WORKER_FILE" | sed "s/.*'\([^']*\)'.*/\1/")

if [[ -z "$CURRENT_VERSION" ]]; then
    print_error "Could not extract current cache version from service worker"
    exit 1
fi

print_info "Current cache version: $CURRENT_VERSION"

# Parse version components
if [[ ! "$CURRENT_VERSION" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
    print_error "Invalid version format: $CURRENT_VERSION (expected: X.Y.Z)"
    exit 1
fi

MAJOR=${BASH_REMATCH[1]}
MINOR=${BASH_REMATCH[2]}
PATCH=${BASH_REMATCH[3]}

# Determine bump type (default to patch)
if [[ -z "$BUMP_TYPE" ]]; then
    BUMP_TYPE="patch"
fi

case "$BUMP_TYPE" in
    "major")
        NEW_MAJOR=$((MAJOR + 1))
        NEW_MINOR=0
        NEW_PATCH=0
        CHANGE_DESCRIPTION="Major version bump (breaking changes or complete overhaul)"
        ;;
    "minor")
        NEW_MAJOR=$MAJOR
        NEW_MINOR=$((MINOR + 1))
        NEW_PATCH=0
        CHANGE_DESCRIPTION="Minor version bump (new features, asset additions)"
        ;;
    "patch")
        NEW_MAJOR=$MAJOR
        NEW_MINOR=$MINOR
        NEW_PATCH=$((PATCH + 1))
        CHANGE_DESCRIPTION="Patch version bump (bug fixes, small tweaks)"
        ;;
    *)
        print_error "Invalid bump type: $BUMP_TYPE"
        echo "Usage: $0 [major|minor|patch] [-y]"
        echo ""
        echo "Bump types:"
        echo "  major - Breaking changes or complete cache strategy overhaul"
        echo "  minor - New features, asset additions, strategy changes"
        echo "  patch - Bug fixes, small tweaks (default)"
        echo ""
        echo "Flags:"
        echo "  -y, --yes       Auto-accept (skip confirmation)"
        exit 1
        ;;
esac

NEW_VERSION="$NEW_MAJOR.$NEW_MINOR.$NEW_PATCH"

print_info "$CHANGE_DESCRIPTION"
print_warning "Version change: $CURRENT_VERSION → $NEW_VERSION"

# Ask for confirmation unless auto-accept is enabled
if [[ "$AUTO_ACCEPT" == "false" ]]; then
    echo ""
    read -p "Do you want to proceed? (y/N): " -n 1 -r
    echo ""

    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_info "Version bump cancelled"
        exit 0
    fi
else
    print_info "Auto-accepting changes (-y flag enabled)"
fi

# Update the version in service worker
sed -i "s/const CACHE_VERSION = '[^']*'/const CACHE_VERSION = '$NEW_VERSION'/" "$SERVICE_WORKER_FILE"

# Verify the change
NEW_VERSION_CHECK=$(grep "const CACHE_VERSION = " "$SERVICE_WORKER_FILE" | sed "s/.*'\([^']*\)'.*/\1/")

if [[ "$NEW_VERSION_CHECK" == "$NEW_VERSION" ]]; then
    print_success "Cache version successfully updated to $NEW_VERSION"
    print_info "Cache names will be:"
    print_info "  • clubmanagement-static-v$NEW_VERSION"
    print_info "  • clubmanagement-dynamic-v$NEW_VERSION"
    echo ""
    print_warning "Remember to:"
    print_warning "  1. Test the service worker in development"
    print_warning "  2. Commit these changes to git"
    print_warning "  3. Deploy to force cache refresh for all users"
else
    print_error "Version update failed. Version check: $NEW_VERSION_CHECK"
    exit 1
fi

# Show what changed
echo ""
print_info "Changed in $SERVICE_WORKER_FILE:"
echo "- const CACHE_VERSION = '$CURRENT_VERSION';"
echo "+ const CACHE_VERSION = '$NEW_VERSION';"

print_success "Cache version bump complete!"