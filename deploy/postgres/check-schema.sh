#!/usr/bin/env sh
# Applies docs/database/schema.sql to a throwaway database in the running
# postgres container, prints the table count, and drops the database.
# The development database "ats" is not touched.
set -eu
cd "$(dirname "$0")/../.."

db=ats_schema_check
user="${POSTGRES_USER:-ats}"
psql() { docker compose exec -T -e PGOPTIONS=--client-min-messages=warning postgres psql -v ON_ERROR_STOP=1 -U "$user" "$@"; }

psql -d postgres -qc "DROP DATABASE IF EXISTS $db" -c "CREATE DATABASE $db"
trap 'psql -d postgres -qc "DROP DATABASE IF EXISTS $db"' EXIT
psql -d "$db" -q -f - < docs/database/schema.sql
psql -d "$db" -tAc "SELECT count(*) || ' tables created' FROM pg_tables WHERE schemaname = 'public'"
