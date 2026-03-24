// ============================================================================
// Music Albums API - Sample Parameters (Modular Architecture)
// ============================================================================
// Purpose: Documentation and local development reference
// 
// Usage Options:
// 1. CI/CD (Recommended): Use ADO Variable Groups with inline parameters
// 2. Local Dev: Copy this file to main.bicepparam and fill in test values
// 
// NEVER commit main.bicepparam to source control!
// 
// Note: postgresAdminLogin/Password are required for server creation but the
// app itself uses passwordless Entra ID authentication (not these credentials).
// jwtKey and apiKey are stored in Key Vault and can be rotated there directly.
// ============================================================================

using 'main.bicep'

param location = 'westeurope'

// Base name for resource naming (e.g., 'music-albums' generates 'music-albums-api-dev')
param baseName = 'music-albums'

// Environment suffix: 'dev' or 'prod'
param deploymentSuffix = 'dev'

param postgresAdminLogin = 'YOUR_ADMIN_USERNAME'

param postgresAdminPassword = 'YOUR_SECURE_PASSWORD'

// Docker image tag to deploy (e.g., 'latest', 'v1.0.0', commit SHA)
param imageTag = 'latest'

// .NET runtime environment: 'Development', 'Staging', or 'Production'
param aspNetCoreEnvironment = 'Development'

param jwtKey = 'YOUR_JWT_SECRET_MIN_32_CHARS'

// JWT Issuer (must match the value used when generating tokens in Identity API)
param jwtIssuer = 'MusicAlbumsIdentity'

// JWT Audience (must match the value used when generating tokens in Identity API)
param jwtAudience = 'MusicAlbumsApi'

param apiKey = 'YOUR_API_KEY'

// ============================================================================
// ADO Variable Group Reference (for CI/CD pipelines)
// ============================================================================
// When using Azure DevOps Variable Groups, create these variables:
// - LOCATION
// - BASE_NAME
// - DEPLOYMENT_SUFFIX
// - POSTGRES_ADMIN_LOGIN
// - POSTGRES_ADMIN_PASSWORD (secret)
// - IMAGE_TAG
// - ASPNETCORE_ENVIRONMENT
// - JWT_KEY (secret)
// - API_KEY (secret)
// ============================================================================
