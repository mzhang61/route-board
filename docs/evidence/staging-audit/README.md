# Staging audit history

The [deployed commit](release.sha) served synthetic trip 4. We expected its creation at version 1 with zero delay and a subsequent update to version 2 with four minutes of delay. We expected the application database role to permit insertion and reject edits. The [query output](query.txt) shows both transitions, the actor fingerprint and UTC timestamps. PostgreSQL returned SQLSTATE 42501 for both attempted mutations.

Run from the repository root with administrative SSH access to the staging host configured as `routeboard-staging`:

```sh
ssh routeboard-staging 'sudo docker exec -i routeboard-staging-db-1 psql -X -v ON_ERROR_STOP=1 -v trip=4 -U routeboard_runtime -d routeboard' < docs/evidence/staging-audit/verify.sql
```

The zero-row predicates let PostgreSQL check permissions without putting historical records at risk. The verification script fails if either statement succeeds. The role also lacks TRUNCATE privilege. The API takes the issuer and subject from validated claims when it writes trip history. The database query shows the saved actor as a fingerprint. The separate TripApiTests assertion checks the issuer and subject against the signed test token. A database administrator retains administrative control.

AI prepared the SQL and collected the output. No token, password, email or raw provider subject is included.
