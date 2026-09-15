# Local Development Environment Setup

This guide explains how to set up the DirectRide API locally for development and testing.

DirectRide is an ASP.NET Core Web API backed by PostgreSQL and Amazon S3. The API uses Entity Framework Core migrations, JWT authentication, CORS configuration, S3 profile-photo storage, and xUnit tests.

## Prerequisites

Install the following tools before you start:

- .NET 10 SDK
- Docker Desktop, or another Docker-compatible runtime with Docker Compose support
- Git
- Optional: PostgreSQL client tools such as `psql`
- Optional: EF Core CLI tools for manually managing migrations
- An AWS account, S3 bucket, and AWS credentials when testing profile-photo operations

Verify the main tools:

```bash
dotnet --version
docker --version
docker compose version
```

The project currently targets `net10.0`.

## Repository Layout

Important project files:

```text
.
├── direct-ride-app.sln
├── docker-compose.yml
├── DirectRide.Api/
│   ├── DirectRide.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Data/
│   ├── Migrations/
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   └── Scripts/
└── DirectRide.Api.Tests/
    └── DirectRide.Api.Tests.csproj
```

## Configuration Overview

Default local configuration lives in `DirectRide.Api/appsettings.json`.

The default local database connection string is:

```text
Host=localhost;Port=5433;Database=directride;Username=postgres;Password=password
```

The Docker Compose database exposes PostgreSQL on local port `5433` so it does not conflict with a local PostgreSQL server using the default `5432` port.

The API can read database configuration in two ways:

- `ConnectionStrings__DefaultConnection`
- Individual database environment variables

Supported individual database variables:

```text
DB_HOST
DB_PORT
DB_NAME
DB_USERNAME
DB_PASSWORD
DB_SSL_MODE
```

The app also accepts common PostgreSQL and AWS RDS-style aliases:

```text
PGHOST
PGPORT
PGDATABASE
PGUSER
PGPASSWORD
PGSSLMODE
RDS_HOSTNAME
RDS_PORT
RDS_DB_NAME
RDS_USERNAME
RDS_PASSWORD
RDS_SSL_MODE
```

JWT settings are also in `DirectRide.Api/appsettings.json`:

```json
{
  "Jwt": {
    "Key": "super-long-dev-secret-key-change-this",
    "Issuer": "DirectRide.Api",
    "Audience": "DirectRide.Client",
    "ExpiryMinutes": 60
  }
}
```

For local development, the checked-in values are enough to run the API. For shared, staged, or production-like environments, override secrets with environment variables or another secure configuration source.

### Profile-photo storage

Profile photos are stored in Amazon S3. The database stores an object key in `Users.ProfilePhotoKey`, and user API responses expose a presigned download URL that is valid for one hour.

The default S3 configuration in `DirectRide.Api/appsettings.json` is:

```json
{
  "AWS": {
    "BucketName": "direct-ride-dev-uploads",
    "Region": "us-east-1"
  }
}
```

Override the bucket name with `AWS__BucketName`. The S3 client uses the AWS SDK's standard region and credential chains, so set the region with `AWS_REGION` (or in the selected AWS profile) and authenticate with an AWS profile or the `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, and optional `AWS_SESSION_TOKEN` environment variables. The bucket must already exist. The identity used by the API needs `s3:PutObject`, `s3:DeleteObject`, and `s3:GetObject` access to `profile-photos/*` in that bucket. In AWS, prefer an ECS task role or another workload identity instead of static credentials.

The API can start without contacting S3, but profile-photo upload, deletion, and presigned URL generation require valid bucket, region, and credential configuration. The test suite replaces S3 with an in-memory test implementation and does not require AWS access.

## Setup Option 1: Run Everything With Docker Compose

From the repository root:

```bash
docker compose up --build
```

Use the root `docker-compose.yml` for this workflow. There is also a `DirectRide.Api/docker-compose.yaml` file that starts PostgreSQL only, but the root Compose file is the complete local environment for the API and database.

This starts:

- PostgreSQL container: `direct-ride-db`
- API container: `direct-ride-api`

Local URLs:

```text
API: http://localhost:8080
Health check: http://localhost:8080/health
OpenAPI document: http://localhost:8080/openapi/v1.json
PostgreSQL host port: localhost:5433
```

The API container connects to PostgreSQL using the Docker service name `db` and port `5432` inside the Compose network.

To exercise profile-photo endpoints through Docker Compose, pass AWS configuration and credentials into the `api` service (or attach an AWS workload role in the deployed environment). The checked-in Compose file does not provide AWS credentials.

Stop the containers:

```bash
docker compose down
```

Stop the containers and remove the local database volume:

```bash
docker compose down --volumes
```

Use `--volumes` only when you want to delete the local PostgreSQL data and start with a fresh database.

## Setup Option 2: Run PostgreSQL in Docker and the API Locally

This is usually the best setup for day-to-day API development because code changes are picked up by `dotnet run` without rebuilding the Docker image.

Start only PostgreSQL:

```bash
docker compose up -d db
```

Restore dependencies:

```bash
dotnet restore
```

Run the API:

```bash
dotnet run --project DirectRide.Api
```

Local URLs from `DirectRide.Api/Properties/launchSettings.json`:

```text
API: http://localhost:5049
Health check: http://localhost:5049/health
OpenAPI document: http://localhost:5049/openapi/v1.json
```

The app runs with `ASPNETCORE_ENVIRONMENT=Development` when launched through the checked-in launch profile.

## Database Migrations

The API applies EF Core migrations automatically during startup unless the environment is `Testing`.

That means this is usually enough:

```bash
docker compose up -d db
dotnet run --project DirectRide.Api
```

If you need to run migrations manually, install the EF Core CLI:

```bash
dotnet tool install --global dotnet-ef
```

Then run migrations from the API project directory:

```bash
cd DirectRide.Api
dotnet ef database update
```

If your shell cannot find `dotnet-ef` after installing it, make sure the .NET global tools directory is on your `PATH`.

## Seeding Local Data

There is a seed script for driver dashboard demo data:

```text
DirectRide.Api/Scripts/seed-driver-dashboard.sql
```

After the database is running and migrations have been applied, run:

```bash
psql "Host=localhost;Port=5433;Database=directride;Username=postgres;Password=password" -f DirectRide.Api/Scripts/seed-driver-dashboard.sql
```

The script is written to be repeatable for the seeded IDs.

If `psql` is not installed locally, you can execute the script through the PostgreSQL container:

```bash
docker exec -i direct-ride-db psql -U postgres -d directride < DirectRide.Api/Scripts/seed-driver-dashboard.sql
```

## Running Tests

Run the full test suite from the repository root:

```bash
dotnet test
```

The integration tests use an in-memory SQLite database through `CustomWebApplicationFactory`, so they do not require the local PostgreSQL container.

## Smoke Testing the API

Check health:

```bash
curl http://localhost:5049/health
```

Expected response:

```json
{
  "status": "Healthy"
}
```

Register a local user:

```bash
curl -X POST http://localhost:5049/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Sample",
    "lastName": "Driver",
    "email": "sample.driver@directride.test",
    "phoneNumber": "555-555-5555",
    "role": 1,
    "password": "password123"
  }'
```

Log in:

```bash
curl -X POST http://localhost:5049/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "sample.driver@directride.test",
    "password": "password123"
  }'
```

Most endpoints require a bearer token returned by `/auth/register` or `/auth/login`.

Example authenticated request:

```bash
TOKEN="paste-token-here"

curl http://localhost:5049/users/me \
  -H "Authorization: Bearer $TOKEN"
```

If you are running the full Docker Compose API instead of `dotnet run`, use port `8080` in the URLs.

## CORS

CORS allowed origins are configured in `DirectRide.Api/appsettings.json` under:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://direct-ride-dev-api-alb-1799009038.us-east-1.elb.amazonaws.com",
      "http://direct-ride-frontend-dev.s3-website-us-east-1.amazonaws.com"
    ]
  }
}
```

For a local frontend, add a local override in `DirectRide.Api/appsettings.Development.json` or use environment variables. Example:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173"
    ]
  }
}
```

## Environment Variable Examples

Connection string override:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=directride;Username=postgres;Password=password"
```

Individual database variable override:

```bash
export DB_HOST=localhost
export DB_PORT=5433
export DB_NAME=directride
export DB_USERNAME=postgres
export DB_PASSWORD=password
```

S3 override using an existing local AWS profile:

```bash
export AWS__BucketName=your-directride-uploads-bucket
export AWS_REGION=us-east-1
export AWS_PROFILE=your-profile
```

Run the API with those variables:

```bash
dotnet run --project DirectRide.Api
```

## Troubleshooting

### Port 5433 is already in use

Change the host-side port in `docker-compose.yml`:

```yaml
ports:
  - "5434:5432"
```

Then update the local connection string port to match.

### API cannot connect to PostgreSQL

Confirm the database container is running:

```bash
docker ps
```

Confirm the connection details:

```text
Host: localhost
Port: 5433
Database: directride
Username: postgres
Password: password
```

If the API is running inside Docker Compose, the host should be `db` and the port should be `5432`.

### Profile-photo requests fail with an AWS or S3 error

Confirm that `AWS__BucketName` identifies an existing bucket, `AWS_REGION` (or the active profile) selects its region, the AWS SDK can resolve credentials, and the active identity has object access under `profile-photos/*`. When running the API container locally, remember that host AWS profiles and environment variables are not automatically available inside the container.

### OpenAPI URL returns 404

The OpenAPI endpoint is mapped only when `ASPNETCORE_ENVIRONMENT=Development`.

When running locally through `dotnet run --project DirectRide.Api`, the launch profile sets this automatically.

### Migrations fail because `appsettings.json` cannot be found

Run EF Core commands from the API project directory:

```bash
cd DirectRide.Api
dotnet ef database update
```

The design-time `AppDbContextFactory` loads `appsettings.json` from the current directory.

### Need a fresh local database

Remove the Compose volume and restart:

```bash
docker compose down --volumes
docker compose up -d db
dotnet run --project DirectRide.Api
```

This deletes local database data.

## Useful Commands

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Start PostgreSQL only
docker compose up -d db

# Start API and PostgreSQL in Docker
docker compose up --build

# Run API locally
dotnet run --project DirectRide.Api

# Stop Docker Compose services
docker compose down
```
