-- migrate:up
-- Add role column to user_workspaces for EF mapping and server-side gating
BEGIN;

-- 1) Add column without constraints
ALTER TABLE
    public.user_workspaces
ADD
    COLUMN role character varying(30);

-- 2) Backfill existing rows to default role
UPDATE
    public.user_workspaces
SET
    role = 'Member'
WHERE
    role IS NULL;

-- 3) Set default and NOT NULL constraint
ALTER TABLE
    public.user_workspaces
ALTER COLUMN
    role
SET
    DEFAULT 'Member';

ALTER TABLE
    public.user_workspaces
ALTER COLUMN
    role
SET
    NOT NULL;

COMMIT;

-- Record migration version (dbmate-compatible)
INSERT INTO
    public.schema_migrations (version)
VALUES
    ('20251215104500');

-- migrate:down