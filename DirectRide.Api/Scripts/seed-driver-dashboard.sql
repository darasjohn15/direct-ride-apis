\set ON_ERROR_STOP on

BEGIN;

CREATE OR REPLACE FUNCTION pg_temp.seed_uuid(seed text)
RETURNS uuid
LANGUAGE sql
IMMUTABLE
AS $$
    SELECT (
        substr(md5(seed), 1, 8) || '-' ||
        substr(md5(seed), 9, 4) || '-' ||
        substr(md5(seed), 13, 4) || '-' ||
        substr(md5(seed), 17, 4) || '-' ||
        substr(md5(seed), 21, 12)
    )::uuid;
$$;

CREATE TEMP TABLE seed_driver_dashboard_context (
    driver_id uuid NOT NULL,
    base_fare numeric(10, 2) NOT NULL
) ON COMMIT DROP;

INSERT INTO seed_driver_dashboard_context (driver_id, base_fare)
SELECT "Id", COALESCE(NULLIF("BaseFare", 0.00), 25.00)
FROM "Users"
WHERE "Id" = 'a4f931f8-021b-4d3e-87ae-f05932389e1c'
  AND "Role" = 1;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM seed_driver_dashboard_context) THEN
        RAISE EXCEPTION 'Driver a4f931f8-021b-4d3e-87ae-f05932389e1c was not found or is not a driver.';
    END IF;
END $$;

UPDATE "Users"
SET "BaseFare" = 25.00
WHERE "Id" = 'a4f931f8-021b-4d3e-87ae-f05932389e1c'
  AND "BaseFare" = 0.00;

INSERT INTO "Users" ("Id", "FirstName", "LastName", "Email", "PhoneNumber", "Role", "CreatedAt", "BaseFare", "PasswordHash")
VALUES
    (pg_temp.seed_uuid('dashboard-rider-maya'), 'Maya', 'Bennett', 'maya.dashboard@directride.test', '555-0101', 0, now() - interval '12 days', 0.00, ''),
    (pg_temp.seed_uuid('dashboard-rider-jordan'), 'Jordan', 'Lee', 'jordan.dashboard@directride.test', '555-0102', 0, now() - interval '10 days', 0.00, ''),
    (pg_temp.seed_uuid('dashboard-rider-sam'), 'Sam', 'Patel', 'sam.dashboard@directride.test', '555-0103', 0, now() - interval '8 days', 0.00, ''),
    (pg_temp.seed_uuid('dashboard-rider-olivia'), 'Olivia', 'Reed', 'olivia.dashboard@directride.test', '555-0104', 0, now() - interval '6 days', 0.00, ''),
    (pg_temp.seed_uuid('dashboard-rider-taylor'), 'Taylor', 'Morgan', 'taylor.dashboard@directride.test', '555-0105', 0, now() - interval '4 days', 0.00, '')
ON CONFLICT ("Id") DO UPDATE
SET
    "FirstName" = EXCLUDED."FirstName",
    "LastName" = EXCLUDED."LastName",
    "Email" = EXCLUDED."Email",
    "PhoneNumber" = EXCLUDED."PhoneNumber",
    "Role" = EXCLUDED."Role";

WITH slot_templates AS (
    SELECT *
    FROM (VALUES
        ('pending', now() + interval '90 minutes', now() + interval '2 hours 30 minutes', true),
        ('accepted', now() + interval '4 hours', now() + interval '5 hours', true),
        ('in_progress', now() - interval '20 minutes', now() + interval '40 minutes', true),
        ('completed_today', now() - interval '3 hours', now() - interval '2 hours', true),
        ('completed_yesterday', now() - interval '1 day 4 hours', now() - interval '1 day 3 hours', true),
        ('completed_week', now() - interval '5 days 2 hours', now() - interval '5 days 1 hour', true),
        ('declined', now() - interval '1 day 7 hours', now() - interval '1 day 6 hours', false),
        ('cancelled', now() + interval '1 day 18 hours', now() + interval '1 day 19 hours', false),
        ('open_morning', now() + interval '1 day 9 hours', now() + interval '1 day 12 hours', false),
        ('open_afternoon', now() + interval '2 days 13 hours', now() + interval '2 days 17 hours', false),
        ('open_weekend', now() + interval '4 days 10 hours', now() + interval '4 days 14 hours', false)
    ) AS template(label, start_time, end_time, is_booked)
)
INSERT INTO "AvailabilitySlots" ("Id", "DriverId", "StartTime", "EndTime", "IsBooked", "CreatedAt")
SELECT
    pg_temp.seed_uuid('dashboard-slot-' || c.driver_id || '-' || s.label),
    c.driver_id,
    s.start_time,
    s.end_time,
    s.is_booked,
    now() - interval '14 days'
FROM seed_driver_dashboard_context c
CROSS JOIN slot_templates s
ON CONFLICT ("Id") DO UPDATE
SET
    "StartTime" = EXCLUDED."StartTime",
    "EndTime" = EXCLUDED."EndTime",
    "IsBooked" = EXCLUDED."IsBooked";

WITH riders AS (
    SELECT *
    FROM (VALUES
        (1, pg_temp.seed_uuid('dashboard-rider-maya')),
        (2, pg_temp.seed_uuid('dashboard-rider-jordan')),
        (3, pg_temp.seed_uuid('dashboard-rider-sam')),
        (4, pg_temp.seed_uuid('dashboard-rider-olivia')),
        (5, pg_temp.seed_uuid('dashboard-rider-taylor'))
    ) AS rider(ordinal, rider_id)
),
ride_templates AS (
    SELECT *
    FROM (VALUES
        ('pending', 1, 0, '125 Peachtree St NE', 'Piedmont Park', 0.00, NULL::interval, NULL::interval, now() - interval '15 minutes', NULL::text),
        ('accepted', 2, 1, 'Georgia Tech Student Center', 'Hartsfield-Jackson Airport', 8.00, NULL::interval, NULL::interval, now() - interval '1 hour', NULL::text),
        ('in_progress', 3, 5, 'Emory University Hospital', 'Midtown MARTA Station', 5.00, interval '18 minutes', NULL::interval, now() - interval '45 minutes', NULL::text),
        ('completed_today', 4, 3, 'Buckhead Village', 'Mercedes-Benz Stadium', 12.00, interval '3 hours', interval '2 hours 8 minutes', now() - interval '4 hours', NULL::text),
        ('completed_yesterday', 5, 3, 'Ponce City Market', 'Grant Park', 10.00, interval '1 day 4 hours', interval '1 day 3 hours 5 minutes', now() - interval '1 day 6 hours', NULL::text),
        ('completed_week', 1, 3, 'Atlantic Station', 'Decatur Square', 6.00, interval '5 days 2 hours', interval '5 days 1 hour 12 minutes', now() - interval '5 days 4 hours', NULL::text),
        ('declined', 2, 2, 'Inman Park MARTA Station', 'Little Five Points', 0.00, NULL::interval, NULL::interval, now() - interval '1 day 8 hours', NULL::text),
        ('cancelled', 3, 4, 'Krog Street Market', 'Old Fourth Ward', 0.00, NULL::interval, NULL::interval, now() - interval '2 hours', 'Rider changed pickup time')
    ) AS template(label, rider_ordinal, status, pickup_location, dropoff_location, fare_bump, started_offset, completed_offset, created_at, cancellation_reason)
),
rides AS (
    SELECT
        pg_temp.seed_uuid('dashboard-ride-' || c.driver_id || '-' || rt.label) AS id,
        r.rider_id,
        c.driver_id,
        pg_temp.seed_uuid('dashboard-slot-' || c.driver_id || '-' || rt.label) AS availability_slot_id,
        rt.pickup_location,
        rt.dropoff_location,
        c.base_fare + rt.fare_bump AS fare_amount,
        c.base_fare + rt.fare_bump AS driver_earnings_amount,
        rt.status,
        rt.created_at,
        a."StartTime" AS scheduled_at,
        CASE WHEN rt.started_offset IS NULL THEN NULL ELSE now() - rt.started_offset END AS started_at,
        CASE WHEN rt.completed_offset IS NULL THEN NULL ELSE now() - rt.completed_offset END AS completed_at,
        CASE WHEN rt.status = 4 THEN now() - interval '90 minutes' ELSE NULL END AS cancelled_at,
        CASE WHEN rt.status = 4 THEN r.rider_id ELSE NULL END AS cancelled_by_user_id,
        rt.cancellation_reason
    FROM seed_driver_dashboard_context c
    JOIN ride_templates rt ON true
    JOIN riders r ON r.ordinal = rt.rider_ordinal
    JOIN "AvailabilitySlots" a ON a."Id" = pg_temp.seed_uuid('dashboard-slot-' || c.driver_id || '-' || rt.label)
)
INSERT INTO "RideRequests" (
    "Id",
    "RiderId",
    "DriverId",
    "AvailabilitySlotId",
    "PickupLocation",
    "DropoffLocation",
    "FareAmount",
    "DriverEarningsAmount",
    "Status",
    "CreatedAt",
    "ScheduledAt",
    "StartedAt",
    "CompletedAt",
    "CancelledAt",
    "CancelledByUserId",
    "CancellationReason"
)
SELECT
    id,
    rider_id,
    driver_id,
    availability_slot_id,
    pickup_location,
    dropoff_location,
    fare_amount,
    driver_earnings_amount,
    status,
    created_at,
    scheduled_at,
    started_at,
    completed_at,
    cancelled_at,
    cancelled_by_user_id,
    cancellation_reason
FROM rides
ON CONFLICT ("Id") DO UPDATE
SET
    "RiderId" = EXCLUDED."RiderId",
    "DriverId" = EXCLUDED."DriverId",
    "AvailabilitySlotId" = EXCLUDED."AvailabilitySlotId",
    "PickupLocation" = EXCLUDED."PickupLocation",
    "DropoffLocation" = EXCLUDED."DropoffLocation",
    "FareAmount" = EXCLUDED."FareAmount",
    "DriverEarningsAmount" = EXCLUDED."DriverEarningsAmount",
    "Status" = EXCLUDED."Status",
    "CreatedAt" = EXCLUDED."CreatedAt",
    "ScheduledAt" = EXCLUDED."ScheduledAt",
    "StartedAt" = EXCLUDED."StartedAt",
    "CompletedAt" = EXCLUDED."CompletedAt",
    "CancelledAt" = EXCLUDED."CancelledAt",
    "CancelledByUserId" = EXCLUDED."CancelledByUserId",
    "CancellationReason" = EXCLUDED."CancellationReason";

WITH notification_templates AS (
    SELECT *
    FROM (VALUES
        ('pending', 'RideRequested', 'New ride request', 'Maya Bennett requested a pickup from 125 Peachtree St NE.', false, now() - interval '12 minutes'),
        ('accepted', 'RideAccepted', 'Upcoming ride confirmed', 'Jordan Lee is confirmed for the airport ride this afternoon.', false, now() - interval '50 minutes'),
        ('in_progress', 'RideStarted', 'Ride in progress', 'Sam Patel is currently on the way to Midtown MARTA Station.', false, now() - interval '18 minutes'),
        ('completed_today', 'RideCompleted', 'Ride completed', 'Olivia Reed''s ride was completed and added to today''s earnings.', true, now() - interval '2 hours'),
        ('cancelled', 'RideCancelled', 'Ride cancelled', 'Sam Patel cancelled tomorrow''s ride: Rider changed pickup time.', true, now() - interval '85 minutes')
    ) AS template(label, notification_type, title, message, is_read, created_at)
),
notifications AS (
    SELECT
        pg_temp.seed_uuid('dashboard-notification-' || c.driver_id || '-' || nt.label) AS id,
        c.driver_id,
        nt.notification_type,
        nt.title,
        nt.message,
        pg_temp.seed_uuid('dashboard-ride-' || c.driver_id || '-' || nt.label) AS ride_id,
        nt.is_read,
        nt.created_at,
        CASE WHEN nt.is_read THEN nt.created_at + interval '20 minutes' ELSE NULL END AS read_at
    FROM seed_driver_dashboard_context c
    CROSS JOIN notification_templates nt
)
INSERT INTO "Notifications" ("Id", "UserId", "NotificationType", "Title", "Message", "RideId", "IsRead", "CreatedAt", "ReadAt")
SELECT id, driver_id, notification_type, title, message, ride_id, is_read, created_at, read_at
FROM notifications
ON CONFLICT ("Id") DO UPDATE
SET
    "NotificationType" = EXCLUDED."NotificationType",
    "Title" = EXCLUDED."Title",
    "Message" = EXCLUDED."Message",
    "RideId" = EXCLUDED."RideId",
    "IsRead" = EXCLUDED."IsRead",
    "CreatedAt" = EXCLUDED."CreatedAt",
    "ReadAt" = EXCLUDED."ReadAt";

COMMIT;
