# USI Web API

REST API wrapper for the Australian Government USI (Unique Student Identifier) verification services.

## Overview

This ASP.NET Core Web API provides modern RESTful endpoints for USI verification, making it easy to integrate USI services into web applications without dealing with SOAP/WCF complexity.

## API Endpoints

### Verify Single USI
```http
POST /api/usi/verify
Content-Type: application/json

{
  "usi": "XNY5NV9WG9",
  "dateOfBirth": "2022-06-07",
  "singleName": "Amy"
}
```

**Response:**
```json
{
  "isValid": true,
  "usi": "XNY5NV9WG9",
  "verificationStatus": "Valid",
  "message": null,
  "recordId": 1
}
```

### Bulk Verify USIs
```http
POST /api/usi/bulk-verify
Content-Type: application/json

{
  "verifications": [
    {
      "usi": "XNY5NV9WG9",
      "dateOfBirth": "2022-06-07",
      "singleName": "Amy"
    },
    {
      "usi": "HQ9HHNJC3J",
      "dateOfBirth": "1986-04-22",
      "firstName": "BERT",
      "familyName": "ZYWIEC"
    }
  ]
}
```

### Get Countries
```http
GET /api/usi/countries
```

### Health Check
```http
GET /health
```

## Running Locally

1. **Prerequisites**
   - .NET 10 SDK installed
   - Valid USI test credentials (keystore-usi.xml)

2. **Start the API**
   ```bash
   cd src/Usi.WebApi
   dotnet run
   ```

3. **Access Swagger UI**
   Navigate to: `https://localhost:5001/swagger`

4. **Test with curl**
   ```bash
   curl -X POST http://localhost:5000/api/usi/verify \
     -H "Content-Type: application/json" \
     -d '{
       "usi": "XNY5NV9WG9",
       "dateOfBirth": "2022-06-07",
       "singleName": "Amy"
     }'
   ```

## Configuration

### appsettings.json
- `atoSettings`: ATO STS endpoint configuration
- `usiSettings`: USI service endpoint and org code
- `mode`: Authentication mode (IssuedToken or IssuerBinding)
- `AllowedOrigins`: CORS allowed origins for web apps

### Environment Variables
Set these in Azure App Service configuration:
- `atoSettings__id`: Your machine account ID
- `usiSettings__code`: Your organization code
- `ASPNETCORE_ENVIRONMENT`: Production/Development

## NextJS Integration

### Server-Side API Route
```typescript
// app/api/verify-usi/route.ts
export async function POST(request: Request) {
  const data = await request.json();
  
  const response = await fetch(`${process.env.USI_API_URL}/api/usi/verify`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  });

  return Response.json(await response.json());
}
```

### Client Component
```typescript
// components/UsiVerification.tsx
const verifyUsi = async (usi: string, dob: string, name: string) => {
  const response = await fetch('/api/verify-usi', {
    method: 'POST',
    body: JSON.stringify({
      usi,
      dateOfBirth: dob,
      singleName: name
    }),
  });
  
  return response.json();
};
```

## Azure Deployment

### Using Azure CLI
```bash
# Login to Azure
az login

# Create resource group
az group create --name usi-api-rg --location australiaeast

# Create App Service plan
az appservice plan create \
  --name usi-api-plan \
  --resource-group usi-api-rg \
  --sku B1 \
  --is-linux

# Create web app
az webapp create \
  --name your-usi-api \
  --resource-group usi-api-rg \
  --plan usi-api-plan \
  --runtime "DOTNET|10.0"

# Deploy
dotnet publish -c Release
cd bin/Release/net10.0/publish
zip -r deploy.zip .
az webapp deployment source config-zip \
  --resource-group usi-api-rg \
  --name your-usi-api \
  --src deploy.zip
```

### Configuration in Azure
After deployment, set these Application Settings in Azure Portal:
- `atoSettings__id`: Your production machine account
- `atoSettings__file`: Path to keystore file
- `atoSettings__password`: Keystore password
- `usiSettings__code`: Your organization code

## Security Considerations

⚠️ **Important Security Notes:**
- Never commit `keystore-usi.xml` to public repositories
- Use Azure Key Vault for production certificates
- Implement API authentication (Azure AD, API keys, etc.)
- Enable rate limiting
- Use HTTPS only in production
- Whitelist specific CORS origins

## Development

### Project Structure
```
Usi.WebApi/
├── Controllers/
│   ├── UsiController.cs       # Main USI endpoints
│   └── HealthController.cs    # Health check
├── Models/
│   ├── VerifyUsiRequest.cs
│   ├── VerifyUsiResponse.cs
│   └── ...
├── Program.cs                 # App configuration
├── appsettings.json
└── keystore-usi.xml           # Test credentials
```

### Testing Different Auth Modes
```bash
# IssuedToken mode (default)
dotnet run --launch-profile https

# IssuerBinding mode
dotnet run --launch-profile IssuerBinding
```

## Troubleshooting

**Certificate errors:**
- Ensure `keystore-usi.xml` is in the project root
- Check password in appsettings.json

**CORS errors:**
- Add your domain to `AllowedOrigins` in appsettings.json

**Connection timeouts:*
- Verify network can reach USI endpoints
- Check firewall rules in Azure

## Support

- USI Support: it@usi.gov.au
- Documentation: See main README.md
- Issues: GitHub Issues

## License

Australian Government © 2025
