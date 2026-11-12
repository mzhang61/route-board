SELECT current_user, current_database(),
       has_table_privilege(current_user, 'audit_events', 'INSERT') AS can_append,
       has_table_privilege(current_user, 'audit_events', 'UPDATE') AS can_update,
       has_table_privilege(current_user, 'audit_events', 'DELETE') AS can_delete,
       has_table_privilege(current_user, 'audit_events', 'TRUNCATE') AS can_truncate;

-- Output a stable fingerprint of the actor, keeping provider identifiers private.
SELECT "Id", "TripId", "OccurredAt", "Action",
       left(encode(sha256(convert_to("Actor", 'UTF8')), 'hex'), 16) AS actor_fingerprint,
       "Before"::jsonb->>'version' AS previous_version,
       "After"::jsonb->>'version' AS new_version,
       "Before"::jsonb->>'delayMinutes' AS previous_delay,
       "After"::jsonb->>'delayMinutes' AS new_delay
FROM audit_events WHERE "TripId" = :trip ORDER BY "Id";

-- PostgreSQL checks table permissions even when a predicate matches zero rows.
DO $$
BEGIN
    BEGIN
        UPDATE audit_events SET "Action" = "Action" WHERE false;
        RAISE EXCEPTION 'Runtime UPDATE unexpectedly allowed';
    EXCEPTION WHEN insufficient_privilege THEN
        RAISE NOTICE 'UPDATE rejected: % %', SQLSTATE, SQLERRM;
    END;
    BEGIN
        DELETE FROM audit_events WHERE false;
        RAISE EXCEPTION 'Runtime DELETE unexpectedly allowed';
    EXCEPTION WHEN insufficient_privilege THEN
        RAISE NOTICE 'DELETE rejected: % %', SQLSTATE, SQLERRM;
    END;
END $$;
