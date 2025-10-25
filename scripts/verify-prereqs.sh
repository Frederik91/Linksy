#!/bin/bash

# Prerequisite verification script for CI/automated testing

set -e

echo "Verifying prerequisites for Linksy..."
echo ""

# Check .NET SDK
echo "Checking .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: dotnet not found"
    exit 1
fi
DOTNET_VERSION=$(dotnet --version)
echo "  Version: $DOTNET_VERSION"

if [[ ! $DOTNET_VERSION =~ ^[89]\. ]]; then
    echo "ERROR: .NET 8 or 9 SDK is required"
    exit 1
fi

# Check Node.js
echo "Checking Node.js..."
if ! command -v node &> /dev/null; then
    echo "ERROR: node not found"
    exit 1
fi
NODE_VERSION=$(node --version)
echo "  Version: $NODE_VERSION"

NODE_MAJOR=$(echo $NODE_VERSION | cut -d'.' -f1 | sed 's/v//')
if [[ $NODE_MAJOR -lt 22 ]]; then
    echo "ERROR: Node.js 22 LTS is required"
    exit 1
fi

# Check npm
echo "Checking npm..."
if ! command -v npm &> /dev/null; then
    echo "ERROR: npm not found"
    exit 1
fi
NPM_VERSION=$(npm --version)
echo "  Version: $NPM_VERSION"

echo ""
echo "✅ All prerequisites verified!"
exit 0
