-- migrate:up
-- Create ticket_comments table
CREATE TABLE public.ticket_comments (
    id SERIAL PRIMARY KEY,
    workspace_id INTEGER NOT NULL,
    ticket_id INTEGER NOT NULL,
    content TEXT NOT NULL,
    is_visible_to_client BOOLEAN DEFAULT false NOT NULL,
    created_by_user_id INTEGER,
    created_by_contact_id INTEGER,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT now() NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE,
    updated_by_user_id INTEGER,
    updated_by_contact_id INTEGER,
    FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id) ON DELETE CASCADE,
    FOREIGN KEY (ticket_id) REFERENCES public.tickets(id) ON DELETE CASCADE,
    FOREIGN KEY (created_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT,
    FOREIGN KEY (created_by_contact_id) REFERENCES public.contacts(id) ON DELETE
    SET
        NULL,
        FOREIGN KEY (updated_by_user_id) REFERENCES public.users(id) ON DELETE
    SET
        NULL,
        FOREIGN KEY (updated_by_contact_id) REFERENCES public.contacts(id) ON DELETE
    SET
        NULL
);

-- Create indexes for faster lookups
CREATE INDEX idx_ticket_comments_ticket_id ON public.ticket_comments(ticket_id);

CREATE INDEX idx_ticket_comments_workspace_id ON public.ticket_comments(workspace_id);

CREATE INDEX idx_ticket_comments_created_by_user_id ON public.ticket_comments(created_by_user_id);

CREATE INDEX idx_ticket_comments_created_by_contact_id ON public.ticket_comments(created_by_contact_id);

-- Add access_token column to contacts table for client portal access
ALTER TABLE
    public.contacts
ADD
    COLUMN access_token varchar NULL;

-- Create index on access_token for faster lookups
CREATE INDEX idx_contacts_access_token ON public.contacts(access_token)
WHERE
    access_token IS NOT NULL;

-- migrate:down
DROP INDEX IF EXISTS idx_contacts_access_token;

ALTER TABLE
    public.contacts DROP COLUMN IF EXISTS access_token;

DROP INDEX IF EXISTS idx_ticket_comments_created_by_user_id;

DROP INDEX IF EXISTS idx_ticket_comments_created_by_contact_id;

DROP INDEX IF EXISTS idx_ticket_comments_workspace_id;

DROP INDEX IF EXISTS idx_ticket_comments_ticket_id;

DROP TABLE IF EXISTS public.ticket_comments;