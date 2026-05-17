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

INSERT INTO "Users" ("Id", "FirstName", "LastName", "Email", "PhoneNumber", "Role", "CreatedAt", "BaseFare", "PasswordHash")
VALUES
    (pg_temp.seed_uuid('seed-rider-maya'), 'Maya', 'Bennett', 'maya.seed@directride.test', '555-0101', 0, now() - interval '12 days', 0.00, ''),
    (pg_temp.seed_uuid('seed-rider-jordan'), 'Jordan', 'Lee', 'jordan.seed@directride.test', '555-0102', 0, now() - interval '10 days', 0.00, ''),
    (pg_temp.seed_uuid('seed-rider-sam'), 'Sam', 'Patel', 'sam.seed@directride.test', '555-0103', 0, now() - interval '8 days', 0.00, ''),
    (pg_temp.seed_uuid('seed-rider-olivia'), 'Olivia', 'Reed', 'olivia.seed@directride.test', '555-0104', 0, now() - interval '6 days', 0.00, '')
ON CONFLICT ("Id") DO UPDATE
SET
    "FirstName" = EXCLUDED."FirstName",
    "LastName" = EXCLUDED."LastName",
    "Email" = EXCLUDED."Email",
    "PhoneNumber" = EXCLUDED."PhoneNumber",
    "Role" = EXCLUDED."Role";

UPDATE "Users"
SET "BaseFare" = 28.00
WHERE "Role" = 1
  AND "BaseFare" = 0.00;

WITH drivers AS (
    SELECT "Id", "BaseFare"
    FROM "Users"
    WHERE "Role" = 1
),
slot_templates AS (
    SELECT *
    FROM (VALUES
        ('open_morning', now() + interval '1 day 9 hours', now() + interval '1 day 12 hours', false),
        ('open_afternoon', now() + interval '2 days 13 hours', now() + interval '2 days 17 hours', false),
        ('pending', now() + interval '90 minutes', now() + interval '2 hours 15 minutes', true),
        ('accepted', now() + interval '4 hours', now() + interval '5 hours', true),
        ('completed_recent', now() - interval '2 days', now() - interval '2 days' + interval '50 minutes', true),
        ('completed_week', now() - interval '6 days', now() - interval '6 days' + interval '45 minutes', true),
        ('declined', now() - interval '1 day', now() - interval '1 day' + interval '40 minutes', false),
        ('cancelled', now() + interval '1 day 18 hours', now() + interval '1 day 19 hours', false)
    ) AS template("Label", "StartTime", "EndTime", "IsBooked")
)
INSERT INTO "AvailabilitySlots" ("Id", "DriverId", "StartTime", "EndTime", "IsBooked", "CreatedAt")
SELECT
    pg_temp.seed_uuid('seed-slot-' || drivers."Id" || '-' || slot_templates."Label"),
    drivers."Id",
    slot_templates."StartTime",
    slot_templates."EndTime",
    slot_templates."IsBooked",
    now() - interval '14 days'
FROM drivers
CROSS JOIN slot_templates
ON CONFLICT ("Id") DO UPDATE
SET
    "StartTime" = EXCLUDED."StartTime",
    "EndTime" = EXCLUDED."EndTime",
    "IsBooked" = EXCLUDED."IsBooked";

WITH drivers AS (
    SELECT "Id", "BaseFare"
    FROM "Users"
    WHERE "Role" = 1
),
riders AS (
    SELECT *
    FROM (VALUES
        (1, pg_temp.seed_uuid('seed-rider-maya')),
        (2, pg_temp.seed_uuid('seed-rider-jordan')),
        (3, pg_temp.seed_uuid('seed-rider-sam')),
        (4, pg_temp.seed_uuid('seed-rider-olivia'))
    ) AS rider("Ordinal", "RiderId")
),
ride_templates AS (
    SELECT *
    FROM (VALUES
        ('pending', 1, 0, '125 Peachtree St NE', 'Piedmont Park', 0.00, NULL::timestamp with time zone, now() - interval '15 minutes'),
        ('accepted', 2, 1, 'Georgia Tech Student Center', 'Hartsfield-Jackson Airport', 0.00, NULL::timestamp with time zone, now() - interval '1 hour'),
        ('completed_recent', 3, 3, 'Emory University Hospital', 'Midtown MARTA Station', 0.00, now() - interval '2 days' + interval '50 minutes', now() - interval '2 days 2 hours'),
        ('completed_week', 4, 3, 'Buckhead Village', 'Mercedes-Benz Stadium', 12.00, now() - interval '6 days' + interval '45 minutes', now() - interval '6 days 2 hours'),
        ('declined', 1, 2, 'Atlantic Station', 'Decatur Square', 0.00, NULL::timestamp with time zone, now() - interval '1 day 3 hours'),
        ('cancelled', 2, 4, 'Ponce City Market', 'Grant Park', 0.00, NULL::timestamp with time zone, now() - interval '2 hours')
    ) AS template("Label", "RiderOrdinal", "Status", "PickupLocation", "DropoffLocation", "FareBump", "CompletedAt", "CreatedAt")
),
rides AS (
    SELECT
        pg_temp.seed_uuid('seed-ride-' || drivers."Id" || '-' || ride_templates."Label") AS "Id",
        riders."RiderId",
        drivers."Id" AS "DriverId",
        pg_temp.seed_uuid('seed-slot-' || drivers."Id" || '-' || ride_templates."Label") AS "AvailabilitySlotId",
        ride_templates."PickupLocation",
        ride_templates."DropoffLocation",
        drivers."BaseFare" + ride_templates."FareBump" AS "FareAmount",
        drivers."BaseFare" + ride_templates."FareBump" AS "DriverEarningsAmount",
        ride_templates."Status",
        ride_templates."CreatedAt",
        ride_templates."CompletedAt"
    FROM drivers
    JOIN ride_templates ON true
    JOIN riders ON riders."Ordinal" = ride_templates."RiderOrdinal"
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
    "CompletedAt"
)
SELECT
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
    "CompletedAt"
FROM rides
ON CONFLICT ("Id") DO UPDATE
SET
    "PickupLocation" = EXCLUDED."PickupLocation",
    "DropoffLocation" = EXCLUDED."DropoffLocation",
    "FareAmount" = EXCLUDED."FareAmount",
    "DriverEarningsAmount" = EXCLUDED."DriverEarningsAmount",
    "Status" = EXCLUDED."Status",
    "CreatedAt" = EXCLUDED."CreatedAt",
    "CompletedAt" = EXCLUDED."CompletedAt";

COMMIT;
