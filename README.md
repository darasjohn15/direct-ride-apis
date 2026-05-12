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

Most endpoints require a bearer token from `POST /auth/login`. Public endpoints are noted below.

### Auth

| Method | Endpoint | Auth | Description |
| --- | --- | --- | --- |
| `POST` | `/auth/login` | Public | Authenticate with email and password. Returns a JWT and basic user details. |

Request body:

```json
{
  "email": "driver@example.com",
  "password": "password123"
}
```

### Users

| Method | Endpoint | Auth | Description |
| --- | --- | --- | --- |
| `GET` | `/users/test` | Public | Returns a sample driver user. |
| `POST` | `/users` | Public | Create a rider or driver account. |
| `GET` | `/users/me` | Required | Get the currently authenticated user. |
| `GET` | `/users` | Required | Get all users. |
| `GET` | `/users/{id}` | Required | Get a user by ID. |
| `PUT` | `/users/{id}` | Required | Replace a user's profile fields. |
| `PATCH` | `/users/{id}` | Required | Update one or more user profile fields. |

Create user body:

```json
{
  "firstName": "Razzo",
  "lastName": "Driver",
  "email": "razzo@directride.com",
  "phoneNumber": "555-555-5555",
  "role": 1,
  "password": "password123"
}
```

Update user body:

```json
{
  "firstName": "Razzo",
  "lastName": "Driver",
  "email": "razzo@directride.com",
  "phoneNumber": "555-555-5555",
  "role": 1,
  "baseFare": 25.00
}
```

Patch user body supports any subset of `firstName`, `lastName`, `email`, `phoneNumber`, `role`, and `baseFare`.

User roles:

| Value | Role |
| --- | --- |
| `0` | Rider |
| `1` | Driver |

### Availability

| Method | Endpoint | Auth | Description |
| --- | --- | --- | --- |
| `GET` | `/availability` | Required | Get driver availability slots. Defaults to unbooked slots when `isBooked` is omitted. |
| `POST` | `/availability` | Required | Create a driver availability slot. |

`GET /availability` query filters:

| Query parameter | Type |
| --- | --- |
| `driverId` | `Guid` |
| `driverName` | `string` |
| `startTimeFrom` | `DateTime` |
| `startTimeTo` | `DateTime` |
| `endTimeFrom` | `DateTime` |
| `endTimeTo` | `DateTime` |
| `isBooked` | `bool` |
| `createdAtFrom` | `DateTime` |
| `createdAtTo` | `DateTime` |

Create availability body:

```json
{
  "driverId": "00000000-0000-0000-0000-000000000000",
  "startTime": "2026-05-11T14:00:00Z",
  "endTime": "2026-05-11T16:00:00Z"
}
```

### Ride Requests

| Method | Endpoint | Auth | Description |
| --- | --- | --- | --- |
| `GET` | `/ride-requests` | Required | Get ride requests with rider, driver, availability, fare, earnings, status, and completion details. |
| `POST` | `/ride-requests` | Required | Create a ride request and mark the availability slot as booked. Fare and driver earnings are set from the driver's base fare. |
| `PATCH` | `/ride-requests/{id}/status?status={status}` | Required | Update a ride request status. Declined requests free the availability slot; completed requests set `completedAt`. |

`GET /ride-requests` query filters:

| Query parameter | Type |
| --- | --- |
| `riderId` | `Guid` |
| `riderName` | `string` |
| `driverId` | `Guid` |
| `driverName` | `string` |
| `availabilitySlotId` | `Guid` |
| `pickupLocation` | `string` |
| `dropoffLocation` | `string` |
| `status` | `RideRequestStatus` |
| `slotStartTimeFrom` | `DateTime` |
| `slotStartTimeTo` | `DateTime` |
| `slotEndTimeFrom` | `DateTime` |
| `slotEndTimeTo` | `DateTime` |
| `createdAtFrom` | `DateTime` |
| `createdAtTo` | `DateTime` |

Create ride request body:

```json
{
  "riderId": "00000000-0000-0000-0000-000000000000",
  "driverId": "00000000-0000-0000-0000-000000000000",
  "availabilitySlotId": "00000000-0000-0000-0000-000000000000",
  "pickupLocation": "123 Main St",
  "dropoffLocation": "456 Oak Ave"
}
```

Ride request statuses:

| Value | Status |
| --- | --- |
| `0` | Pending |
| `1` | Accepted |
| `2` | Declined |
| `3` | Completed |
| `4` | Cancelled |
