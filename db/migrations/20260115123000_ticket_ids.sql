-- migrate:up

-- Add ID-based foreign key columns to tickets
ALTER TABLE public.tickets
    ADD COLUMN ticket_type_id integer,
    ADD COLUMN priority_id integer,
    ADD COLUMN status_id integer;

-- Backfill ticket_type_id from ticket_types by matching workspace_id and name
UPDATE public.tickets t
SET ticket_type_id = tt.id
FROM public.ticket_types tt
WHERE tt.workspace_id = t.workspace_id AND tt.name = t.type;

-- Backfill priority_id from priorities by matching workspace_id and name
UPDATE public.tickets t
SET priority_id = p.id
FROM public.priorities p
WHERE p.workspace_id = t.workspace_id AND p.name = t.priority;

-- Backfill status_id from ticket_statuses by matching workspace_id and name
UPDATE public.tickets t
SET status_id = s.id
FROM public.ticket_statuses s
WHERE s.workspace_id = t.workspace_id AND s.name = t.status;

-- Add foreign keys (no cascade)
ALTER TABLE public.tickets
    ADD CONSTRAINT fk_tickets_ticket_type_id FOREIGN KEY (ticket_type_id) REFERENCES public.ticket_types(id),
    ADD CONSTRAINT fk_tickets_priority_id FOREIGN KEY (priority_id) REFERENCES public.priorities(id),
    ADD CONSTRAINT fk_tickets_status_id FOREIGN KEY (status_id) REFERENCES public.ticket_statuses(id);

-- Helpful indexes for lookups
CREATE INDEX IF NOT EXISTS idx_tickets_workspace_ticket_type_id ON public.tickets (workspace_id, ticket_type_id);
CREATE INDEX IF NOT EXISTS idx_tickets_workspace_priority_id ON public.tickets (workspace_id, priority_id);
CREATE INDEX IF NOT EXISTS idx_tickets_workspace_status_id ON public.tickets (workspace_id, status_id);

-- migrate:down

-- Drop indexes
DROP INDEX IF EXISTS idx_tickets_workspace_ticket_type_id;
DROP INDEX IF EXISTS idx_tickets_workspace_priority_id;
DROP INDEX IF EXISTS idx_tickets_workspace_status_id;

-- Drop foreign keys
ALTER TABLE public.tickets
    DROP CONSTRAINT IF EXISTS fk_tickets_ticket_type_id,
    DROP CONSTRAINT IF EXISTS fk_tickets_priority_id,
    DROP CONSTRAINT IF EXISTS fk_tickets_status_id;

-- Remove columns
ALTER TABLE public.tickets
    DROP COLUMN IF EXISTS ticket_type_id,
    DROP COLUMN IF EXISTS priority_id,
    DROP COLUMN IF EXISTS status_id;
