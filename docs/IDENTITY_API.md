# Identity API - JWT Token Generator

A simple development/testing tool for generating JWT tokens to test the Music Albums API.

## Purpose

Generates JWT tokens with custom claims for testing authorization in the main Music Albums API.

## Running Locally

```bash
cd tools/Identity.Api
dotnet run
```

Server runs at: `http://localhost:5003`

---

## Configuration

The Identity API requires `Jwt:Key` (user-secrets, min 32 characters).

**Setup (first time):**
```bash
cd tools/Identity.Api
dotnet user-secrets set "Jwt:Key" "your-secret-key-min-32-chars"
```

This secret must match the `Jwt:Key` used in the main Music Albums API for token validation.

On startup, the API validates the secret and fails fast if missing or too short.

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
     http://localhost:5001/api/albums
```

---

## Quick Test

```bash
dotnet run &
sleep 2

curl -X POST http://localhost:5003/token \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "test@test.com",
    "customClaims": {}
  }'
```

---

---

## Important

⚠️ **DO NOT use this in production:**
- Has no authentication
- Has no user database
- Generates tokens for anyone
- For local/testing only

Tokens expire after 8 hours.
