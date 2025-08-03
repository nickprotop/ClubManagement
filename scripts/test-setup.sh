#!/bin/bash

# Test script to validate setup.sh functionality
# This script tests the setup without actually running Docker

echo "🧪 Testing Club Management Platform Setup Script"
echo "================================================"

# Create a backup if .env exists
if [ -f ".env" ]; then
    cp .env .env.test-backup
    echo "📁 Backed up existing .env to .env.test-backup"
fi

# Test quick setup
echo ""
echo "🚀 Testing quick setup mode..."
echo ""

# Remove .env to test fresh setup
rm -f .env

# Run quick setup (this should work without user input)
if ./scripts/setup.sh --quick > test-output.log 2>&1; then
    echo "✅ Quick setup completed successfully"
    
    # Check if .env was created
    if [ -f ".env" ]; then
        echo "✅ .env file was created"
        
        # Check if config.json was generated
        if [ -f "src/Api/ClubManagement.Api/config.json" ]; then
            echo "✅ config.json was generated"
        else
            echo "❌ config.json was not generated"
        fi
        
        # Validate some key values in .env
        if grep -q "POSTGRES_PASSWORD=" .env && grep -q "JWT_SECRET_KEY=" .env; then
            echo "✅ .env contains required configuration"
        else
            echo "❌ .env missing required configuration"
        fi
    else
        echo "❌ .env file was not created"
    fi
else
    echo "❌ Quick setup failed"
    echo "Error output:"
    tail -20 test-output.log
fi

# Clean up test files
rm -f test-output.log

# Restore backup if it existed
if [ -f ".env.test-backup" ]; then
    mv .env.test-backup .env
    echo "📁 Restored original .env file"
fi

echo ""
echo "🧪 Test completed!"
echo ""
echo "To test interactive mode manually:"
echo "   ./scripts/setup.sh"
echo ""
echo "To run the actual setup:"
echo "   ./scripts/setup.sh --quick    # Quick setup"
echo "   ./scripts/setup.sh            # Interactive setup"