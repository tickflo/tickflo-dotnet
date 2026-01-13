-- migrate:up
SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

CREATE TABLE IF NOT EXISTS public.notifications (
    id integer NOT NULL,
    workspace_id integer,
    user_id integer NOT NULL,
    type character varying(50) NOT NULL,
    delivery_method character varying(20) DEFAULT 'email'::character varying NOT NULL,
    priority character varying(20) DEFAULT 'normal'::character varying NOT NULL,
    subject text NOT NULL,
    body text NOT NULL,
    data text,
    status character varying(20) DEFAULT 'pending'::character varying NOT NULL,
    sent_at timestamp with time zone,
    failed_at timestamp with time zone,
    failure_reason text,
    read_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by integer,
    scheduled_for timestamp with time zone,
    batch_id character varying(100)
);


--
-- Name: notifications_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE public.notifications ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
        SEQUENCE NAME public.notifications_id_seq
        START WITH 1
        INCREMENT BY 1
        NO MINVALUE
        NO MAXVALUE
        CACHE 1
    );
EXCEPTION WHEN duplicate_table OR duplicate_object THEN NULL; END $$;


CREATE TABLE IF NOT EXISTS public.meta (
    key text NOT NULL,
    value text
);


CREATE TABLE IF NOT EXISTS public.permissions (
    id integer NOT NULL,
    resource text NOT NULL,
    action text NOT NULL
);


--
-- Name: permissions_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE public.permissions ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
        SEQUENCE NAME public.permissions_id_seq
        START WITH 1
        INCREMENT BY 1
        NO MINVALUE
        NO MAXVALUE
        CACHE 1
    );
EXCEPTION WHEN duplicate_table OR duplicate_object THEN NULL; END $$;


CREATE TABLE IF NOT EXISTS public.role_permissions (
    role_id integer NOT NULL,
    permission_id integer NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by integer,
    updated_at timestamp with time zone,
    updated_by integer
);


CREATE TABLE IF NOT EXISTS public.roles (
    id integer NOT NULL,
    workspace_id integer NOT NULL,
    name character varying(30) NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by integer NOT NULL,
    updated_at timestamp with time zone,
    updated_by integer,
    admin boolean DEFAULT false NOT NULL
);


--
-- Name: roles_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE public.roles ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
        SEQUENCE NAME public.roles_id_seq
        START WITH 1
        INCREMENT BY 1
        NO MINVALUE
        NO MAXVALUE
        CACHE 1
    );
EXCEPTION WHEN duplicate_table OR duplicate_object THEN NULL; END $$;


CREATE TABLE IF NOT EXISTS public.tokens (
    user_id integer NOT NULL,
    token character varying(64) NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    max_age integer NOT NULL
);


CREATE TABLE IF NOT EXISTS public.user_email_changes (
    user_id integer NOT NULL,
    old character varying(254) NOT NULL,
    new character varying(254) NOT NULL,
    confirm_token character varying(100) NOT NULL,
    undo_token character varying(100) NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by integer NOT NULL,
    confirmed_at timestamp with time zone,
    confirm_max_age integer NOT NULL,
    undo_max_age integer NOT NULL,
    undone_at timestamp with time zone
);


CREATE TABLE IF NOT EXISTS public.user_workspace_roles (
    user_id integer NOT NULL,
    workspace_id integer NOT NULL,
    role_id integer NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by integer NOT NULL
);


CREATE TABLE IF NOT EXISTS public.user_workspaces (
    user_id integer NOT NULL,
    workspace_id integer NOT NULL,
    accepted boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by integer NOT NULL,
    updated_at timestamp with time zone,
    updated_by integer
);


CREATE TABLE IF NOT EXISTS public.users (
    id integer NOT NULL,
    name character varying(100) NOT NULL,
    email character varying(254) NOT NULL,
    email_confirmed boolean DEFAULT false NOT NULL,
    email_confirmation_code character varying(100),
    password_hash character varying(100),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by integer,
    updated_at timestamp with time zone,
    updated_by integer,
    system_admin boolean DEFAULT false NOT NULL,
    "recoveryEmail" character varying(254)
);


--
-- Name: users_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE public.users ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
        SEQUENCE NAME public.users_id_seq
        START WITH 1
        INCREMENT BY 1
        NO MINVALUE
        NO MAXVALUE
        CACHE 1
    );
EXCEPTION WHEN duplicate_table OR duplicate_object THEN NULL; END $$;


CREATE TABLE IF NOT EXISTS public.user_notification_preferences (
    user_id integer NOT NULL,
    notification_type character varying(50) NOT NULL,
    email_enabled boolean DEFAULT true NOT NULL,
    in_app_enabled boolean DEFAULT true NOT NULL,
    sms_enabled boolean DEFAULT false NOT NULL,
    push_enabled boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone,
    PRIMARY KEY (user_id, notification_type)
);


CREATE TABLE IF NOT EXISTS public.workspaces (
    id integer NOT NULL,
    name character varying(100) NOT NULL,
    slug character varying(30) NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by integer NOT NULL,
    updated_at timestamp with time zone,
    updated_by integer
);


--
-- Name: workspaces_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE public.workspaces ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
        SEQUENCE NAME public.workspaces_id_seq
        START WITH 1
        INCREMENT BY 1
        NO MINVALUE
        NO MAXVALUE
        CACHE 1
    );
EXCEPTION WHEN duplicate_table OR duplicate_object THEN NULL; END $$;

--
-- Name: notifications notifications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p'
          AND rel.relname = 'notifications'
          AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.notifications
            ADD CONSTRAINT notifications_pkey PRIMARY KEY (id);
    END IF;
END $$;


--
-- Name: meta meta_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p'
          AND rel.relname = 'meta'
          AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.meta
            ADD CONSTRAINT meta_pkey PRIMARY KEY (key);
    END IF;
END $$;


--
-- Name: permissions permissions_action_resource_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'permissions_action_resource_unique'
    ) THEN
        ALTER TABLE ONLY public.permissions
            ADD CONSTRAINT permissions_action_resource_unique UNIQUE (action, resource);
    END IF;
END $$;


--
-- Name: permissions permissions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p'
          AND rel.relname = 'permissions'
          AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.permissions
            ADD CONSTRAINT permissions_pkey PRIMARY KEY (id);
    END IF;
END $$;


--
-- Name: role_permissions role_permissions_role_id_permission_id_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p'
          AND rel.relname = 'role_permissions'
          AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.role_permissions
            ADD CONSTRAINT role_permissions_role_id_permission_id_pk PRIMARY KEY (role_id, permission_id);
    END IF;
END $$;


--
-- Name: roles roles_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p'
          AND rel.relname = 'roles'
          AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.roles
            ADD CONSTRAINT roles_pkey PRIMARY KEY (id);
    END IF;
END $$;

--
-- Name: user_workspace_roles user_workspace_roles_user_id_workspace_id_role_id_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p'
          AND rel.relname = 'user_workspace_roles'
          AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.user_workspace_roles
            ADD CONSTRAINT user_workspace_roles_user_id_workspace_id_role_id_pk PRIMARY KEY (user_id, workspace_id, role_id);
    END IF;
END $$;


--
-- Name: user_workspaces user_workspaces_user_id_workspace_id_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p'
          AND rel.relname = 'user_workspaces'
          AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.user_workspaces
            ADD CONSTRAINT user_workspaces_user_id_workspace_id_pk PRIMARY KEY (user_id, workspace_id);
    END IF;
END $$;


--
-- Name: users users_email_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'users_email_unique'
    ) THEN
        ALTER TABLE ONLY public.users
            ADD CONSTRAINT users_email_unique UNIQUE (email);
    END IF;
END $$;


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p'
          AND rel.relname = 'users'
          AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.users
            ADD CONSTRAINT users_pkey PRIMARY KEY (id);
    END IF;
END $$;


--
-- Name: workspaces workspaces_name_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'workspaces_name_unique'
    ) THEN
        ALTER TABLE ONLY public.workspaces
            ADD CONSTRAINT workspaces_name_unique UNIQUE (name);
    END IF;
END $$;


--
-- Name: workspaces workspaces_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p'
          AND rel.relname = 'workspaces'
          AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.workspaces
            ADD CONSTRAINT workspaces_pkey PRIMARY KEY (id);
    END IF;
END $$;


--
-- Name: workspaces workspaces_slug_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'workspaces_slug_unique'
    ) THEN
        ALTER TABLE ONLY public.workspaces
            ADD CONSTRAINT workspaces_slug_unique UNIQUE (slug);
    END IF;
END $$;


--
-- Name: email_templates email_templates_created_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.email_templates
        ADD CONSTRAINT email_templates_created_by_users_id_fk FOREIGN KEY (created_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: email_templates email_templates_updated_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.email_templates
        ADD CONSTRAINT email_templates_updated_by_users_id_fk FOREIGN KEY (updated_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: email_templates email_templates_workspace_id_workspaces_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.email_templates
        ADD CONSTRAINT email_templates_workspace_id_workspaces_id_fk FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: emails emails_created_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.emails
        ADD CONSTRAINT emails_created_by_users_id_fk FOREIGN KEY (created_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: emails emails_template_id_email_templates_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.emails
        ADD CONSTRAINT emails_template_id_email_templates_id_fk FOREIGN KEY (template_id) REFERENCES public.email_templates(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: emails emails_updated_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.emails
        ADD CONSTRAINT emails_updated_by_users_id_fk FOREIGN KEY (updated_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: role_permissions role_permissions_created_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.role_permissions
        ADD CONSTRAINT role_permissions_created_by_users_id_fk FOREIGN KEY (created_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: role_permissions role_permissions_permission_id_permissions_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.role_permissions
        ADD CONSTRAINT role_permissions_permission_id_permissions_id_fk FOREIGN KEY (permission_id) REFERENCES public.permissions(id) ON DELETE CASCADE;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: role_permissions role_permissions_role_id_roles_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.role_permissions
        ADD CONSTRAINT role_permissions_role_id_roles_id_fk FOREIGN KEY (role_id) REFERENCES public.roles(id) ON DELETE CASCADE;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: role_permissions role_permissions_updated_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.role_permissions
        ADD CONSTRAINT role_permissions_updated_by_users_id_fk FOREIGN KEY (updated_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: roles roles_created_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.roles
        ADD CONSTRAINT roles_created_by_users_id_fk FOREIGN KEY (created_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: roles roles_updated_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.roles
        ADD CONSTRAINT roles_updated_by_users_id_fk FOREIGN KEY (updated_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: roles roles_workspace_id_workspaces_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.roles
        ADD CONSTRAINT roles_workspace_id_workspaces_id_fk FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: tokens tokens_user_id_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.tokens
        ADD CONSTRAINT tokens_user_id_users_id_fk FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: user_email_changes user_email_changes_created_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.user_email_changes
        ADD CONSTRAINT user_email_changes_created_by_users_id_fk FOREIGN KEY (created_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: user_email_changes user_email_changes_user_id_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.user_email_changes
        ADD CONSTRAINT user_email_changes_user_id_users_id_fk FOREIGN KEY (user_id) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: user_workspace_roles user_workspace_roles_created_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.user_workspace_roles
        ADD CONSTRAINT user_workspace_roles_created_by_users_id_fk FOREIGN KEY (created_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: user_workspace_roles user_workspace_roles_role_id_roles_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.user_workspace_roles
        ADD CONSTRAINT user_workspace_roles_role_id_roles_id_fk FOREIGN KEY (role_id) REFERENCES public.roles(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: user_workspace_roles user_workspace_roles_user_id_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.user_workspace_roles
        ADD CONSTRAINT user_workspace_roles_user_id_users_id_fk FOREIGN KEY (user_id) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: user_workspace_roles user_workspace_roles_workspace_id_workspaces_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.user_workspace_roles
        ADD CONSTRAINT user_workspace_roles_workspace_id_workspaces_id_fk FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: user_notification_preferences user_notification_preferences_user_id_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.user_notification_preferences
        ADD CONSTRAINT user_notification_preferences_user_id_users_id_fk FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: user_workspaces user_workspaces_created_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.user_workspaces
        ADD CONSTRAINT user_workspaces_created_by_users_id_fk FOREIGN KEY (created_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: user_workspaces user_workspaces_updated_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.user_workspaces
        ADD CONSTRAINT user_workspaces_updated_by_users_id_fk FOREIGN KEY (updated_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: user_workspaces user_workspaces_user_id_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.user_workspaces
        ADD CONSTRAINT user_workspaces_user_id_users_id_fk FOREIGN KEY (user_id) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: user_workspaces user_workspaces_workspace_id_workspaces_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.user_workspaces
        ADD CONSTRAINT user_workspaces_workspace_id_workspaces_id_fk FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: workspaces workspaces_created_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.workspaces
        ADD CONSTRAINT workspaces_created_by_users_id_fk FOREIGN KEY (created_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Name: workspaces workspaces_updated_by_users_id_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

DO $$ BEGIN
    ALTER TABLE ONLY public.workspaces
        ADD CONSTRAINT workspaces_updated_by_users_id_fk FOREIGN KEY (updated_by) REFERENCES public.users(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- PostgreSQL database dump complete
--

--
-- Combined additions: locations and reports tables + constraints
--

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

-- Create reports table
CREATE TABLE IF NOT EXISTS public.reports (
    id integer NOT NULL,
    workspace_id integer NOT NULL,
    name character varying(100) NOT NULL,
    ready boolean DEFAULT false NOT NULL,
    last_run timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by integer,
    updated_at timestamp with time zone,
    updated_by integer
);

-- Identities for locations.id and reports.id
DO $$ BEGIN
    ALTER TABLE public.locations ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
        SEQUENCE NAME public.locations_id_seq
        START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1
    );
EXCEPTION WHEN duplicate_table OR duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    ALTER TABLE public.reports ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
        SEQUENCE NAME public.reports_id_seq
        START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1
    );
EXCEPTION WHEN duplicate_table OR duplicate_object THEN NULL; END $$;

-- Primary keys if not already present
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p' AND rel.relname = 'locations' AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.locations ADD CONSTRAINT locations_pkey PRIMARY KEY (id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p' AND rel.relname = 'reports' AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.reports ADD CONSTRAINT reports_pkey PRIMARY KEY (id);
    END IF;
END $$;

-- Unique workspace_id+name constraints
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'locations_workspace_id_name_unique') THEN
        ALTER TABLE ONLY public.locations
            ADD CONSTRAINT locations_workspace_id_name_unique UNIQUE (workspace_id, name);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'reports_workspace_id_name_unique') THEN
        ALTER TABLE ONLY public.reports
            ADD CONSTRAINT reports_workspace_id_name_unique UNIQUE (workspace_id, name);
    END IF;
END $$;

-- Foreign keys to workspaces
DO $$ BEGIN
    ALTER TABLE ONLY public.locations
        ADD CONSTRAINT locations_workspace_id_workspaces_id_fk FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Locations: default assignee per location (idempotent)
DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'locations' AND column_name = 'default_assignee_user_id'
    ) THEN
        ALTER TABLE public.locations
            ADD COLUMN default_assignee_user_id integer NULL;
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'locations_default_assignee_user_id_users_id_fk'
    ) THEN
        ALTER TABLE ONLY public.locations
            ADD CONSTRAINT locations_default_assignee_user_id_users_id_fk FOREIGN KEY (default_assignee_user_id) REFERENCES public.users(id) ON DELETE SET NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_locations_ws_default_assignee ON public.locations(workspace_id, default_assignee_user_id);

DO $$ BEGIN
    ALTER TABLE ONLY public.reports
        ADD CONSTRAINT reports_workspace_id_workspaces_id_fk FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

--
-- Add role column to user_workspaces (idempotent)
--
ALTER TABLE public.user_workspaces
    ADD COLUMN IF NOT EXISTS role character varying(30);

UPDATE public.user_workspaces
SET role = 'Member'
WHERE role IS NULL;

ALTER TABLE public.user_workspaces
    ALTER COLUMN role SET DEFAULT 'Member';

ALTER TABLE public.user_workspaces
    ALTER COLUMN role SET NOT NULL;


-- Contacts: table, identity, PK, unique, and FK (idempotent)
CREATE TABLE IF NOT EXISTS public.contacts (
    id integer NOT NULL,
    workspace_id integer NOT NULL,
    name text NOT NULL,
    email text NOT NULL,
    phone text NULL,
    company text NULL,
    title text NULL,
    notes text NULL,
    tags text NULL,
    preferred_channel text NULL,
    priority text NULL,
    status text NULL,
    assigned_user_id integer NULL,
    last_interaction timestamp NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);

DO $$ BEGIN
    ALTER TABLE public.contacts ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
        SEQUENCE NAME public.contacts_id_seq
        START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1
    );
EXCEPTION WHEN duplicate_table OR duplicate_object THEN NULL; END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p' AND rel.relname = 'contacts' AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.contacts ADD CONSTRAINT contacts_pkey PRIMARY KEY (id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'contacts_workspace_email_unique') THEN
        ALTER TABLE ONLY public.contacts
            ADD CONSTRAINT contacts_workspace_email_unique UNIQUE NULLS NOT DISTINCT (workspace_id, email);
    END IF;
END $$;

DO $$ BEGIN
    ALTER TABLE ONLY public.contacts
        ADD CONSTRAINT contacts_workspace_id_workspaces_id_fk FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Contact to Location assignments (idempotent)
CREATE TABLE IF NOT EXISTS public.contact_locations (
    contact_id integer NOT NULL,
    location_id integer NOT NULL,
    workspace_id integer NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p' AND rel.relname = 'contact_locations' AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.contact_locations
            ADD CONSTRAINT contact_locations_pk PRIMARY KEY (contact_id, location_id);
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'contact_locations_contact_id_contacts_id_fk'
    ) THEN
        ALTER TABLE ONLY public.contact_locations
            ADD CONSTRAINT contact_locations_contact_id_contacts_id_fk FOREIGN KEY (contact_id) REFERENCES public.contacts(id) ON DELETE CASCADE;
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'contact_locations_location_id_locations_id_fk'
    ) THEN
        ALTER TABLE ONLY public.contact_locations
            ADD CONSTRAINT contact_locations_location_id_locations_id_fk FOREIGN KEY (location_id) REFERENCES public.locations(id) ON DELETE CASCADE;
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'contact_locations_workspace_id_workspaces_id_fk'
    ) THEN
        ALTER TABLE ONLY public.contact_locations
            ADD CONSTRAINT contact_locations_workspace_id_workspaces_id_fk FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id) ON DELETE CASCADE;
    END IF;
END $$;

-- Helpful index for lookups by workspace/contact
CREATE INDEX IF NOT EXISTS ix_contact_locations_workspace_contact ON public.contact_locations(workspace_id, contact_id);

-- Tickets: table, identity, PK, and FKs (idempotent)
CREATE TABLE IF NOT EXISTS public.tickets (
    id integer NOT NULL,
    workspace_id integer NOT NULL,
    contact_id integer NULL,
    location_id integer NULL,
    subject text NOT NULL,
    description text NOT NULL,
    type text DEFAULT 'Standard' NOT NULL,
    priority text DEFAULT 'Normal' NOT NULL,
    status text DEFAULT 'New' NOT NULL,
    assigned_user_id integer NULL,
    inventory_ref text NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone
);

DO $$ BEGIN
    ALTER TABLE public.tickets ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
        SEQUENCE NAME public.tickets_id_seq
        START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1
    );
EXCEPTION WHEN duplicate_table OR duplicate_object THEN NULL; END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p' AND rel.relname = 'tickets' AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.tickets ADD CONSTRAINT tickets_pkey PRIMARY KEY (id);
    END IF;
END $$;

-- Ticket Type column & index (idempotent)
DO $$ BEGIN
    ALTER TABLE IF EXISTS public.tickets
        ADD COLUMN IF NOT EXISTS type text DEFAULT 'Standard' NOT NULL;
EXCEPTION WHEN duplicate_column THEN NULL; END $$;

DO $$ BEGIN
    CREATE INDEX IF NOT EXISTS idx_tickets_workspace_type
        ON public.tickets (workspace_id, type);
EXCEPTION WHEN duplicate_table OR duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    ALTER TABLE ONLY public.tickets
        ADD CONSTRAINT tickets_workspace_id_workspaces_id_fk FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Tickets: add location FK and index (idempotent)
-- Ensure tickets.location_id exists for existing schemas
DO $$ BEGIN
    ALTER TABLE IF EXISTS public.tickets
        ADD COLUMN IF NOT EXISTS location_id integer NULL;
EXCEPTION WHEN duplicate_column THEN NULL; END $$;

DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'tickets_location_id_locations_id_fk'
    ) THEN
        ALTER TABLE ONLY public.tickets
            ADD CONSTRAINT tickets_location_id_locations_id_fk FOREIGN KEY (location_id) REFERENCES public.locations(id);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_tickets_workspace_location ON public.tickets(workspace_id, location_id, id);

DO $$ BEGIN
    -- Drop existing FK if present to replace with ON DELETE SET NULL
    IF EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'tickets_contact_id_contacts_id_fk'
    ) THEN
        ALTER TABLE ONLY public.tickets DROP CONSTRAINT tickets_contact_id_contacts_id_fk;
    END IF;
    ALTER TABLE ONLY public.tickets
        ADD CONSTRAINT tickets_contact_id_contacts_id_fk FOREIGN KEY (contact_id) REFERENCES public.contacts(id) ON DELETE SET NULL;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


-- Inventory: table, identity, PK, uniques, and FKs (idempotent)
CREATE TABLE IF NOT EXISTS public.inventory (
    id integer NOT NULL,
    workspace_id integer NOT NULL,
    sku character varying(100) NOT NULL,
    name character varying(200) NOT NULL,
    description text,
    quantity integer DEFAULT 0 NOT NULL,
    location_id integer,
    min_stock integer,
    cost numeric(12,2) DEFAULT 0 NOT NULL,
    price numeric(12,2),
    category character varying(100),
    tags text,
    status character varying(30) DEFAULT 'active' NOT NULL,
    supplier character varying(200),
    last_restock_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone
);

DO $$ BEGIN
    ALTER TABLE public.inventory ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
        SEQUENCE NAME public.inventory_id_seq
        START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1
    );
EXCEPTION WHEN duplicate_table OR duplicate_object THEN NULL; END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p' AND rel.relname = 'inventory' AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.inventory ADD CONSTRAINT inventory_pkey PRIMARY KEY (id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'inventory_workspace_id_sku_unique') THEN
        ALTER TABLE ONLY public.inventory
            ADD CONSTRAINT inventory_workspace_id_sku_unique UNIQUE (workspace_id, sku);
    END IF;
END $$;

DO $$ BEGIN
    ALTER TABLE ONLY public.inventory
        ADD CONSTRAINT inventory_workspace_id_workspaces_id_fk FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Ensure inventory.location_id exists for existing schemas
DO $$ BEGIN
    ALTER TABLE IF EXISTS public.inventory
        ADD COLUMN IF NOT EXISTS location_id integer NULL;
EXCEPTION WHEN duplicate_column THEN NULL; END $$;

DO $$ BEGIN
    ALTER TABLE ONLY public.inventory
    ADD CONSTRAINT inventory_location_id_locations_id_fk FOREIGN KEY (location_id) REFERENCES public.locations(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;


--
-- Dbmate schema migrations
--

--
-- Indexes for query performance (idempotent)
--

-- Tickets: common filters and pagination
CREATE INDEX IF NOT EXISTS idx_tickets_ws_status
    ON public.tickets (workspace_id, status);

-- Teams: table and membership (idempotent)
CREATE TABLE IF NOT EXISTS public.teams (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    workspace_id integer NOT NULL REFERENCES public.workspaces(id),
    name text NOT NULL,
    description text,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by integer NOT NULL,
    updated_at timestamptz,
    updated_by integer
);
CREATE UNIQUE INDEX IF NOT EXISTS teams_workspace_id_name_idx ON public.teams(workspace_id, name);

CREATE TABLE IF NOT EXISTS public.team_members (
    team_id integer NOT NULL REFERENCES public.teams(id) ON DELETE CASCADE,
    user_id integer NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    joined_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (team_id, user_id)
);

-- Tickets: optional team assignment (idempotent)
ALTER TABLE IF EXISTS public.tickets ADD COLUMN IF NOT EXISTS assigned_team_id integer;
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'tickets_assigned_team_id_teams_id_fk'
    ) THEN
        ALTER TABLE ONLY public.tickets
            ADD CONSTRAINT tickets_assigned_team_id_teams_id_fk FOREIGN KEY (assigned_team_id) REFERENCES public.teams(id) ON DELETE SET NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_tickets_ws_priority
    ON public.tickets (workspace_id, priority);

CREATE INDEX IF NOT EXISTS idx_tickets_ws_assigned
    ON public.tickets (workspace_id, assigned_user_id);

CREATE INDEX IF NOT EXISTS idx_tickets_ws_contact
    ON public.tickets (workspace_id, contact_id);

CREATE INDEX IF NOT EXISTS idx_tickets_ws_created_at
    ON public.tickets (workspace_id, created_at DESC);

-- Inventory: status filter, name queries, and location filter
CREATE INDEX IF NOT EXISTS idx_inventory_ws_status
    ON public.inventory (workspace_id, status);

CREATE INDEX IF NOT EXISTS idx_inventory_ws_name
    ON public.inventory (workspace_id, name);

CREATE INDEX IF NOT EXISTS idx_inventory_ws_location
    ON public.inventory (workspace_id, location_id);

-- Contacts: optional name searches alongside existing (workspace_id, email) unique index
CREATE INDEX IF NOT EXISTS idx_contacts_ws_name
    ON public.contacts (workspace_id, name);


--
-- Ticket Statuses (customizable per workspace)
--
CREATE TABLE IF NOT EXISTS public.ticket_statuses (
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY,
    workspace_id integer NOT NULL,
    name character varying(50) NOT NULL,
    color character varying(20) NOT NULL DEFAULT 'neutral',
    sort_order integer NOT NULL DEFAULT 0,
    is_closed_state boolean NOT NULL DEFAULT false
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p'
          AND rel.relname = 'ticket_statuses'
          AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.ticket_statuses
            ADD CONSTRAINT ticket_statuses_pkey PRIMARY KEY (id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'ticket_statuses_workspace_name_unique'
    ) THEN
        ALTER TABLE ONLY public.ticket_statuses
            ADD CONSTRAINT ticket_statuses_workspace_name_unique UNIQUE (workspace_id, name);
    END IF;
END $$;

DO $$ BEGIN
    ALTER TABLE public.ticket_statuses
        ADD CONSTRAINT ticket_statuses_workspace_fk
        FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id) ON DELETE CASCADE;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Seed default statuses for all existing workspaces if none exist
INSERT INTO public.ticket_statuses (workspace_id, name, color, sort_order, is_closed_state)
SELECT w.id, 'New', 'info', 1, false
FROM public.workspaces w
WHERE NOT EXISTS (SELECT 1 FROM public.ticket_statuses s WHERE s.workspace_id = w.id)
ON CONFLICT DO NOTHING;

INSERT INTO public.ticket_statuses (workspace_id, name, color, sort_order, is_closed_state)
SELECT w.id, 'Completed', 'success', 2, true
FROM public.workspaces w
WHERE NOT EXISTS (
    SELECT 1 FROM public.ticket_statuses s WHERE s.workspace_id = w.id AND s.name = 'Completed'
);

INSERT INTO public.ticket_statuses (workspace_id, name, color, sort_order, is_closed_state)
SELECT w.id, 'Closed', 'error', 3, true
FROM public.workspaces w
WHERE NOT EXISTS (
    SELECT 1 FROM public.ticket_statuses s WHERE s.workspace_id = w.id AND s.name = 'Closed'
);

-- Index for ordering and listing statuses per workspace
CREATE INDEX IF NOT EXISTS idx_ticket_statuses_ws_order_name
    ON public.ticket_statuses (workspace_id, sort_order, name);

--
-- Priorities (shared across tickets and contacts, per workspace)
CREATE TABLE IF NOT EXISTS public.priorities (
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY,
    workspace_id integer NOT NULL,
    name character varying(50) NOT NULL,
    color character varying(20) NOT NULL DEFAULT 'neutral',
    sort_order integer NOT NULL DEFAULT 0
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p'
          AND rel.relname = 'priorities'
          AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.priorities
            ADD CONSTRAINT priorities_pkey PRIMARY KEY (id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'priorities_workspace_name_unique'
    ) THEN
        ALTER TABLE ONLY public.priorities
            ADD CONSTRAINT priorities_workspace_name_unique UNIQUE (workspace_id, name);
    END IF;
END $$;

DO $$ BEGIN
    ALTER TABLE public.priorities
        ADD CONSTRAINT priorities_workspace_fk
        FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id) ON DELETE CASCADE;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Seed default priorities for all existing workspaces if none exist
INSERT INTO public.priorities (workspace_id, name, color, sort_order)
SELECT w.id, 'Low', 'warning', 1
FROM public.workspaces w
WHERE NOT EXISTS (SELECT 1 FROM public.priorities p WHERE p.workspace_id = w.id)
ON CONFLICT DO NOTHING;

INSERT INTO public.priorities (workspace_id, name, color, sort_order)
SELECT w.id, 'Normal', 'neutral', 2
FROM public.workspaces w
WHERE NOT EXISTS (
    SELECT 1 FROM public.priorities p WHERE p.workspace_id = w.id AND p.name = 'Normal'
);

INSERT INTO public.priorities (workspace_id, name, color, sort_order)
SELECT w.id, 'High', 'error', 3
FROM public.workspaces w
WHERE NOT EXISTS (
    SELECT 1 FROM public.priorities p WHERE p.workspace_id = w.id AND p.name = 'High'
);

-- Index for ordering and listing priorities per workspace
CREATE INDEX IF NOT EXISTS idx_priorities_ws_order_name
    ON public.priorities (workspace_id, sort_order, name);

--
-- Ticket Types (customizable per workspace)
CREATE TABLE IF NOT EXISTS public.ticket_types (
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY,
    workspace_id integer NOT NULL,
    name character varying(50) NOT NULL,
    color character varying(20) NOT NULL DEFAULT 'neutral',
    sort_order integer NOT NULL DEFAULT 0
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p'
          AND rel.relname = 'ticket_types'
          AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.ticket_types
            ADD CONSTRAINT ticket_types_pkey PRIMARY KEY (id);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'ticket_types_workspace_name_unique'
    ) THEN
        ALTER TABLE ONLY public.ticket_types
            ADD CONSTRAINT ticket_types_workspace_name_unique UNIQUE (workspace_id, name);
    END IF;
END $$;

DO $$ BEGIN
    ALTER TABLE public.ticket_types
        ADD CONSTRAINT ticket_types_workspace_fk
        FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id) ON DELETE CASCADE;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Seed default types for all existing workspaces if none exist
INSERT INTO public.ticket_types (workspace_id, name, color, sort_order)
SELECT w.id, 'Standard', 'neutral', 1
FROM public.workspaces w
WHERE NOT EXISTS (SELECT 1 FROM public.ticket_types t WHERE t.workspace_id = w.id)
ON CONFLICT DO NOTHING;

INSERT INTO public.ticket_types (workspace_id, name, color, sort_order)
SELECT w.id, 'Bug', 'error', 2
FROM public.workspaces w
WHERE NOT EXISTS (
    SELECT 1 FROM public.ticket_types t WHERE t.workspace_id = w.id AND t.name = 'Bug'
);

INSERT INTO public.ticket_types (workspace_id, name, color, sort_order)
SELECT w.id, 'Feature', 'primary', 3
FROM public.workspaces w
WHERE NOT EXISTS (
    SELECT 1 FROM public.ticket_types t WHERE t.workspace_id = w.id AND t.name = 'Feature'
);

-- Index for ordering and listing types per workspace
CREATE INDEX IF NOT EXISTS idx_ticket_types_ws_order_name
    ON public.ticket_types (workspace_id, sort_order, name);

--
CREATE TABLE IF NOT EXISTS public.ticket_inventory (
    id integer NOT NULL,
    ticket_id integer NOT NULL,
    inventory_id integer NOT NULL,
    quantity integer NOT NULL DEFAULT 1,
    unit_price numeric(12,2) NOT NULL DEFAULT 0
);

-- TicketInventories: plural for EF Core compatibility (idempotent)
CREATE TABLE IF NOT EXISTS public.ticket_inventories (
    id integer NOT NULL,
    ticket_id integer NOT NULL,
    inventory_id integer NOT NULL,
    quantity integer NOT NULL DEFAULT 1,
    unit_price numeric(12,2) NOT NULL DEFAULT 0
);

DO $$ BEGIN
    ALTER TABLE public.ticket_inventories ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
        SEQUENCE NAME public.ticket_inventories_id_seq
        START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1
    );
EXCEPTION WHEN duplicate_table OR duplicate_object THEN NULL; END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p' AND rel.relname = 'ticket_inventories' AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.ticket_inventories ADD CONSTRAINT ticket_inventories_pkey PRIMARY KEY (id);
    END IF;
END $$;

DO $$ BEGIN
    ALTER TABLE ONLY public.ticket_inventories
        ADD CONSTRAINT ticket_inventories_ticket_id_fk FOREIGN KEY (ticket_id) REFERENCES public.tickets(id) ON DELETE CASCADE;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    ALTER TABLE ONLY public.ticket_inventories
        ADD CONSTRAINT ticket_inventories_inventory_id_fk FOREIGN KEY (inventory_id) REFERENCES public.inventory(id) ON DELETE CASCADE;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

CREATE INDEX IF NOT EXISTS idx_ticket_inventories_ticket_id ON public.ticket_inventories(ticket_id);
CREATE INDEX IF NOT EXISTS idx_ticket_inventories_inventory_id ON public.ticket_inventories(inventory_id);

DO $$ BEGIN
    ALTER TABLE public.ticket_inventory ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
        SEQUENCE NAME public.ticket_inventory_id_seq
        START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1
    );
EXCEPTION WHEN duplicate_table OR duplicate_object THEN NULL; END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p' AND rel.relname = 'ticket_inventory' AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.ticket_inventory ADD CONSTRAINT ticket_inventory_pkey PRIMARY KEY (id);
    END IF;
END $$;

DO $$ BEGIN
    ALTER TABLE ONLY public.ticket_inventory
        ADD CONSTRAINT ticket_inventory_ticket_id_fk FOREIGN KEY (ticket_id) REFERENCES public.tickets(id) ON DELETE CASCADE;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    ALTER TABLE ONLY public.ticket_inventory
        ADD CONSTRAINT ticket_inventory_inventory_id_fk FOREIGN KEY (inventory_id) REFERENCES public.inventory(id) ON DELETE CASCADE;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

CREATE INDEX IF NOT EXISTS idx_ticket_inventory_ticket_id ON public.ticket_inventory(ticket_id);
CREATE INDEX IF NOT EXISTS idx_ticket_inventory_inventory_id ON public.ticket_inventory(inventory_id);
CREATE TABLE IF NOT EXISTS public.ticket_history (
    workspace_id integer NOT NULL,
    ticket_id integer NOT NULL,
    created_by_user_id integer NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    action text NOT NULL,
    field text NULL,
    old_value text NULL,
    new_value text NULL,
    note text NULL
);

-- Add id column (no identity yet so we can backfill values); skip if already present
ALTER TABLE public.ticket_history
    ADD COLUMN IF NOT EXISTS id integer;

-- Backfill ids for pre-existing rows that were inserted before the column existed
DO $$
BEGIN
    WITH numbered AS (
        SELECT ctid, row_number() OVER (ORDER BY created_at, ticket_id, workspace_id) AS rn
        FROM public.ticket_history
        WHERE id IS NULL
    )
    UPDATE public.ticket_history th
    SET id = numbered.rn
    FROM numbered
    WHERE th.ctid = numbered.ctid;
END $$;

-- Ensure id is non-null before attaching identity
ALTER TABLE public.ticket_history
    ALTER COLUMN id SET NOT NULL;

-- Attach identity generation now that values exist; ignore if already identity
DO $$
BEGIN
    BEGIN
        ALTER TABLE public.ticket_history
            ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;
    EXCEPTION WHEN duplicate_object THEN
        NULL;
    END;
END $$;

-- Align the identity sequence with the current max(id)
DO $$
DECLARE seq_name text;
BEGIN
    SELECT pg_get_serial_sequence('public.ticket_history', 'id') INTO seq_name;
    IF seq_name IS NOT NULL THEN
        PERFORM setval(seq_name, COALESCE((SELECT MAX(id) FROM public.ticket_history), 0));
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint con
        JOIN pg_class rel ON rel.oid = con.conrelid
        JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
        WHERE con.contype = 'p'
          AND rel.relname = 'ticket_history'
          AND nsp.nspname = 'public'
    ) THEN
        ALTER TABLE ONLY public.ticket_history
            ADD CONSTRAINT ticket_history_pkey PRIMARY KEY (id);
    END IF;
END $$;

DO $$ BEGIN
    ALTER TABLE public.ticket_history
        ADD CONSTRAINT ticket_history_workspace_fk
        FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id) ON DELETE CASCADE;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- Index to quickly list history entries for a ticket
CREATE INDEX IF NOT EXISTS idx_ticket_history_ws_ticket_time
    ON public.ticket_history (workspace_id, ticket_id, created_at DESC);

-- migrate:down

--
-- Reporting enhancements: add definition/schedule columns and report_runs table (idempotent)
--

-- Add columns to reports for definition and scheduling (idempotent)
DO $$
BEGIN
    BEGIN
        ALTER TABLE public.reports ADD COLUMN definition_json text;
    EXCEPTION WHEN duplicate_column THEN NULL; END;

    BEGIN
        ALTER TABLE public.reports ADD COLUMN schedule_enabled boolean NOT NULL DEFAULT false;
    EXCEPTION WHEN duplicate_column THEN NULL; END;

    BEGIN
        ALTER TABLE public.reports ADD COLUMN schedule_type character varying(10) NOT NULL DEFAULT 'none'; -- none|daily|weekly|monthly
    EXCEPTION WHEN duplicate_column THEN NULL; END;

    BEGIN
        ALTER TABLE public.reports ADD COLUMN schedule_time time;
    EXCEPTION WHEN duplicate_column THEN NULL; END;

    BEGIN
        ALTER TABLE public.reports ADD COLUMN schedule_day_of_week integer; -- 0=Sunday..6=Saturday
    EXCEPTION WHEN duplicate_column THEN NULL; END;

    BEGIN
        ALTER TABLE public.reports ADD COLUMN schedule_day_of_month integer; -- 1..31
    EXCEPTION WHEN duplicate_column THEN NULL; END;
END$$;

-- Create report_runs table (idempotent)
CREATE TABLE IF NOT EXISTS public.report_runs (
    id integer PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    workspace_id integer NOT NULL,
    report_id integer NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'Pending',
    started_at timestamp with time zone NOT NULL DEFAULT now(),
    finished_at timestamp with time zone NULL,
    row_count integer NOT NULL DEFAULT 0,
    file_path text NULL
);

-- Indexes and FKs
DO $$ BEGIN
    ALTER TABLE ONLY public.report_runs
        ADD CONSTRAINT report_runs_report_fk FOREIGN KEY (report_id) REFERENCES public.reports(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    ALTER TABLE ONLY public.report_runs
        ADD CONSTRAINT report_runs_workspace_fk FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

CREATE INDEX IF NOT EXISTS report_runs_workspace_report_idx ON public.report_runs (workspace_id, report_id, started_at DESC);

-- Add content columns for DB-stored report files (idempotent)
ALTER TABLE public.report_runs ADD COLUMN IF NOT EXISTS file_bytes bytea;
ALTER TABLE public.report_runs ADD COLUMN IF NOT EXISTS content_type text;
ALTER TABLE public.report_runs ADD COLUMN IF NOT EXISTS file_name text;

