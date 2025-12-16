-- Create locations table
CREATE TABLE IF NOT EXISTS public.locations (
    id integer NOT NULL,
    workspace_id integer NOT NULL,
    name character varying(100) NOT NULL,
    address text,
    active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by integer,
    updated_at timestamp with time zone,
    updated_by integer
);

-- Identity sequence for id
DO $$ BEGIN
    ALTER TABLE public.locations ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Primary key
ALTER TABLE ONLY public.locations
    ADD CONSTRAINT locations_pkey PRIMARY KEY (id);

-- Unique name per workspace
ALTER TABLE ONLY public.locations
    ADD CONSTRAINT locations_workspace_id_name_unique UNIQUE (workspace_id, name);

-- Foreign key to workspaces
ALTER TABLE ONLY public.locations
    ADD CONSTRAINT locations_workspace_id_workspaces_id_fk FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id);
