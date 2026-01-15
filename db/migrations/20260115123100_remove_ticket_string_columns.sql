-- migrate:up

-- Remove old string-based columns from tickets table now that we have ID-based foreign keys
ALTER TABLE public.tickets
    DROP COLUMN IF EXISTS type,
    DROP COLUMN IF EXISTS priority,
    DROP COLUMN IF EXISTS status;

-- migrate:down

-- Restore old string columns (note: data will be lost, values need to be repopulated from IDs)
ALTER TABLE public.tickets
    ADD COLUMN type text DEFAULT 'Standard'::text NOT NULL,
    ADD COLUMN priority text DEFAULT 'Normal'::text NOT NULL,
    ADD COLUMN status text DEFAULT 'New'::text NOT NULL;

-- Attempt to backfill names from IDs if they still exist
UPDATE public.tickets t
SET type = tt.name
FROM public.ticket_types tt
WHERE t.ticket_type_id = tt.id;

UPDATE public.tickets t
SET priority = p.name
FROM public.priorities p
WHERE t.priority_id = p.id;

UPDATE public.tickets t
SET status = s.name
FROM public.ticket_statuses s
WHERE t.status_id = s.id;
