-- Reference data for the fictional shuttle. Dispatchers create operational trips through the API.
BEGIN;
INSERT INTO routes ("Code", "Name", "IsActive") VALUES ('RB-DEMO', 'Synthetic River Loop', true)
ON CONFLICT ("Code") DO NOTHING;
INSERT INTO vehicles ("Code", "Capacity", "IsActive") VALUES ('RB-SYN-01', 20, true)
ON CONFLICT ("Code") DO NOTHING;
INSERT INTO stops ("Code", "Name", "Location") VALUES
    ('RB-DEMO-A', 'Demo North', ST_SetSRID(ST_MakePoint(-87.63, 41.88), 4326)::geography),
    ('RB-DEMO-B', 'Demo South', ST_SetSRID(ST_MakePoint(-87.62, 41.87), 4326)::geography)
ON CONFLICT ("Code") DO NOTHING;
INSERT INTO route_stops ("RouteId", "StopId", "StopOrder", "DwellMinutes", "TravelMinutesFromPreviousStop")
SELECT r."Id", s."Id", leg.stop_order, leg.dwell, leg.travel
FROM (VALUES ('RB-DEMO-A', 1, 3, 8), ('RB-DEMO-B', 2, 0, 12)) AS leg(code, stop_order, dwell, travel)
JOIN stops s ON s."Code" = leg.code
JOIN routes r ON r."Code" = 'RB-DEMO'
ON CONFLICT ("RouteId", "StopOrder") DO NOTHING;
COMMIT;
