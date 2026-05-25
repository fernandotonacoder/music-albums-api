# Identity API - JWT Token Generator

A simple development/testing tool for generating JWT tokens to test the Music Albums API.

## Purpose

Generates JWT tokens with custom claims for testing authorization in the main Music Albums API.

## Running Locally

The Identity API requires `Jwt:Key` (min 32 characters). The key **must match** the one used by the main Music Albums API, otherwise tokens won't validate. On startup, the app fails fast if the key is missing or too short.

### Via Aspire (recommended)

The AppHost reads `jwt-key` from its user-secrets and injects it as `Jwt:Key` into both services — set it once and both API and Identity API pick it up:

```bash
cd src/MusicAlbumsApi.AppHost
dotnet user-secrets set "jwt-key" "your-secret-key-min-32-chars"
cd ..
aspire start
```

Exposed on `http://localhost:5003` and `https://localhost:5004`.

### Standalone (without Aspire)

Configure the secret on the Identity API project itself, then run:

```bash
cd tools/Identity.Api
dotnet user-secrets set "Jwt:Key" "your-secret-key-min-32-chars"
dotnet run
```

---

## Usage

### Generate a Token

**Endpoint:** `POST /token`

**Validation:**
- `userId` must be a valid GUID (not all zeros)

**Request:**
```json
{
  "userId": "d8663e5e-7494-4f81-8739-6e0de1bdb96f",
  "email": "admin@test.com",
  "customClaims": {
    "admin": "true",
    "trusted_member": "true"
  }
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### Test User Roles

**Admin:**
```json
{
  "userId": "00000000-0000-0000-0000-000000000001",
  "email": "admin@test.com",
  "customClaims": {
    "admin": "true",
    "trusted_member": "true"
  }
}
```

**Member:**
```json
{
  "userId": "00000000-0000-0000-0000-000000000002",
  "email": "member@test.com",
  "customClaims": {
    "trusted_member": "true"
  }
}
```

**Regular User:**
```json
{
  "userId": "00000000-0000-0000-0000-000000000003",
  "email": "user@test.com",
  "customClaims": {}
}
```

---

## Using the Token

Copy the token and use it in requests to the Music Albums API:

```bash
curl -H "Authorization: Bearer YOUR_TOKEN_HERE" \
     https://localhost:5002/api/albums
```

---

## Quick Test

```bash
aspire start &
sleep 10

curl -X POST https://localhost:5004/token \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "test@test.com",
    "customClaims": {}
  }'
```

---

## Important

⚠️ **DO NOT use this in production:**
- Has no authentication
- Has no user database
- Generates tokens for anyone
- For local/testing only

Tokens expire after 8 hours.
