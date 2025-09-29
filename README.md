# Secure User Registration with Redis and reCAPTCHA

This project demonstrates a secure user registration system using:
- ASP.NET Core with Vertical Slice Architecture
- Redis as a temporary, non-persistent data store
- Google reCAPTCHA v2 for bot protection
- Angular frontend with Angular Material

## Architecture Overview

- **Backend**: ASP.NET Core API with custom Redis-based Identity store
- **Frontend**: Angular application with reCAPTCHA integration
- **Data Storage**: Redis (temporary, non-persistent)
- **Bot Protection**: Google reCAPTCHA v2

## Prerequisites

1. .NET 8.0 SDK
2. Node.js and npm
3. Redis server (version 6.0 or higher)
4. Google reCAPTCHA v2 API keys

## Setting up reCAPTCHA

1. Go to [Google reCAPTCHA Admin](https://www.google.com/recaptcha/admin)
2. Register a new site with type "reCAPTCHA v2"
3. Add your domain (for development: localhost)
4. Copy the Site Key and Secret Key

### Configure API Keys

**Backend (.NET)**:
1. Open `SignUpApi/appsettings.json`
2. Replace `YOUR_RECAPTCHA_SECRET_KEY_HERE` with your actual reCAPTCHA secret key:

```json
{
  "Recaptcha": {
    "SecretKey": "YOUR_ACTUAL_RECAPTCHA_SECRET_KEY"
  }
}
```

**Frontend (Angular)**:
1. Open `SignUpFrontend/src/environments/environment.ts`
2. Replace `YOUR_RECAPTCHA_SITE_KEY_HERE` with your actual reCAPTCHA site key:

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001',
  reCaptchaSiteKey: 'YOUR_ACTUAL_RECAPTCHA_SITE_KEY'
};
```

## Running the Application

### Backend (.NET API)

1. Navigate to the SignUpApi directory:
```bash
cd SignUpApi
```

2. Install dependencies:
```bash
dotnet restore
```

3. Ensure Redis is running on localhost:6379

4. Run the application:
```bash
dotnet run
```

The API will be available at `https://localhost:5001`

### Frontend (Angular)

1. Navigate to the SignUpFrontend directory:
```bash
cd SignUpFrontend
```

2. Install dependencies:
```bash
npm install
```

3. Run the development server:
```bash
npm start
```

The frontend will be available at `http://localhost:4200`

## Redis Configuration

For this application to work with temporary data:
1. Ensure Redis persistence is disabled (no RDB/AOF)
2. The default configuration in this project uses `localhost:6379`
3. All user data will be lost when Redis is restarted

To run Redis without persistence, you can start it with:
```bash
redis-server --save "" --appendonly no
```

## Architecture Details

### Backend Architecture

The backend follows Vertical Slice Architecture using:
- MediatR for command/query handling
- FluentValidation for input validation
- Custom RedisUserStore implementing ASP.NET Core Identity interfaces
- UserManager for password hashing and user management

### Frontend Architecture

The frontend is built with:
- Angular with standalone components
- Angular Material for UI components
- ReactiveFormsModule for form handling
- NgxCaptcha for reCAPTCHA integration

## Security Features

- Passwords are securely hashed using ASP.NET Core Identity's default hasher
- reCAPTCHA verification happens server-side to prevent bot registrations
- Input validation both client and server-side
- Temporary data storage prevents permanent data leakage

## Data Flow

1. User fills out signup form on Angular frontend
2. reCAPTCHA validation occurs
3. Form data is sent to .NET API with reCAPTCHA token
4. Server verifies reCAPTCHA with Google
5. If verification passes, user is created using UserManager
6. UserManager persists user to Redis via custom RedisUserStore
7. Success/failure response is returned to frontend

## Temporary Data Characteristics

All user data is stored temporarily in Redis:
- Data is lost when Redis server restarts
- No permanent database is used
- Perfect for temporary user accounts or development environments