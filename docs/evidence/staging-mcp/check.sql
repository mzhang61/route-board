SELECT "Id", "Action", "After"::jsonb->>'authorized' AS scope_and_role_allowed
FROM audit_events
WHERE "Id" >= (SELECT min("Id") FROM audit_events WHERE "TripId" = :trip)
  AND "OccurredAt" < (SELECT max("OccurredAt") + interval '2 seconds'
                      FROM trip_events WHERE "TripId" = :trip)
ORDER BY "Id";

SELECT "TripId", "Kind", "Snapshot"::jsonb->>'version' AS version,
       "Snapshot"::jsonb->>'delayMinutes' AS total_delay
FROM trip_events WHERE "TripId" IN (:baseline, :trip) ORDER BY "Id";
