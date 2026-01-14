-- migrate:up
-- Add CreatedByContactId column to ticket_comments table
-- This allows tracking comments created by clients in the portal
ALTER TABLE ticket_comments
ADD COLUMN created_by_contact_id INTEGER NULL;

-- Create index for performance on client comment queries
CREATE INDEX idx_ticket_comments_contact_id ON ticket_comments(created_by_contact_id);

-- Add foreign key constraint for data integrity
ALTER TABLE ticket_comments
ADD CONSTRAINT fk_ticket_comments_contact
FOREIGN KEY (created_by_contact_id) REFERENCES contacts(id) ON DELETE SET NULL;

-- migrate:down
-- Rollback: Remove foreign key and index
ALTER TABLE ticket_comments
DROP CONSTRAINT fk_ticket_comments_contact;

DROP INDEX idx_ticket_comments_contact_id;

-- Remove the column
ALTER TABLE ticket_comments
DROP COLUMN created_by_contact_id;
