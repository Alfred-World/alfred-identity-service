# Alfred Identity Service

OAuth 2.0 / OpenID Connect Identity Provider cho Alfred Platform.

## 🚀 Quick Start

### 1. Cấu hình Hosts

Thêm vào `/etc/hosts`:

```bash
127.0.0.1 api.test
127.0.0.1 gateway.test
127.0.0.1 identity.test
```

### 2. Cài đặt Dependencies

```bash
dotnet restore
```

### 3. Cấu hình Database

Copy `.env.example` sang `.env` và cấu hình PostgreSQL:

```bash
cp .env.example .env
```

Cấu hình database trong `.env`:
```env
DB_HOST=localhost
DB_PORT=5432
DB_NAME=alfred_identity
DB_USER=postgres
DB_PASSWORD=your_password
```

### 4. Chạy Migration

```bash
dotnet ef database update --project src/Alfred.Identity.Infrastructure --startup-project src/Alfred.Identity.WebApi
```

Hoặc sử dụng Makefile:
```bash
make migrate
```

### 5. Seed Data (Development)

Để seed dữ liệu mẫu bao gồm:
- Signing keys cho JWT
- Roles (Admin, User)
- Admin user (admin@alfred.com / Admin@123)
- OAuth2 Applications (identity-web, gateway, core-api)

Chạy trong Production mode hoặc sử dụng CLI:
```bash
# Option 1: Run in Production mode (auto seed)
ASPNETCORE_ENVIRONMENT=Production dotnet run --project src/Alfred.Identity.WebApi

# Option 2: Using CLI
dotnet run --project src/Alfred.Identity.Cli seed
```

**Default Client Secret (Development)**: `alfred-identity-client-secret-2026`

### 6. Run Application

```bash
dotnet run --project src/Alfred.Identity.WebApi
```

Hoặc sử dụng Makefile:
```bash
make run
```

Service sẽ chạy tại: `http://api.test:8100`

## 🏗️ Architecture

### SSO Flow

1. **User Access Protected Resource** → Gateway redirects to Identity Service
2. **Identity Service** checks authentication cookie
3. **If not authenticated** → Redirect to Identity Web (identity.test:7100)
4. **User Login** → Identity Web calls SSO Login API → Sets cookie
5. **Redirect back** → Authorization endpoint → Generate code
6. **Exchange code for token** → Gateway/Client gets access token

### Endpoints

- **SSO Login**: `POST /api/v1/identity/auth/sso-login`
- **OAuth2 Authorize**: `GET /connect/authorize`
- **Token Exchange**: `POST /connect/token`
- **User Info**: `GET /connect/userinfo`

### Domain Configuration

- **Identity Service API**: `api.test:8100` - Backend API
- **Identity Web UI**: `identity.test:7100` - Login UI
- **Gateway**: `gateway.test:8080` - API Gateway

### Cookie Domain

Cookie được set với domain `.test` để share across subdomains:
- `api.test:8100`
- `identity.test:7100`
- `gateway.test:8080`

## 🔧 Development

### Available Commands (Makefile)

```bash
make build          # Build solution
make run            # Run application
make migrate        # Run database migrations
make migration      # Create new migration
make test           # Run tests
make clean          # Clean build artifacts
```

### Environment Variables

Xem `.env.example` để biết danh sách đầy đủ các biến môi trường.

## 📦 Seeded Data

### Default Users

| Email | Password | Role |
|-------|----------|------|
| admin@alfred.com | Admin@123 | Admin |
| user@alfred.com | User@123 | User |

### OAuth2 Applications

| Client ID | Display Name | Redirect URIs |
|-----------|--------------|---------------|
| alfred-identity-web | Alfred Identity Web | http://identity.test:7100/callback |
| alfred-gateway | Alfred Gateway | http://gateway.test:8080/callback |
| alfred-core-api | Alfred Core API | http://api.test:5001/callback |

## 🔐 Security Notes

- Cookie domain: `.test` (development)
- Client secret được hash với BCrypt
- JWT signing keys được auto-generate khi seed
- CORS được cấu hình cho identity.test và gateway.test
