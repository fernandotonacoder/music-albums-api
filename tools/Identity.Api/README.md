# Identity API - JWT Token Generator

A simple development tool for generating JWT tokens to test the MusicAlbums API.

## Purpose

This is a **development/testing tool only**. It generates JWT tokens with custom claims for testing authorization in the main MusicAlbums API.

## Running
```bash
cd tools/Identity.Api
dotnet run
```

Default URL: `http://localhost:5003`

## Usage

### Generate a Token

**Endpoint:** `POST /token`

**Validation:**
- `userId` must be a valid GUID (not all zeros)
- Invalid GUID format returns 400 Bad Request

**Request:**
```json
{
  "userId": "d8663e5e-7494-4f81-8739-6e0de1bdb96f",
  "email": "admin@musicalbums.com",
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

### Testing Different User Roles

**Admin User:**
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

**Trusted Member:**
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

## Using the Token

Copy the generated token and use it in requests to the MusicAlbums API:
```bash
curl -H "Authorization: Bearer YOUR_TOKEN_HERE" \
     http://localhost:5001/api/albums
```

## Configuration

**Setup (first time):**
```bash
cd tools/Identity.Api
dotnet user-secrets set "Jwt:Secret" "your-secret-key-min-32-chars"
```

This secret must match the `Jwt:Key` in the main API for token validation to work.

Tokens expire after 8 hours.

## Quick Test

Test the service is running:
```bash
curl -X POST http://localhost:5003/token \
  -H "Content-Type: application/json" \
  -d '{"userId":"550e8400-e29b-41d4-a716-446655440000","email":"test@test.com","customClaims":{}}'
```

## ⚠️ Important

**DO NOT use this in production!** This tool:
- Has no authentication
- Has no user database
- Generates tokens for anyone
- Is for local testing purposes only