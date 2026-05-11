# DirectRide APIs

DirectRide is a private ride-booking backend that allows riders to book rides directly with drivers, eliminating middleman fees.

## Tech Stack
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Docker
- xUnit (integration tests)

## Features
- User management (drivers & riders)
- Driver availability scheduling
- Ride request system
- Booking logic (prevents double-booking)
- Accept/decline ride requests
- DTO-based API design
- Integration test coverage

## Endpoints
- `POST /users`
- `GET /users`
- `POST /availability`
- `GET /availability`
- `POST /ride-requests`
- `GET /ride-requests`
- `PATCH /ride-requests/{id}/status`