SELECT "Id", "Kind", "Snapshot"::jsonb->>'version' AS version,
       "Snapshot"::jsonb->>'delayMinutes' AS total_delay,
       "Snapshot"::jsonb->'stops'->-1->>'estimated' AS final_eta
FROM trip_events WHERE "TripId" = :trip ORDER BY "Id";
