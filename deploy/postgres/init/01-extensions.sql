-- Runs once, when the pgdata volume is created.
-- Tables come from EF Core migrations; the extensions they need are enabled here
-- so that a fresh database is ready for the first migration.
CREATE EXTENSION IF NOT EXISTS vector;      -- pgvector: talent-pool matching (AD-13)
CREATE EXTENSION IF NOT EXISTS btree_gist;  -- exclusion constraints (schema.sql)
