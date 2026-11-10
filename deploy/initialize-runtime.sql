-- Read a mounted secret inside psql; never put the password in process arguments.
\set runtime_password `cat /run/secrets/runtime_password`
CREATE ROLE routeboard_runtime LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION
    PASSWORD :'runtime_password';
\unset runtime_password
