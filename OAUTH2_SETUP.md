# OAuth2 Setup Guide

This guide explains how to set up OAuth2 authentication with Google and Microsoft for the Mootable project.

## Prerequisites

Before you begin, you'll need:
- A Google Cloud Platform account
- A Microsoft Azure account
- Your application's production URL (for redirect URIs)

## Google OAuth2 Setup

### 1. Create a Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click "Create Project" or select an existing project
3. Give your project a name (e.g., "Mootable")

### 2. Enable Google+ API

1. Go to "APIs & Services" > "Library"
2. Search for "Google+ API"
3. Click on it and press "Enable"

### 3. Create OAuth 2.0 Credentials

1. Go to "APIs & Services" > "Credentials"
2. Click "Create Credentials" > "OAuth client ID"
3. If prompted, configure the OAuth consent screen:
   - Choose "External" for user type
   - Fill in application name: "Mootable"
   - Add your email and support email
   - Add authorized domains (e.g., `yourdomain.com`)
   - Save and continue

4. For Application type, select "Web application"
5. Name: "Mootable Web Client"
6. Add Authorized JavaScript origins:
   - `http://localhost:3000` (development)
   - `http://localhost:5000` (backend)
   - `https://yourdomain.com` (production)

7. Add Authorized redirect URIs:
   - `http://localhost:5000/api/oauth2/callback/google` (development)
   - `https://yourapi.com/api/oauth2/callback/google` (production)

8. Click "Create"
9. Copy the Client ID and Client Secret

### 4. Update Configuration

In `appsettings.json`:
```json
"OAuth2": {
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  }
}
```

## Microsoft OAuth2 Setup

### 1. Register an Application in Azure

1. Go to [Azure Portal](https://portal.azure.com/)
2. Navigate to "Azure Active Directory"
3. Go to "App registrations" > "New registration"

### 2. Configure the Application

1. Name: "Mootable"
2. Supported account types: "Accounts in any organizational directory and personal Microsoft accounts"
3. Redirect URI:
   - Platform: "Web"
   - URI: `http://localhost:5000/api/oauth2/callback/microsoft` (development)
4. Click "Register"

### 3. Get Client Credentials

1. Copy the "Application (client) ID" - this is your Client ID
2. Go to "Certificates & secrets"
3. Click "New client secret"
4. Add a description: "Mootable Client Secret"
5. Select expiration (recommended: 24 months)
6. Click "Add"
7. Copy the secret value immediately (it won't be shown again)

### 4. Configure API Permissions

1. Go to "API permissions"
2. Click "Add a permission"
3. Choose "Microsoft Graph"
4. Select "Delegated permissions"
5. Add these permissions:
   - User.Read
   - email
   - profile
   - openid
6. Click "Add permissions"

### 5. Add Additional Redirect URIs

1. Go to "Authentication"
2. Add redirect URIs:
   - `http://localhost:5000/api/oauth2/callback/microsoft`
   - `https://yourapi.com/api/oauth2/callback/microsoft` (production)
3. Under "Implicit grant and hybrid flows", check:
   - Access tokens
   - ID tokens
4. Save

### 6. Update Configuration

In `appsettings.json`:
```json
"OAuth2": {
  "Microsoft": {
    "ClientId": "YOUR_MICROSOFT_CLIENT_ID",
    "ClientSecret": "YOUR_MICROSOFT_CLIENT_SECRET"
  }
}
```

## Environment Variables (Production)

For production, use environment variables or Azure Key Vault / AWS Secrets Manager:

```bash
# Google OAuth2
OAuth2__Google__ClientId=your_google_client_id
OAuth2__Google__ClientSecret=your_google_client_secret

# Microsoft OAuth2
OAuth2__Microsoft__ClientId=your_microsoft_client_id
OAuth2__Microsoft__ClientSecret=your_microsoft_client_secret
```

## Testing OAuth2 Flow

### 1. Start Backend

```bash
cd mootable-back/src/WebAPI
dotnet run
```

### 2. Start Frontend

```bash
cd mootable-front
npm run dev
```

### 3. Test Login Flow

1. Navigate to http://localhost:3000/login
2. Click on "Google" or "Microsoft" button
3. You'll be redirected to the provider's login page
4. After successful authentication, you'll be redirected back to the application
5. Check if user is created/logged in successfully

## Security Considerations

1. **Never commit credentials**: Always use environment variables or secret management services
2. **Use HTTPS in production**: OAuth2 requires secure connections
3. **Validate redirect URIs**: Only allow known redirect URIs
4. **Token storage**: Store tokens securely (httpOnly cookies or secure storage)
5. **Scope limitation**: Only request necessary permissions
6. **Regular rotation**: Rotate client secrets regularly

## Troubleshooting

### Common Issues

1. **"Redirect URI mismatch"**
   - Ensure the redirect URI in your app matches exactly what's configured in Google/Azure
   - Check for trailing slashes, protocol (http vs https), and port numbers

2. **"Invalid client"**
   - Verify Client ID and Client Secret are correct
   - Check if the secret has expired (Microsoft)
   - Ensure the application is enabled

3. **"Access denied"**
   - Check if the user has granted necessary permissions
   - Verify API permissions in Azure/Google Console

4. **CORS errors**
   - Add frontend URL to CORS configuration in `appsettings.json`
   - Ensure cookies are enabled for cross-origin requests

## Additional Providers

To add more OAuth2 providers (GitHub, Discord, etc.):

1. Install the appropriate NuGet package:
   ```bash
   dotnet add package AspNet.Security.OAuth.GitHub
   dotnet add package AspNet.Security.OAuth.Discord
   ```

2. Update `ExternalLoginProviders` in `ExternalLogin.cs`

3. Configure in `DependencyInjection.cs`:
   ```csharp
   authBuilder.AddGitHub(options => {
       options.ClientId = configuration["OAuth2:GitHub:ClientId"];
       options.ClientSecret = configuration["OAuth2:GitHub:ClientSecret"];
   });
   ```

4. Add configuration to `appsettings.json`

5. Update frontend `OAuth2LoginButtons.tsx` with new provider button

## Resources

- [Google OAuth2 Documentation](https://developers.google.com/identity/protocols/oauth2)
- [Microsoft Identity Platform](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [ASP.NET Core Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/)
- [OAuth 2.0 Security Best Practices](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics)