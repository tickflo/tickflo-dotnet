-- migrate:up
-- Add workspace portal settings
-- Migration: Add PortalEnabled and PortalAccessToken to workspaces table

ALTER TABLE public.workspaces 
ADD COLUMN IF NOT EXISTS portal_enabled BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS portal_access_token VARCHAR(64);

-- Create index on portal_access_token for quick lookups
CREATE INDEX IF NOT EXISTS idx_workspaces_portal_token ON public.workspaces(portal_access_token) WHERE portal_access_token IS NOT NULL;

-- Add comments
COMMENT ON COLUMN public.workspaces.portal_enabled IS 'Indicates if the workspace public ticket submission portal is enabled';
COMMENT ON COLUMN public.workspaces.portal_access_token IS 'Unique token for accessing the workspace public portal';

-- migrate:down
-- Rollback workspace portal settings

DROP INDEX IF EXISTS idx_workspaces_portal_token;
ALTER TABLE public.workspaces 
DROP COLUMN IF EXISTS portal_access_token,
DROP COLUMN IF EXISTS portal_enabled;
