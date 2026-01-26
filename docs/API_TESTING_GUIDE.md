# Music Albums API - Manual Testing Guide

Complete collection of copy-pastable HTTP requests for testing all API endpoints.

## 📋 Table of Contents

- [Prerequisites](#prerequisites)
- [Authentication](#authentication)
- [Album Endpoints](#album-endpoints)
- [Rating Endpoints](#rating-endpoints)
- [Test Scenarios](#test-scenarios)

---

## Prerequisites

**Base URL**: `http://localhost:5000` (adjust port as needed)

**Required Headers**:

```
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN_HERE
```

**Get a Token**: First, get a token from your Identity API:

```bash
# Get Admin Token
curl -X POST http://localhost:5001/token \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "00000000-0000-0000-0000-000000000001",
    "email": "admin@example.com",
    "customClaims": {
      "admin": "true",
      "trusted_member": "true"
    }
  }'

# Get Trusted Member Token
curl -X POST http://localhost:5001/token \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "00000000-0000-0000-0000-000000000002",
    "email": "user@example.com",
    "customClaims": {
      "trusted_member": "true"
    }
  }'

# Get Regular User Token
curl -X POST http://localhost:5001/token \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "00000000-0000-0000-0000-000000000003",
    "email": "regular@example.com",
    "customClaims": {}
  }'
```

---

## 🎵 Album Endpoints

### 1. Create Album - Single Artist

**Endpoint**: `POST /api/albums`  
**Auth**: Trusted Member required  
**Scenario**: Simple album with one artist and tracks

```bash
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "The Dark Side of the Moon",
    "yearOfRelease": 1973,
    "artistNames": ["Pink Floyd"],
    "genres": ["Progressive Rock", "Psychedelic Rock"],
    "tracks": [
      {
        "title": "Speak to Me",
        "trackNumber": 1,
        "durationInSeconds": 90
      },
      {
        "title": "Breathe",
        "trackNumber": 2,
        "durationInSeconds": 163
      },
      {
        "title": "On the Run",
        "trackNumber": 3,
        "durationInSeconds": 216
      },
      {
        "title": "Time",
        "trackNumber": 4,
        "durationInSeconds": 413
      },
      {
        "title": "Money",
        "trackNumber": 5,
        "durationInSeconds": 382
      }
    ]
  }'
```

### 2. Create Album - Collaboration (Multiple Artists)

**Scenario**: Album with multiple main artists

```bash
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Watch the Throne",
    "yearOfRelease": 2011,
    "artistNames": ["Jay-Z", "Kanye West"],
    "genres": ["Hip Hop", "Rap"],
    "tracks": [
      {
        "title": "No Church in the Wild",
        "trackNumber": 1,
        "durationInSeconds": 292
      },
      {
        "title": "Lift Off",
        "trackNumber": 2,
        "durationInSeconds": 260
      },
      {
        "title": "Niggas in Paris",
        "trackNumber": 3,
        "durationInSeconds": 219
      }
    ]
  }'
```

### 3. Create Album - Featured Artists on Tracks

**Scenario**: Album artist with featured artists on specific tracks

```bash
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "good kid, m.A.A.d city",
    "yearOfRelease": 2012,
    "artistNames": ["Kendrick Lamar"],
    "genres": ["Hip Hop", "West Coast Hip Hop"],
    "tracks": [
      {
        "title": "Sherane a.k.a Master Splinters Daughter",
        "trackNumber": 1,
        "durationInSeconds": 274
      },
      {
        "title": "Bitch, Dont Kill My Vibe",
        "trackNumber": 2,
        "durationInSeconds": 310
      },
      {
        "title": "Backseat Freestyle",
        "trackNumber": 3,
        "durationInSeconds": 213
      },
      {
        "title": "Poetic Justice",
        "trackNumber": 4,
        "durationInSeconds": 301,
        "artistNames": ["Kendrick Lamar", "Drake"]
      },
      {
        "title": "Swimming Pools (Drank)",
        "trackNumber": 5,
        "durationInSeconds": 313
      }
    ]
  }'
```

### 4. Create Album - Various Artists Compilation

**Scenario**: Compilation where each track has different artists

```bash
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Now Thats What I Call Music! 50",
    "yearOfRelease": 2020,
    "artistNames": ["Various Artists"],
    "genres": ["Pop", "Rock", "Hip Hop"],
    "tracks": [
      {
        "title": "Blinding Lights",
        "trackNumber": 1,
        "durationInSeconds": 200,
        "artistNames": ["The Weeknd"]
      },
      {
        "title": "Watermelon Sugar",
        "trackNumber": 2,
        "durationInSeconds": 174,
        "artistNames": ["Harry Styles"]
      },
      {
        "title": "Levitating",
        "trackNumber": 3,
        "durationInSeconds": 203,
        "artistNames": ["Dua Lipa"]
      },
      {
        "title": "Save Your Tears",
        "trackNumber": 4,
        "durationInSeconds": 215,
        "artistNames": ["The Weeknd", "Ariana Grande"]
      }
    ]
  }'
```

### 5. Create Album - No Tracks (Minimal)

**Scenario**: Create album with just basic info

```bash
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Abbey Road",
    "yearOfRelease": 1969,
    "artistNames": ["The Beatles"],
    "genres": ["Rock", "Pop"],
    "tracks": []
  }'
```

### 6. Create Album - Tracks Without Duration

**Scenario**: Tracks where duration is optional

```bash
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Nevermind",
    "yearOfRelease": 1991,
    "artistNames": ["Nirvana"],
    "genres": ["Grunge", "Alternative Rock"],
    "tracks": [
      {
        "title": "Smells Like Teen Spirit",
        "trackNumber": 1
      },
      {
        "title": "In Bloom",
        "trackNumber": 2
      },
      {
        "title": "Come as You Are",
        "trackNumber": 3
      }
    ]
  }'
```

### 7. Get Album by ID

**Endpoint**: `GET /api/albums/{id}`  
**Auth**: None required

```bash
# Replace {album-id} with actual ID from create response
curl -X GET http://localhost:5000/api/albums/{album-id}
```

### 8. Get Album by Slug

**Endpoint**: `GET /api/albums/{slug}`  
**Auth**: None required

```bash
curl -X GET http://localhost:5000/api/albums/the-dark-side-of-the-moon-1973
```

### 9. Get All Albums (No Filters)

**Endpoint**: `GET /api/albums`  
**Auth**: None required

```bash
curl -X GET http://localhost:5000/api/albums
```

### 10. Get All Albums - With Pagination

**Query Parameters**: `page`, `pageSize`

```bash
curl -X GET "http://localhost:5000/api/albums?page=1&pageSize=10"
```

### 11. Get All Albums - Filter by Title

**Query Parameter**: `title`

```bash
curl -X GET "http://localhost:5000/api/albums?title=dark"
```

### 12. Get All Albums - Filter by Year

**Query Parameter**: `year`

```bash
curl -X GET "http://localhost:5000/api/albums?year=1973"
```

### 13. Get All Albums - Combined Filters

**Query Parameters**: Multiple filters

```bash
curl -X GET "http://localhost:5000/api/albums?title=moon&year=1973&page=1&pageSize=5"
```

### 14. Get All Albums - Sort by Title (Ascending)

**Query Parameter**: `sortBy`

```bash
curl -X GET "http://localhost:5000/api/albums?sortBy=title"
```

### 15. Get All Albums - Sort by Title (Descending)

```bash
curl -X GET "http://localhost:5000/api/albums?sortBy=-title"
```

### 16. Get All Albums - Sort by Year

```bash
curl -X GET "http://localhost:5000/api/albums?sortBy=yearOfRelease"
```

### 17. Get All Albums - Sort Descending

```bash
curl -X GET "http://localhost:5000/api/albums?sortBy=-yearOfRelease"
```

### 18. Update Album - Change Title and Year

**Endpoint**: `PUT /api/albums/{id}`  
**Auth**: Trusted Member required

```bash
curl -X PUT http://localhost:5000/api/albums/{album-id} \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "The Dark Side of the Moon (Remastered)",
    "yearOfRelease": 1973,
    "artistNames": ["Pink Floyd"],
    "genres": ["Progressive Rock", "Psychedelic Rock"],
    "tracks": [
      {
        "title": "Speak to Me",
        "trackNumber": 1,
        "durationInSeconds": 90
      },
      {
        "title": "Breathe",
        "trackNumber": 2,
        "durationInSeconds": 163
      }
    ]
  }'
```

### 19. Update Album - Change Artists

**Scenario**: Update album to have different artists

```bash
curl -X PUT http://localhost:5000/api/albums/{album-id} \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Watch the Throne (Deluxe)",
    "yearOfRelease": 2011,
    "artistNames": ["Jay-Z", "Kanye West", "Frank Ocean"],
    "genres": ["Hip Hop", "Rap"],
    "tracks": [
      {
        "title": "No Church in the Wild",
        "trackNumber": 1,
        "durationInSeconds": 292
      }
    ]
  }'
```

### 20. Update Album - Add More Tracks

**Scenario**: Expand track list

```bash
curl -X PUT http://localhost:5000/api/albums/{album-id} \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Abbey Road",
    "yearOfRelease": 1969,
    "artistNames": ["The Beatles"],
    "genres": ["Rock", "Pop"],
    "tracks": [
      {
        "title": "Come Together",
        "trackNumber": 1,
        "durationInSeconds": 259
      },
      {
        "title": "Something",
        "trackNumber": 2,
        "durationInSeconds": 182
      },
      {
        "title": "Maxwells Silver Hammer",
        "trackNumber": 3,
        "durationInSeconds": 207
      },
      {
        "title": "Oh! Darling",
        "trackNumber": 4,
        "durationInSeconds": 206
      },
      {
        "title": "Here Comes the Sun",
        "trackNumber": 5,
        "durationInSeconds": 185
      }
    ]
  }'
```

### 21. Delete Album

**Endpoint**: `DELETE /api/albums/{id}`  
**Auth**: Admin required

```bash
curl -X DELETE http://localhost:5000/api/albums/{album-id} \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

---

## ⭐ Rating Endpoints

### 22. Rate an Album (1-5 stars)

**Endpoint**: `PUT /api/albums/{id}/ratings`  
**Auth**: Any authenticated user

```bash
# Rate 5 stars
curl -X PUT http://localhost:5000/api/albums/{album-id}/ratings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_USER_TOKEN" \
  -d '{
    "rating": 5
  }'
```

### 23. Rate an Album (1 star)

```bash
curl -X PUT http://localhost:5000/api/albums/{album-id}/ratings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_USER_TOKEN" \
  -d '{
    "rating": 1
  }'
```

### 24. Update Rating (Change from 5 to 3)

**Note**: PUT on same album updates the rating

```bash
curl -X PUT http://localhost:5000/api/albums/{album-id}/ratings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_USER_TOKEN" \
  -d '{
    "rating": 3
  }'
```

### 25. Delete Rating

**Endpoint**: `DELETE /api/albums/{id}/ratings`  
**Auth**: Any authenticated user

```bash
curl -X DELETE http://localhost:5000/api/albums/{album-id}/ratings \
  -H "Authorization: Bearer YOUR_USER_TOKEN"
```

### 26. Get My Ratings

**Endpoint**: `GET /api/ratings/me`  
**Auth**: Any authenticated user

```bash
curl -X GET http://localhost:5000/api/ratings/me \
  -H "Authorization: Bearer YOUR_USER_TOKEN"
```

---

## 🧪 Test Scenarios

### Scenario 1: Complete Album Lifecycle

**Test the full CRUD cycle**

```bash
# 1. Create Album
RESPONSE=$(curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Test Album",
    "yearOfRelease": 2024,
    "artistNames": ["Test Artist"],
    "genres": ["Test Genre"],
    "tracks": [
      {
        "title": "Test Track",
        "trackNumber": 1,
        "durationInSeconds": 180
      }
    ]
  }')

# Extract ID from response (requires jq)
ALBUM_ID=$(echo $RESPONSE | jq -r '.id')

# 2. Get Album by ID
curl -X GET http://localhost:5000/api/albums/$ALBUM_ID

# 3. Update Album
curl -X PUT http://localhost:5000/api/albums/$ALBUM_ID \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Test Album Updated",
    "yearOfRelease": 2024,
    "artistNames": ["Test Artist"],
    "genres": ["Test Genre", "New Genre"],
    "tracks": [
      {
        "title": "Test Track Updated",
        "trackNumber": 1,
        "durationInSeconds": 200
      }
    ]
  }'

# 4. Rate Album
curl -X PUT http://localhost:5000/api/albums/$ALBUM_ID/ratings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_USER_TOKEN" \
  -d '{"rating": 4}'

# 5. Get Album with Rating
curl -X GET http://localhost:5000/api/albums/$ALBUM_ID \
  -H "Authorization: Bearer YOUR_USER_TOKEN"

# 6. Delete Rating
curl -X DELETE http://localhost:5000/api/albums/$ALBUM_ID/ratings \
  -H "Authorization: Bearer YOUR_USER_TOKEN"

# 7. Delete Album
curl -X DELETE http://localhost:5000/api/albums/$ALBUM_ID \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

### Scenario 2: Artist Reusability Test

**Verify artists are reused across albums**

```bash
# Create first album with "The Beatles"
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Abbey Road",
    "yearOfRelease": 1969,
    "artistNames": ["The Beatles"],
    "genres": ["Rock"],
    "tracks": []
  }'

# Create second album with "The Beatles" (should reuse same artist)
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Let It Be",
    "yearOfRelease": 1970,
    "artistNames": ["The Beatles"],
    "genres": ["Rock"],
    "tracks": []
  }'

# Both albums should show same artist ID in response
```

### Scenario 3: Case-Insensitive Artist Matching

**Verify "The Beatles" = "the beatles"**

```bash
# Create with "The Beatles"
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Album One",
    "yearOfRelease": 1969,
    "artistNames": ["The Beatles"],
    "genres": ["Rock"],
    "tracks": []
  }'

# Create with "the beatles" (lowercase)
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Album Two",
    "yearOfRelease": 1970,
    "artistNames": ["the beatles"],
    "genres": ["Rock"],
    "tracks": []
  }'

# Should still reuse the same artist
```

### Scenario 4: Pagination Test

**Test large result sets**

```bash
# Get page 1
curl -X GET "http://localhost:5000/api/albums?page=1&pageSize=5"

# Get page 2
curl -X GET "http://localhost:5000/api/albums?page=2&pageSize=5"

# Get page 3
curl -X GET "http://localhost:5000/api/albums?page=3&pageSize=5"

# Verify total count is consistent across pages
```

### Scenario 5: Multiple User Ratings

**Test that different users can rate the same album**

```bash
ALBUM_ID="your-album-id-here"

# User 1 rates 5 stars
curl -X PUT http://localhost:5000/api/albums/$ALBUM_ID/ratings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer USER1_TOKEN" \
  -d '{"rating": 5}'

# User 2 rates 3 stars
curl -X PUT http://localhost:5000/api/albums/$ALBUM_ID/ratings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer USER2_TOKEN" \
  -d '{"rating": 3}'

# User 3 rates 4 stars
curl -X PUT http://localhost:5000/api/albums/$ALBUM_ID/ratings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer USER3_TOKEN" \
  -d '{"rating": 4}'

# Get album (should show average rating of 4.0)
curl -X GET http://localhost:5000/api/albums/$ALBUM_ID
```

---

## ❌ Error Cases to Test

### 1. Invalid Rating (Out of Range)

```bash
# Rating 0 (should fail)
curl -X PUT http://localhost:5000/api/albums/{album-id}/ratings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_USER_TOKEN" \
  -d '{"rating": 0}'

# Rating 6 (should fail)
curl -X PUT http://localhost:5000/api/albums/{album-id}/ratings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_USER_TOKEN" \
  -d '{"rating": 6}'
```

### 2. Missing Required Fields

```bash
# Missing title
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "yearOfRelease": 2024,
    "artistNames": ["Artist"],
    "genres": ["Genre"],
    "tracks": []
  }'

# Missing artists
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Album",
    "yearOfRelease": 2024,
    "genres": ["Genre"],
    "tracks": []
  }'
```

### 3. Duplicate Track Numbers

```bash
# Two tracks with same number (should fail)
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Test Album",
    "yearOfRelease": 2024,
    "artistNames": ["Artist"],
    "genres": ["Genre"],
    "tracks": [
      {
        "title": "Track 1",
        "trackNumber": 1,
        "durationInSeconds": 180
      },
      {
        "title": "Track 2",
        "trackNumber": 1,
        "durationInSeconds": 200
      }
    ]
  }'
```

### 4. Unauthorized Access

```bash
# Try to create without token (should fail 401)
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test",
    "yearOfRelease": 2024,
    "artistNames": ["Artist"],
    "genres": ["Genre"],
    "tracks": []
  }'

# Try to delete with regular user token (should fail 403)
curl -X DELETE http://localhost:5000/api/albums/{album-id} \
  -H "Authorization: Bearer YOUR_REGULAR_USER_TOKEN"
```

### 5. Non-Existent Album

```bash
# Get non-existent ID (should return 404)
curl -X GET http://localhost:5000/api/albums/00000000-0000-0000-0000-000000000000

# Get non-existent slug (should return 404)
curl -X GET http://localhost:5000/api/albums/this-album-does-not-exist-9999
```

### 6. Future Year Validation

```bash
# Album from future year (should fail)
curl -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TRUSTED_MEMBER_TOKEN" \
  -d '{
    "title": "Future Album",
    "yearOfRelease": 2030,
    "artistNames": ["Artist"],
    "genres": ["Genre"],
    "tracks": []
  }'
```

---

## 📊 Response Examples

### Successful Album Creation (201 Created)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "The Dark Side of the Moon",
  "slug": "the-dark-side-of-the-moon-1973",
  "yearOfRelease": 1973,
  "rating": null,
  "userRating": null,
  "artists": [
    {
      "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "name": "Pink Floyd",
      "slug": "pink-floyd",
      "country": null,
      "yearFormed": null
    }
  ],
  "genres": ["Progressive Rock", "Psychedelic Rock"],
  "tracks": [
    {
      "id": "1234-5678-90ab-cdef",
      "title": "Speak to Me",
      "trackNumber": 1,
      "durationInSeconds": 90,
      "artists": []
    },
    {
      "id": "2345-6789-0abc-def1",
      "title": "Breathe",
      "trackNumber": 2,
      "durationInSeconds": 163,
      "artists": []
    }
  ]
}
```

### Get All Albums Response (200 OK)

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "The Dark Side of the Moon",
      "slug": "the-dark-side-of-the-moon-1973",
      "yearOfRelease": 1973,
      "rating": 4.5,
      "userRating": 5,
      "artists": [...],
      "genres": [...],
      "tracks": [...]
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 42
}
```

### Validation Error (400 Bad Request)

```json
{
  "errors": [
    {
      "propertyName": "Title",
      "message": "Title is required",
      "attemptedValue": null
    },
    {
      "propertyName": "Artists",
      "message": "At least one artist is required",
      "attemptedValue": []
    }
  ]
}
```

### Not Found (404)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "traceId": "00-..."
}
```

---

## 🔧 Tips for Testing

1. **Use a REST Client**: Consider using Postman, Insomnia, or HTTPie for easier testing
2. **Save IDs**: Keep track of created album IDs for subsequent operations
3. **Test Order**: Create albums before rating/updating them
4. **Multiple Users**: Test with different user tokens to verify ratings work correctly
5. **Check Database**: Verify artists are actually being reused (same ID across albums)
6. **Performance**: Test pagination with large datasets
7. **Edge Cases**: Try empty tracks, long titles, special characters

---

## 🚀 Quick Start Testing Sequence

```bash
# 1. Get a token
TOKEN=$(curl -s -X POST http://localhost:5001/token \
  -H "Content-Type: application/json" \
  -d '{"userId":"00000000-0000-0000-0000-000000000002","email":"user@example.com","customClaims":{"trusted_member":"true"}}' \
  | jq -r '.token')

# 2. Create an album
ALBUM=$(curl -s -X POST http://localhost:5000/api/albums \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Test Album","yearOfRelease":2024,"artistNames":["Test Artist"],"genres":["Rock"],"tracks":[{"title":"Track 1","trackNumber":1}]}')

# 3. Extract album ID
ALBUM_ID=$(echo $ALBUM | jq -r '.id')

# 4. Get the album
curl -s -X GET http://localhost:5000/api/albums/$ALBUM_ID | jq

# 5. Rate it
curl -s -X PUT http://localhost:5000/api/albums/$ALBUM_ID/ratings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"rating":5}' | jq

# 6. Get all albums
curl -s -X GET http://localhost:5000/api/albums | jq
```

---

**Happy Testing! 🎵**
