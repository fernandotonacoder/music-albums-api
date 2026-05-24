# Identity API - JWT Token Generator

A development/testing tool for generating JWT tokens to test the Music Albums API.

> **Full documentation**: [docs/IDENTITY_API.md](../../docs/IDENTITY_API.md) — covers Aspire setup (recommended), standalone run, configuration, and usage examples.

## Quick start

**Via Aspire (recommended):**
```bash
aspire start
```
Exposed on `http://localhost:5003` / `https://localhost:5004`.

**Standalone:**
```bash
cd tools/Identity.Api
dotnet user-secrets set "Jwt:Key" "your-secret-key-min-32-chars"
dotnet run
```

## ⚠️ Important

Not for production — no authentication, no user database, generates tokens for anyone.
