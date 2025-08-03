#!/bin/bash

# Generate a secure JWT secret key
# This script generates a cryptographically secure random key for JWT signing

echo "🔐 Generating secure JWT secret key..."

# Generate a 64-character random string (256 bits)
JWT_SECRET=$(openssl rand -base64 48 | tr -d "=+/" | cut -c1-64)

echo "✅ Generated JWT secret key:"
echo ""
echo "JWT_SECRET_KEY=$JWT_SECRET"
echo ""
echo "📝 Copy this line and update your .env file:"
echo "   sed -i 's/JWT_SECRET_KEY=.*/JWT_SECRET_KEY=$JWT_SECRET/' .env"
echo ""
echo "Or manually edit .env and replace the JWT_SECRET_KEY value."

# Optionally update .env automatically
if [ -f ".env" ]; then
    read -p "🤔 Do you want to automatically update your .env file? (y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        # Backup original .env
        cp .env .env.backup
        # Update JWT secret key
        sed -i "s/JWT_SECRET_KEY=.*/JWT_SECRET_KEY=$JWT_SECRET/" .env
        echo "✅ .env file updated! (backup saved as .env.backup)"
    fi
fi