-- migrate:up
SET search_path TO public;
-- Add access_token column to contacts table for client portal access
ALTER TABLE public.contacts ADD COLUMN access_token varchar NULL;

-- Create index on access_token for faster lookups
CREATE INDEX idx_contacts_access_token ON public.contacts(access_token) WHERE access_token IS NOT NULL;

-- migrate:down
DROP INDEX IF EXISTS idx_contacts_access_token;
ALTER TABLE public.contacts DROP COLUMN IF EXISTS access_token;
