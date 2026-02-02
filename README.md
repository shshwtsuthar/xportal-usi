# USI Web API

REST API wrapper for Australian Government **USI (Unique Student Identifier)** verification services. Use this service from your Next.js (or other) applications to verify USIs and fetch country reference data.

---

## Base URL

- **Local:** `https://localhost:7xxx` or `http://localhost:5xxx` (see `launchSettings.json` or your host)
- **Production:** Your Azure App Service URL (e.g. `https://your-app.azurewebsites.net`)

Obtain the exact base URL from your deployment or environment configuration.

---

## Authentication

All USI endpoints require **API key authentication**. Send the key on every request.

| Header name   | Description                          |
|---------------|--------------------------------------|
| `X-Api-Key`   | Your API key (required for USI APIs) |

**Example:**

```http
X-Api-Key: your-api-key-here
```

- **Health** (`GET /health`) does **not** require the API key; use it for load balancers and readiness checks.
- **All other endpoints** require a valid `X-Api-Key`. Missing or invalid key returns **401 Unauthorized**.

---

## Web API Endpoints

### 1. Health check (no auth)

| Method | Path     | Auth | Description                    |
|--------|----------|------|--------------------------------|
| `GET`  | `/health`| No   | Service health for probes      |

**Response (200 OK):**

```json
{
  "status": "Healthy",
  "timestamp": "2025-02-02T12:00:00Z",
  "service": "USI Web API"
}
```

---

### 2. Verify a single USI

| Method | Path               | Auth | Description        |
|--------|--------------------|------|--------------------|
| `POST` | `/api/usi/verify`  | Yes  | Verify one USI     |

**Request body:** `application/json`

| Field        | Type   | Required | Description |
|-------------|--------|----------|-------------|
| `usi`       | string | Yes      | 10-character USI |
| `dateOfBirth` | string (ISO date) | Yes | e.g. `"2000-01-15"` |
| `singleName`   | string | No*  | Full name in one field |
| `firstName`   | string | No*  | First name |
| `familyName`  | string | No*  | Family name |

\* Provide either **`singleName`** OR both **`firstName`** and **`familyName`**.

**Example request:**

```json
{
  "usi": "1234567890",
  "dateOfBirth": "2000-05-20",
  "firstName": "Jane",
  "familyName": "Smith"
}
```

**Response (200 OK):**

```json
{
  "isValid": true,
  "usi": "1234567890",
  "verificationStatus": "Valid",
  "message": null,
  "recordId": 1
}
```

**Error responses:** `400` (validation), `401` (missing/invalid API key), `500` (service error).

---

### 3. Bulk verify USIs

| Method | Path                  | Auth | Description         |
|--------|-----------------------|------|---------------------|
| `POST` | `/api/usi/bulk-verify`| Yes  | Verify multiple USIs|

**Request body:** `application/json`

| Field           | Type                | Required | Description |
|----------------|---------------------|----------|-------------|
| `verifications`| array of objects    | Yes      | Same shape as single verify (see above) |

**Example request:**

```json
{
  "verifications": [
    {
      "usi": "1234567890",
      "dateOfBirth": "2000-05-20",
      "firstName": "Jane",
      "familyName": "Smith"
    },
    {
      "usi": "0987654321",
      "dateOfBirth": "1998-11-10",
      "singleName": "John Doe"
    }
  ]
}
```

**Response (200 OK):**

```json
{
  "totalRequested": 2,
  "validCount": 1,
  "invalidCount": 1,
  "results": [
    {
      "isValid": true,
      "usi": "1234567890",
      "verificationStatus": "Valid",
      "message": null,
      "recordId": 1
    },
    {
      "isValid": false,
      "usi": "0987654321",
      "verificationStatus": "Invalid",
      "message": null,
      "recordId": 2
    }
  ]
}
```

**Error responses:** `400`, `401`, `500`.

---

### 4. Get countries (reference data)

| Method | Path              | Auth | Description          |
|--------|-------------------|------|----------------------|
| `GET`  | `/api/usi/countries` | Yes  | List country codes/names |

**Response (200 OK):**

```json
[
  { "code": "AU", "name": "Australia" },
  { "code": "NZ", "name": "New Zealand" }
]
```

**Error responses:** `401`, `500`.

---

## Error response shape

On `400` or `500`, the body is:

```json
{
  "error": "Short message",
  "details": "Longer or technical details",
  "timestamp": "2025-02-02T12:00:00Z"
}
```

---

## Next.js integration

### 1. Environment variables

Store the API base URL and API key in **server-side** environment variables (never expose the key to the browser).

**.env.local (or Azure App Settings / Vercel env):**

```env
USI_API_BASE_URL=https://your-usi-api.azurewebsites.net
USI_API_KEY=your-api-key-here
```

### 2. Call from server-side only

Call the USI API from **API Routes** (e.g. `app/api/verify-usi/route.ts`) or **Server Components / Server Actions**. Do not call it from client components with the API key.

**Example (Next.js App Router API route):**

```ts
// app/api/verify-usi/route.ts
export async function POST(request: Request) {
  const body = await request.json();

  const res = await fetch(`${process.env.USI_API_BASE_URL}/api/usi/verify`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-Api-Key": process.env.USI_API_KEY!,
    },
    body: JSON.stringify(body),
  });

  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    return Response.json(
      { error: err.error ?? "USI verification failed", details: err.details },
      { status: res.status }
    );
  }

  const data = await res.json();
  return Response.json(data);
}
```

### 3. CORS

The API allows requests only from origins configured in **AllowedOrigins** (e.g. your Next.js origin). Ensure your production domain is included in the API’s configuration so browser requests from your frontend are allowed.

### 4. Checklist for engineers

- [ ] Use **base URL** and **API key** from environment variables (e.g. `USI_API_BASE_URL`, `USI_API_KEY`).
- [ ] Send **`X-Api-Key`** on every request to `/api/usi/*`.
- [ ] Use **server-side** code (API routes, Server Actions, getServerSideProps) when calling the USI API; do not put the API key in client-side code.
- [ ] Send **`Content-Type: application/json`** for POST bodies.
- [ ] Handle **401** (check API key and env), **400** (validation), and **500** (show user-friendly message, log details).
- [ ] Use **`GET /health`** without a key for health checks only.

---

## Swagger (development)

When running in **Development**, Swagger UI is available at:

- **Swagger UI:** `{baseUrl}/swagger`
- **OpenAPI JSON:** `{baseUrl}/swagger/v1/swagger.json`

You can add the API key in Swagger via “Authorize” if the project supports it, or test with a tool that sends the `X-Api-Key` header.
