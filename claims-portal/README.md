# Insurance Claims Portal

Take-home assessment prototype built with ASP.NET Core (.NET 10), Angular 19, Entity Framework Core, and PostgreSQL.

## Features and architecture

- ASP.NET Core Web API with Swagger at `/swagger`
- Angular frontend for claimant and claims officer interfaces
- PostgreSQL persistence through EF Core migrations
- JWT-based login and role-based access control
- Claim submission, status history, assignments, documents and assessment endpoints in the backend

## Prerequisites

- .NET 10 SDK
- Node.js and npm
- PostgreSQL 16 or compatible
- EF Core CLI (`dotnet-ef`) compatible with .NET 10

## Local setup

1. Create a local PostgreSQL database called `claims_portal` and a database user with appropriate permissions.
2. Configure the API's connection string and JWT signing key using .NET User Secrets (preferred), environment variables, or a **local, untracked** `appsettings.Development.json` based on `appsettings.Development.example.json`. Never commit real secrets.
3. From the `backend` directory, restore and build:

   ```powershell
   dotnet restore
   dotnet build
   ```

4. Apply the existing EF Core initial migration:

   ```powershell
   dotnet ef database update --project "src/ClaimsPortal.Claims.Infrastructure/ClaimsPortal.Claims.Infrastructure.csproj" --startup-project "src/ClaimsPortal.Claims.Api/ClaimsPortal.Claims.Api.csproj" --context ClaimsDbContext
   ```

5. Run the API:

   ```powershell
   dotnet run --project "src/ClaimsPortal.Claims.Api/ClaimsPortal.Claims.Api.csproj"
   ```

   Swagger: `http://localhost:5080/swagger` (when using the provided development launch profile).

6. In a **second terminal**, start Angular:

   ```powershell
   cd frontend
   npm ci
   npm start
   ```

   Frontend: `http://localhost:4200/login` (if the port is free).

> Note: Step 6 commands are run from the repository root (rather than inside `backend`).

## Demo data

The API supports development-only demo seeding, controlled by application configuration. Demo accounts are not guaranteed to exist in a newly migrated database. Configure a fresh development database before enabling seeding and never use demo passwords in production. See the existing demo seeder for the supported configuration.

## Tests

From the `backend` directory:

```powershell
dotnet test
```

From the `frontend` directory:

```powershell
npm run build
```

## Current limitations

The source code and EF Core migration were prepared for the assessment. Local backend startup, Swagger access and frontend rendering were observed during development, but end-to-end demo login and the full claim lifecycle were **not confirmed as passing**. Additional API integration validation is required.

## Security

Local development credentials, JWT signing keys, user-secrets, generated files, and downloaded dependencies are intentionally excluded from this repository.

## Branding and scope

This is a generic, independently developed software assessment prototype. All organization-specific branding has been removed from the supplied source code. It is not an official insurer product.
