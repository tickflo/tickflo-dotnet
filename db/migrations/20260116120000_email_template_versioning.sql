-- migrate:up

-- Drop the old unique constraint on workspace_id and template_type_id
ALTER TABLE public.email_templates
    DROP CONSTRAINT IF EXISTS email_templates_workspace_id_template_type_id_unique;

-- Add version column (default to 1 for existing templates)
ALTER TABLE public.email_templates
    ADD COLUMN version integer NOT NULL DEFAULT 1;

-- Drop workspace_id column (templates are now global with versioning)
ALTER TABLE public.email_templates
    DROP COLUMN workspace_id;

-- Create a unique constraint on template_type_id and version
-- This ensures each template type can have multiple versions, but each version is unique
ALTER TABLE public.email_templates
    ADD CONSTRAINT email_templates_type_version_unique UNIQUE (template_type_id, version);

-- migrate:down

-- Re-add workspace_id column
ALTER TABLE public.email_templates
    ADD COLUMN workspace_id integer;

-- Drop the version-based unique constraint
ALTER TABLE public.email_templates
    DROP CONSTRAINT IF EXISTS email_templates_type_version_unique;

-- Drop version column
ALTER TABLE public.email_templates
    DROP COLUMN version;

-- Restore the old unique constraint
ALTER TABLE public.email_templates
    ADD CONSTRAINT email_templates_workspace_id_template_type_id_unique 
    UNIQUE NULLS NOT DISTINCT (workspace_id, template_type_id);
