// ============================================================================
// Music Albums API - Sample Parameters
// ============================================================================
// Copy this file to main.bicepparam and fill in your values
// NEVER commit main.bicepparam to source control!
// ============================================================================

using 'main.bicep'

// Resource naming
param location = 'westeurope'
param baseName = 'music-albums'
param acrName = 'YOUR_UNIQUE_ACR_NAME' // Must be globally unique, alphanumeric only

// PostgreSQL
param postgresAdminLogin = 'YOUR_ADMIN_USERNAME'
param postgresAdminPassword = 'YOUR_SECURE_PASSWORD'

// Container image
param imageTag = 'latest'

// JWT Configuration (must match Identity API)
param jwtKey = 'YOUR_JWT_SECRET_MIN_32_CHARS'
param jwtIssuer = 'https://your-app.azurecontainerapps.io'
param jwtAudience = 'https://your-app.azurecontainerapps.io'

// API Key for admin operations
param apiKey = 'YOUR_API_KEY'
