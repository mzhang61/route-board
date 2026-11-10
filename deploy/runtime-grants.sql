-- Run as the migration owner after every schema migration.
-- The runtime login must already exist and must not own application objects.
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA public FROM routeboard_runtime;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM routeboard_runtime;
GRANT USAGE ON SCHEMA public TO routeboard_runtime;
GRANT SELECT ON routes, stops, route_stops, vehicles,
    trips, trip_events, audit_events, vehicle_positions TO routeboard_runtime;
GRANT INSERT, UPDATE ON trips, vehicle_positions TO routeboard_runtime;
GRANT INSERT ON trip_events, audit_events TO routeboard_runtime;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO routeboard_runtime;
