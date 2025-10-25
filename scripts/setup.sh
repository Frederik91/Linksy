#!/bin/bash

set -e

echo "==================================="
echo "Linksy Project Setup"
echo "==================================="
echo ""

# Check for .NET 8 SDK
echo "Checking for .NET 8 SDK..."
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found!"
    echo "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
if [[ ! $DOTNET_VERSION =~ ^(8|9)\. ]]; then
    echo "❌ .NET 8 or higher is required, but found: $DOTNET_VERSION"
    echo "Please install .NET 8 or later from: https://dotnet.microsoft.com/download"
    exit 1
fi
echo "✅ .NET $DOTNET_VERSION"
echo ""

# Check for Node.js 22 LTS
echo "Checking for Node.js 22 LTS..."
if ! command -v node &> /dev/null; then
    echo "❌ Node.js not found!"
    echo "Please install Node.js 22 LTS from: https://nodejs.org/"
    exit 1
fi

NODE_VERSION=$(node --version)
NODE_MAJOR=$(echo $NODE_VERSION | cut -d'.' -f1 | sed 's/v//')
if [[ $NODE_MAJOR -lt 22 ]]; then
    echo "❌ Node.js 22 LTS is required, but found: $NODE_VERSION"
    echo "Please install Node.js 22 LTS from: https://nodejs.org/"
    exit 1
fi
echo "✅ $NODE_VERSION"
echo ""

# Check for npm
echo "Checking for npm..."
if ! command -v npm &> /dev/null; then
    echo "❌ npm not found!"
    echo "npm should be included with Node.js. Please reinstall Node.js."
    exit 1
fi
NPM_VERSION=$(npm --version)
echo "✅ npm $NPM_VERSION"
echo ""

# Restore .NET dependencies
echo "Restoring .NET dependencies..."
dotnet restore Linksy.sln
echo "✅ .NET dependencies restored"
echo ""

# Install npm dependencies
echo "Installing npm dependencies..."
npm install --prefix ./frontend
echo "✅ npm dependencies installed"
echo ""

echo "==================================="
echo "Setup Complete! ✅"
echo "==================================="
echo ""
echo "Next steps:"
echo "1. Start the Aspire AppHost:"
echo "   dotnet run --project Linksy.AppHost"
echo ""
echo "2. Open your browser to:"
echo "   - Frontend: http://localhost:5173"
echo "   - API Health: http://localhost:5000/health"
echo "   - Aspire Dashboard: http://localhost:15217"
echo ""
