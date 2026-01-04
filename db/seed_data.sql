-- =============================================
-- Tickflo Demo Seed Data
-- This file contains demo data for testing and development
-- =============================================

-- Clear existing data (in reverse order of dependencies)
DELETE FROM public.notifications;
DELETE FROM public.user_notification_preferences;
DELETE FROM public.role_permissions;
DELETE FROM public.user_workspace_roles;
DELETE FROM public.user_workspaces;
DELETE FROM public.ticket_history;
DELETE FROM public.ticket_inventories;
DELETE FROM public.tickets;
DELETE FROM public.team_members;
DELETE FROM public.teams;
DELETE FROM public.contact_locations;
DELETE FROM public.contacts;
DELETE FROM public.inventory;
DELETE FROM public.ticket_statuses;
DELETE FROM public.priorities;
DELETE FROM public.ticket_types;
DELETE FROM public.report_runs;
DELETE FROM public.reports;
DELETE FROM public.locations;
DELETE FROM public.roles;
DELETE FROM public.permissions;
DELETE FROM public.tokens;
DELETE FROM public.workspaces;
DELETE FROM public.users;

-- Reset sequences
ALTER SEQUENCE public.users_id_seq RESTART WITH 1;
ALTER SEQUENCE public.workspaces_id_seq RESTART WITH 1;
ALTER SEQUENCE public.roles_id_seq RESTART WITH 1;
ALTER SEQUENCE public.permissions_id_seq RESTART WITH 1;
ALTER SEQUENCE public.locations_id_seq RESTART WITH 1;
ALTER SEQUENCE public.reports_id_seq RESTART WITH 1;
ALTER SEQUENCE public.contacts_id_seq RESTART WITH 1;
ALTER SEQUENCE public.inventory_id_seq RESTART WITH 1;
ALTER SEQUENCE public.teams_id_seq RESTART WITH 1;
ALTER SEQUENCE public.tickets_id_seq RESTART WITH 1;
ALTER SEQUENCE public.ticket_statuses_id_seq RESTART WITH 1;
ALTER SEQUENCE public.priorities_id_seq RESTART WITH 1;
ALTER SEQUENCE public.ticket_types_id_seq RESTART WITH 1;
ALTER SEQUENCE public.report_runs_id_seq RESTART WITH 1;
ALTER SEQUENCE public.notifications_id_seq RESTART WITH 1;

-- =============================================
-- Users
-- Demo users are created with NULL passwords
-- They must set a password on first login
-- =============================================
INSERT INTO public.users (name, email, email_confirmed, password_hash, system_admin, created_at) VALUES
('John Admin', 'admin@demo.com', true, NULL, true, NOW()),
('Sarah Manager', 'sarah@demo.com', true, NULL, false, NOW()),
('Mike Technician', 'mike@demo.com', true, NULL, false, NOW()),
('Lisa Support', 'lisa@demo.com', true, NULL, false, NOW()),
('Tom Developer', 'tom@demo.com', true, NULL, false, NOW()),
('Emma Sales', 'emma@demo.com', true, NULL, false, NOW());

-- =============================================
-- Workspaces
-- =============================================
INSERT INTO public.workspaces (name, slug, created_by, created_at) VALUES
('Acme Corporation', 'acme-corp', 1, NOW()),
('TechStart Inc', 'techstart', 1, NOW()),
('Global Services', 'global-services', 1, NOW());

-- =============================================
-- User Workspaces (User-Workspace associations)
-- =============================================
INSERT INTO public.user_workspaces (user_id, workspace_id, accepted, created_by, created_at) VALUES
-- Acme Corporation
(1, 1, true, 1, NOW()),
(2, 1, true, 1, NOW()),
(3, 1, true, 1, NOW()),
(4, 1, true, 1, NOW()),
-- TechStart Inc
(1, 2, true, 1, NOW()),
(5, 2, true, 1, NOW()),
(6, 2, true, 1, NOW()),
-- Global Services
(1, 3, true, 1, NOW()),
(2, 3, true, 1, NOW());

-- =============================================
-- Permissions
-- =============================================
INSERT INTO public.permissions (resource, action) VALUES
('tickets', 'create'),
('tickets', 'read'),
('tickets', 'update'),
('tickets', 'delete'),
('tickets', 'assign'),
('contacts', 'create'),
('contacts', 'read'),
('contacts', 'update'),
('contacts', 'delete'),
('locations', 'create'),
('locations', 'read'),
('locations', 'update'),
('locations', 'delete'),
('inventory', 'create'),
('inventory', 'read'),
('inventory', 'update'),
('inventory', 'delete'),
('teams', 'create'),
('teams', 'read'),
('teams', 'update'),
('teams', 'delete'),
('reports', 'create'),
('reports', 'read'),
('reports', 'update'),
('reports', 'delete'),
('reports', 'execute'),
('roles', 'create'),
('roles', 'read'),
('roles', 'update'),
('roles', 'delete'),
('roles', 'assign'),
('users', 'invite'),
('users', 'read'),
('users', 'update'),
('users', 'delete'),
('workspace', 'manage');

-- =============================================
-- Roles
-- =============================================
INSERT INTO public.roles (workspace_id, name, admin, created_by, created_at) VALUES
-- Acme Corporation roles
(1, 'Admin', true, 1, NOW()),
(1, 'Manager', false, 1, NOW()),
(1, 'Technician', false, 1, NOW()),
(1, 'Support Agent', false, 1, NOW()),
-- TechStart Inc roles
(2, 'Admin', true, 1, NOW()),
(2, 'Developer', false, 1, NOW()),
(2, 'Sales Rep', false, 1, NOW()),
-- Global Services roles
(3, 'Admin', true, 1, NOW()),
(3, 'Operator', false, 1, NOW());

-- =============================================
-- Role Permissions
-- =============================================
-- Acme Corp Admin (role_id 1) - All permissions
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT 1, id, 1, NOW() FROM public.permissions;

-- Acme Corp Manager (role_id 2)
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT 2, id, 1, NOW() FROM public.permissions 
WHERE resource IN ('tickets', 'contacts', 'locations', 'inventory', 'teams', 'reports', 'users');

-- Acme Corp Technician (role_id 3)
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT 3, id, 1, NOW() FROM public.permissions 
WHERE resource IN ('tickets', 'contacts', 'inventory') AND action IN ('create', 'read', 'update');

-- Acme Corp Support Agent (role_id 4)
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT 4, id, 1, NOW() FROM public.permissions 
WHERE resource IN ('tickets', 'contacts') AND action IN ('create', 'read', 'update');

-- TechStart Admin (role_id 5) - All permissions
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT 5, id, 1, NOW() FROM public.permissions;

-- TechStart Developer (role_id 6)
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT 6, id, 1, NOW() FROM public.permissions 
WHERE resource IN ('tickets', 'contacts', 'inventory');

-- TechStart Sales Rep (role_id 7)
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT 7, id, 1, NOW() FROM public.permissions 
WHERE resource IN ('contacts') OR (resource = 'tickets' AND action = 'read');

-- Global Services Admin (role_id 8) - All permissions
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT 8, id, 1, NOW() FROM public.permissions;

-- Global Services Operator (role_id 9)
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT 9, id, 1, NOW() FROM public.permissions 
WHERE resource IN ('tickets', 'locations', 'inventory');

-- =============================================
-- User Workspace Roles
-- =============================================
INSERT INTO public.user_workspace_roles (user_id, workspace_id, role_id, created_by, created_at) VALUES
-- Acme Corporation
(1, 1, 1, 1, NOW()), -- John is Admin
(2, 1, 2, 1, NOW()), -- Sarah is Manager
(3, 1, 3, 1, NOW()), -- Mike is Technician
(4, 1, 4, 1, NOW()), -- Lisa is Support Agent
-- TechStart Inc
(1, 2, 5, 1, NOW()), -- John is Admin
(5, 2, 6, 1, NOW()), -- Tom is Developer
(6, 2, 7, 1, NOW()), -- Emma is Sales Rep
-- Global Services
(1, 3, 8, 1, NOW()), -- John is Admin
(2, 3, 9, 1, NOW()); -- Sarah is Operator

-- =============================================
-- Locations
-- =============================================
INSERT INTO public.locations (workspace_id, name, address, active, created_by, created_at) VALUES
-- Acme Corporation
(1, 'Headquarters', '123 Main Street, New York, NY 10001', true, 1, NOW()),
(1, 'West Coast Office', '456 Tech Blvd, San Francisco, CA 94105', true, 1, NOW()),
(1, 'Chicago Branch', '789 Lake Shore Dr, Chicago, IL 60611', true, 1, NOW()),
(1, 'Warehouse A', '321 Industrial Pkwy, Newark, NJ 07102', true, 1, NOW()),
-- TechStart Inc
(2, 'Main Office', '555 Startup Ave, Austin, TX 78701', true, 1, NOW()),
(2, 'R&D Lab', '777 Innovation Dr, Seattle, WA 98101', true, 1, NOW()),
-- Global Services
(3, 'Regional Hub', '999 Commerce St, Dallas, TX 75201', true, 1, NOW()),
(3, 'Service Center', '111 Support Way, Phoenix, AZ 85001', true, 1, NOW());

-- =============================================
-- Contacts
-- =============================================
INSERT INTO public.contacts (workspace_id, name, email, phone, company, title, notes, tags, preferred_channel, priority, status, assigned_user_id, last_interaction, created_at) VALUES
-- Acme Corporation
(1, 'Robert Johnson', 'robert.j@client1.com', '555-0101', 'Client Corp A', 'IT Director', 'Key decision maker for IT purchases', 'vip,enterprise', 'email', 'High', 'Active', 2, NOW() - INTERVAL '2 days', NOW() - INTERVAL '30 days'),
(1, 'Jennifer Williams', 'jennifer.w@client2.com', '555-0102', 'Client Corp B', 'Operations Manager', 'Handles day-to-day operations', 'enterprise,support', 'phone', 'Normal', 'Active', 3, NOW() - INTERVAL '5 days', NOW() - INTERVAL '60 days'),
(1, 'Michael Brown', 'michael.b@client3.com', '555-0103', 'Small Business Inc', 'Owner', 'Small business client', 'smb,friendly', 'email', 'Normal', 'Active', 4, NOW() - INTERVAL '1 day', NOW() - INTERVAL '15 days'),
(1, 'Emily Davis', 'emily.d@client4.com', '555-0104', 'Enterprise Solutions', 'CTO', 'Technical contact for large projects', 'vip,technical', 'email', 'High', 'Active', 2, NOW() - INTERVAL '3 days', NOW() - INTERVAL '45 days'),
(1, 'David Martinez', 'david.m@client5.com', '555-0105', 'MidSize Corp', 'Facilities Manager', 'Manages facility issues', 'facility,regular', 'phone', 'Normal', 'Active', 3, NOW() - INTERVAL '7 days', NOW() - INTERVAL '90 days'),
(1, 'Jessica Taylor', 'jessica.t@client6.com', '555-0106', 'Startup X', 'CEO', 'Fast-growing startup', 'startup,urgent', 'email', 'High', 'Active', 2, NOW(), NOW() - INTERVAL '10 days'),
-- TechStart Inc
(2, 'Christopher Anderson', 'chris.a@prospect1.com', '555-0201', 'Prospect Alpha', 'VP Technology', 'Evaluating our platform', 'prospect,interested', 'email', 'High', 'Active', 5, NOW() - INTERVAL '1 day', NOW() - INTERVAL '5 days'),
(2, 'Amanda White', 'amanda.w@customer1.com', '555-0202', 'Customer Beta', 'Product Manager', 'Existing customer, very happy', 'customer,advocate', 'email', 'Normal', 'Active', 6, NOW() - INTERVAL '4 days', NOW() - INTERVAL '120 days'),
(2, 'James Thomas', 'james.t@partner1.com', '555-0203', 'Partner Gamma', 'Business Development', 'Strategic partner', 'partner,strategic', 'phone', 'High', 'Active', 5, NOW() - INTERVAL '2 days', NOW() - INTERVAL '20 days'),
-- Global Services
(3, 'Linda Harris', 'linda.h@globclient1.com', '555-0301', 'Global Client 1', 'Regional Manager', 'Multi-location client', 'global,enterprise', 'email', 'High', 'Active', 2, NOW() - INTERVAL '1 day', NOW() - INTERVAL '40 days'),
(3, 'Daniel Clark', 'daniel.c@globclient2.com', '555-0302', 'Global Client 2', 'Operations Director', 'Complex service needs', 'global,complex', 'phone', 'Normal', 'Active', 2, NOW() - INTERVAL '6 days', NOW() - INTERVAL '75 days');

-- =============================================
-- Contact Locations (many-to-many)
-- =============================================
INSERT INTO public.contact_locations (contact_id, location_id, workspace_id) VALUES
-- Acme Corporation
(1, 1, 1),
(2, 2, 1),
(3, 3, 1),
(4, 1, 1),
(4, 2, 1), -- Emily has multiple locations
(5, 3, 1),
(6, 1, 1),
-- TechStart Inc
(7, 5, 2),
(8, 5, 2),
(9, 6, 2),
-- Global Services
(10, 7, 3),
(10, 8, 3), -- Linda has multiple locations
(11, 8, 3);

-- =============================================
-- Teams
-- =============================================
INSERT INTO public.teams (workspace_id, name, description, created_by, created_at) VALUES
-- Acme Corporation
(1, 'Field Services', 'On-site technical support team', 1, NOW()),
(1, 'Help Desk', 'First line of support', 1, NOW()),
(1, 'Infrastructure', 'IT infrastructure maintenance', 1, NOW()),
-- TechStart Inc
(2, 'Engineering', 'Product development team', 1, NOW()),
(2, 'Customer Success', 'Customer support and onboarding', 1, NOW()),
-- Global Services
(3, 'Operations', 'Daily operations team', 1, NOW());

-- =============================================
-- Team Members
-- =============================================
INSERT INTO public.team_members (team_id, user_id) VALUES
-- Acme Corporation
(1, 3), -- Mike in Field Services
(2, 4), -- Lisa in Help Desk
(3, 3), -- Mike also in Infrastructure
-- TechStart Inc
(4, 5), -- Tom in Engineering
(5, 6), -- Emma in Customer Success
-- Global Services
(6, 2); -- Sarah in Operations

-- =============================================
-- Inventory
-- =============================================
INSERT INTO public.inventory (workspace_id, sku, name, description, quantity, category, status, location_id, cost, price) VALUES
-- Acme Corporation
(1, 'LAPTOP-001', 'Dell Latitude 7420', 'i7-1185G7, 16GB RAM, 512GB SSD - Serial: DL7420-XYZ123', 1, 'Laptops', 'active', 1, 1299.99, 1499.99),
(1, 'LAPTOP-002', 'Dell Latitude 7420', 'i7-1185G7, 16GB RAM, 512GB SSD - Serial: DL7420-XYZ124', 1, 'Laptops', 'active', 2, 1299.99, 1499.99),
(1, 'DESKTOP-001', 'HP EliteDesk 800 G6', 'i7-10700, 32GB RAM, 1TB SSD - Serial: HP800-ABC456', 1, 'Desktops', 'active', 1, 1099.99, 1299.99),
(1, 'MONITOR-001', 'Dell UltraSharp U2720Q', '27" 4K IPS Monitor - Serial: DU27-MON789', 1, 'Monitors', 'active', 1, 499.99, 599.99),
(1, 'MONITOR-002', 'Dell UltraSharp U2720Q', '27" 4K IPS Monitor - Serial: DU27-MON790', 1, 'Monitors', 'active', 2, 499.99, 599.99),
(1, 'PRINTER-001', 'HP LaserJet Pro M404dn', 'Monochrome laser printer - Serial: HPLJ-PRT001', 1, 'Printers', 'active', 1, 299.99, 399.99),
(1, 'ROUTER-001', 'Cisco Catalyst 9300', '48-port managed switch - Serial: CC9300-NET001', 1, 'Network', 'active', 1, 4199.99, 4999.99),
(1, 'SERVER-001', 'Dell PowerEdge R740', 'Dual Xeon, 128GB RAM, RAID storage - Serial: DPE-R740-SRV01', 1, 'Servers', 'active', 4, 7999.99, 8999.99),
(1, 'PHONE-001', 'iPhone 14 Pro', '256GB, Space Black - Serial: IP14P-PH001', 1, 'Mobile Devices', 'active', 1, 899.99, 1099.99),
(1, 'TABLET-001', 'iPad Pro 12.9"', '512GB, Wi-Fi + Cellular - Serial: IPP129-TAB01', 1, 'Mobile Devices', 'active', 1, 1099.99, 1299.99),
-- TechStart Inc
(2, 'DEV-LAPTOP-001', 'MacBook Pro 16"', 'M2 Max, 32GB RAM, 1TB SSD - Serial: MBP16-DEV01', 1, 'Laptops', 'active', 5, 3099.99, 3499.99),
(2, 'DEV-LAPTOP-002', 'MacBook Pro 16"', 'M2 Max, 32GB RAM, 1TB SSD - Serial: MBP16-DEV02', 1, 'Laptops', 'active', 5, 3099.99, 3499.99),
(2, 'DEV-MONITOR-001', 'LG UltraWide 38"', '38" Curved UltraWide - Serial: LG38-MON01', 1, 'Monitors', 'active', 5, 1099.99, 1299.99),
-- Global Services
(3, 'GS-LAPTOP-001', 'Lenovo ThinkPad X1 Carbon', 'i7-1260P, 16GB RAM, 512GB SSD - Serial: LTX1C-GS01', 1, 'Laptops', 'active', 7, 1699.99, 1899.99);

-- =============================================
-- Ticket Statuses
-- =============================================
INSERT INTO public.ticket_statuses (workspace_id, name, color, sort_order) VALUES
-- Acme Corporation
(1, 'New', 'info', 0),
(1, 'In Progress', 'warning', 1),
(1, 'Waiting', 'neutral', 2),
(1, 'Resolved', 'success', 3),
(1, 'Closed', 'neutral', 4),
-- TechStart Inc
(2, 'Open', 'info', 0),
(2, 'Working', 'warning', 1),
(2, 'Done', 'success', 2),
-- Global Services
(3, 'Submitted', 'info', 0),
(3, 'Assigned', 'warning', 1),
(3, 'Completed', 'success', 2);

-- =============================================
-- Priorities
-- =============================================
INSERT INTO public.priorities (workspace_id, name, color, sort_order) VALUES
-- Acme Corporation
(1, 'Low', 'success', 0),
(1, 'Normal', 'info', 1),
(1, 'High', 'warning', 2),
(1, 'Urgent', 'error', 3),
-- TechStart Inc
(2, 'P3', 'neutral', 0),
(2, 'P2', 'warning', 1),
(2, 'P1', 'error', 2),
-- Global Services
(3, 'Low', 'success', 0),
(3, 'Medium', 'warning', 1),
(3, 'High', 'error', 2);

-- =============================================
-- Ticket Types
-- =============================================
INSERT INTO public.ticket_types (workspace_id, name, color, sort_order) VALUES
-- Acme Corporation
(1, 'Incident', 'error', 0),
(1, 'Service Request', 'info', 1),
(1, 'Change Request', 'warning', 2),
(1, 'Problem', 'error', 3),
(1, 'Question', 'info', 4),
-- TechStart Inc
(2, 'Bug', 'error', 0),
(2, 'Feature Request', 'success', 1),
(2, 'Support', 'info', 2),
-- Global Services
(3, 'Maintenance', 'warning', 0),
(3, 'Repair', 'error', 1),
(3, 'Installation', 'info', 2);

-- =============================================
-- Tickets
-- =============================================
INSERT INTO public.tickets (workspace_id, contact_id, location_id, subject, description, type, priority, status, assigned_user_id, assigned_team_id, created_at, updated_at) VALUES
-- Acme Corporation - Recent and varied
(1, 1, 1, 'Email not syncing on mobile device', 'Robert reports that his iPhone is not syncing emails since this morning. He can receive but not send.', 'Incident', 'High', 'In Progress', 3, 1, NOW() - INTERVAL '2 hours', NOW() - INTERVAL '1 hour'),
(1, 2, 2, 'Request new laptop for new hire', 'Jennifer needs a new laptop configured for their new IT specialist starting next Monday.', 'Service Request', 'Normal', 'New', 2, NULL, NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
(1, 3, 3, 'Printer paper jam recurring issue', 'Michael says the office printer keeps jamming. Might need maintenance or replacement.', 'Problem', 'Normal', 'Waiting', 3, 1, NOW() - INTERVAL '3 days', NOW() - INTERVAL '1 day'),
(1, 4, 1, 'VPN connection dropping frequently', 'Emily experiences VPN disconnections every 10-15 minutes when working remotely.', 'Incident', 'High', 'In Progress', 3, 3, NOW() - INTERVAL '4 hours', NOW() - INTERVAL '2 hours'),
(1, 5, 3, 'Install new access control system', 'David requests installation of card readers at the Chicago facility entrance.', 'Change Request', 'Normal', 'New', NULL, NULL, NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
(1, 6, 1, 'Software license inquiry', 'Jessica asks about available licenses for Adobe Creative Cloud for her design team.', 'Question', 'Low', 'Resolved', 4, 2, NOW() - INTERVAL '5 days', NOW() - INTERVAL '4 days'),
(1, 1, 1, 'Cannot access shared drive', 'Robert cannot access the Finance shared drive. Getting "Access Denied" error.', 'Incident', 'High', 'Resolved', 3, 1, NOW() - INTERVAL '7 days', NOW() - INTERVAL '6 days'),
(1, 2, 2, 'Monitor flickering issue', 'Jennifer''s monitor flickers intermittently. Might be cable or monitor issue.', 'Incident', 'Normal', 'Closed', 3, 1, NOW() - INTERVAL '10 days', NOW() - INTERVAL '9 days'),
(1, 4, 1, 'Upgrade Office 365 subscription', 'Emily''s team needs upgraded Office 365 licenses with advanced features.', 'Service Request', 'Normal', 'Closed', 2, NULL, NOW() - INTERVAL '15 days', NOW() - INTERVAL '12 days'),
(1, 3, 3, 'WiFi slow in conference room', 'Michael reports very slow WiFi speeds in the main conference room during meetings.', 'Problem', 'Normal', 'Resolved', 3, 3, NOW() - INTERVAL '8 days', NOW() - INTERVAL '7 days'),
-- TechStart Inc
(2, 7, 5, 'Login page not loading', 'Christopher reports that the login page shows a blank screen on Chrome.', 'Bug', 'P1', 'Working', 5, 4, NOW() - INTERVAL '3 hours', NOW() - INTERVAL '1 hour'),
(2, 8, 5, 'Feature request: Dark mode', 'Amanda suggests adding a dark mode option for the dashboard interface.', 'Feature Request', 'P3', 'Open', NULL, NULL, NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),
(2, 9, 6, 'API documentation outdated', 'James mentions that the API docs for v2.3 still reference old endpoints.', 'Support', 'P2', 'Working', 5, 4, NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day'),
(2, 7, 5, 'Export function timeout', 'Christopher gets timeout errors when exporting large reports (>10k rows).', 'Bug', 'P2', 'Open', NULL, 4, NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
(2, 8, 5, 'SSO integration with Azure AD', 'Amanda wants to enable SSO for her team using Azure Active Directory.', 'Feature Request', 'P2', 'Done', 5, 4, NOW() - INTERVAL '20 days', NOW() - INTERVAL '15 days'),
-- Global Services
(3, 10, 7, 'HVAC system making loud noise', 'Linda reports unusual sounds from the air conditioning unit in the server room.', 'Maintenance', 'High', 'Assigned', 2, 6, NOW() - INTERVAL '6 hours', NOW() - INTERVAL '4 hours'),
(3, 11, 8, 'Replace broken door lock', 'Daniel says the main entrance lock is stuck and needs replacement.', 'Repair', 'Medium', 'Submitted', NULL, NULL, NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
(3, 10, 7, 'Fire alarm system annual test', 'Linda schedules annual fire alarm testing for next week.', 'Maintenance', 'Medium', 'Submitted', 2, 6, NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),
(3, 11, 8, 'Emergency lighting installation', 'Daniel requests installation of emergency exit lighting in the new warehouse section.', 'Installation', 'High', 'Assigned', 2, 6, NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day'),
(3, 10, 8, 'Security camera not recording', 'Linda notices that camera #3 in the parking lot is not recording footage.', 'Repair', 'High', 'Completed', 2, 6, NOW() - INTERVAL '5 days', NOW() - INTERVAL '4 days');

-- =============================================
-- Ticket Inventory (linking tickets to assets)
-- =============================================
INSERT INTO public.ticket_inventories (ticket_id, inventory_id) VALUES
-- Acme Corporation
(1, 9),  -- Email issue related to iPhone
(2, 10), -- New laptop request - assigning iPad temporarily
(3, 6),  -- Printer jam issue
(4, 1),  -- VPN issue on Sarah's laptop
(8, 4),  -- Monitor flickering - Jennifer's monitor
(10, 7); -- WiFi issue related to router

-- =============================================
-- Ticket History
-- =============================================
INSERT INTO public.ticket_history (workspace_id, ticket_id, created_by_user_id, action, field, old_value, new_value, note, created_at) VALUES
-- History for ticket 1 (Email not syncing)
(1, 1, 3, 'status_change', 'status', 'New', 'In Progress', 'Starting investigation', NOW() - INTERVAL '1 hour'),
(1, 1, 3, 'comment', NULL, NULL, NULL, 'Checked email server logs. Issue appears to be with device configuration.', NOW() - INTERVAL '45 minutes'),
-- History for ticket 4 (VPN connection)
(1, 4, 3, 'status_change', 'status', 'New', 'In Progress', 'Assigned to infrastructure team', NOW() - INTERVAL '2 hours'),
(1, 4, 3, 'comment', NULL, NULL, NULL, 'Updated VPN client to latest version. Monitoring for stability.', NOW() - INTERVAL '1 hour'),
-- History for ticket 6 (Software license - resolved)
(1, 6, 4, 'status_change', 'status', 'New', 'In Progress', 'Checking license availability', NOW() - INTERVAL '5 days'),
(1, 6, 4, 'comment', NULL, NULL, NULL, 'We have 5 available licenses. Sent details to Jessica.', NOW() - INTERVAL '4 days 12 hours'),
(1, 6, 4, 'status_change', 'status', 'In Progress', 'Resolved', 'Issue resolved', NOW() - INTERVAL '4 days'),
-- History for ticket 7 (Shared drive access - resolved)
(1, 7, 3, 'status_change', 'status', 'New', 'In Progress', 'Investigating permissions', NOW() - INTERVAL '7 days'),
(1, 7, 3, 'comment', NULL, NULL, NULL, 'Found the issue - AD group membership was missing. Added user to correct group.', NOW() - INTERVAL '6 days 18 hours'),
(1, 7, 3, 'status_change', 'status', 'In Progress', 'Resolved', 'Access restored', NOW() - INTERVAL '6 days'),
-- History for ticket 11 (Login page bug)
(2, 11, 5, 'status_change', 'status', 'Open', 'Working', 'Reproducing the issue', NOW() - INTERVAL '2 hours'),
(2, 11, 5, 'priority_change', 'priority', 'P2', 'P1', 'Escalating - affects multiple users', NOW() - INTERVAL '1 hour'),
(2, 11, 5, 'comment', NULL, NULL, NULL, 'Issue traced to caching problem. Deploying fix now.', NOW() - INTERVAL '30 minutes'),
-- History for ticket 16 (HVAC noise)
(3, 16, 2, 'status_change', 'status', 'Submitted', 'Assigned', 'Scheduled technician visit', NOW() - INTERVAL '4 hours'),
(3, 16, 2, 'comment', NULL, NULL, NULL, 'HVAC technician will arrive at 2 PM today.', NOW() - INTERVAL '3 hours');

-- =============================================
-- Reports
-- =============================================
INSERT INTO public.reports (workspace_id, name, ready, created_by, created_at) VALUES
(1, 'Open Tickets by Priority', true, 1, NOW() - INTERVAL '60 days'),
(1, 'Monthly Resolution Time', true, 1, NOW() - INTERVAL '60 days'),
(1, 'Tickets by Location', true, 1, NOW() - INTERVAL '60 days'),
(1, 'Asset Warranty Expiry', true, 1, NOW() - INTERVAL '60 days'),
(2, 'Bug Report Summary', true, 1, NOW() - INTERVAL '45 days'),
(2, 'Customer Satisfaction', false, 1, NOW() - INTERVAL '30 days'),
(3, 'Maintenance Schedule', true, 1, NOW() - INTERVAL '50 days');

-- =============================================
-- Report Runs
-- =============================================
INSERT INTO public.report_runs (workspace_id, report_id, status, started_at, finished_at) VALUES
(1, 1, 'Completed', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
(1, 1, 'Completed', NOW() - INTERVAL '7 days', NOW() - INTERVAL '7 days'),
(1, 2, 'Completed', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
(1, 3, 'Completed', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),
(1, 4, 'Completed', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),
(2, 5, 'Completed', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
(3, 7, 'Completed', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days');

-- =============================================
-- Update last_run for reports
-- =============================================
UPDATE public.reports SET last_run = NOW() - INTERVAL '1 day' WHERE id = 1;
UPDATE public.reports SET last_run = NOW() - INTERVAL '2 days' WHERE id = 2;
UPDATE public.reports SET last_run = NOW() - INTERVAL '3 days' WHERE id = 3;
UPDATE public.reports SET last_run = NOW() - INTERVAL '5 days' WHERE id = 4;
UPDATE public.reports SET last_run = NOW() - INTERVAL '1 day' WHERE id = 5;
UPDATE public.reports SET last_run = NOW() - INTERVAL '4 days' WHERE id = 7;

-- =============================================
-- Summary
-- =============================================
-- This seed data includes:
-- - 6 demo users (1 admin, 5 regular users)
-- - 3 workspaces
-- - 9 roles with appropriate permissions
-- - 8 locations across workspaces
-- - 11 contacts with various attributes
-- - 6 teams with members
-- - 14 inventory items
-- - 20 tickets with various statuses
-- - Ticket history entries showing activity
-- - 7 reports with run history
--
-- Demo Users:
-- All demo users are created with NULL passwords and must set a password on first login.
-- Try logging in with any of these email addresses - you'll be redirected to set a password:
-- - admin@demo.com (System Admin)
-- - sarah@demo.com (Manager)
-- - mike@demo.com (Technician)
-- - lisa@demo.com (Support Agent)
-- - tom@demo.com (Developer)
-- - emma@demo.com (Sales Rep)

-- =============================================
-- Notifications
-- Sample notifications for different types and delivery methods
-- =============================================
INSERT INTO public.notifications (workspace_id, user_id, type, delivery_method, priority, subject, body, status, created_at, created_by) VALUES
-- Email notifications
(1, 2, 'workspace_invite', 'email', 'high', 'Welcome to Acme Corporation', '<p>You have been invited to join <strong>Acme Corporation</strong> workspace.</p><p>Click here to accept the invitation and get started.</p>', 'sent', NOW() - INTERVAL '3 days', 1),
(1, 3, 'ticket_assigned', 'email', 'normal', 'Ticket #1001 assigned to you', '<p>A new ticket has been assigned to you:</p><p><strong>Title:</strong> Email not syncing on mobile device</p><p><strong>Priority:</strong> High</p>', 'sent', NOW() - INTERVAL '2 days', 2),
(2, 5, 'ticket_comment', 'email', 'normal', 'New comment on Ticket #1005', '<p>Lisa Johnson added a comment to your ticket:</p><blockquote>I have identified the root cause. Will update shortly.</blockquote>', 'sent', NOW() - INTERVAL '1 day', 4),

-- In-app notifications
(1, 2, 'ticket_status_change', 'in_app', 'normal', 'Ticket #1001 status updated', 'Ticket status changed from Open to In Progress', 'sent', NOW() - INTERVAL '2 hours', 3),
(1, 3, 'ticket_assigned', 'in_app', 'normal', 'New ticket assigned', 'Ticket #1015 "Network connectivity issues" has been assigned to you', 'sent', NOW() - INTERVAL '1 hour', 2),
(2, 4, 'report_completed', 'in_app', 'low', 'Weekly report completed', 'Your weekly ticket report has finished processing', 'sent', NOW() - INTERVAL '30 minutes', NULL),
(3, 6, 'mention', 'in_app', 'high', 'You were mentioned in a comment', 'Tom Wilson mentioned you in ticket #1012', 'pending', NOW() - INTERVAL '15 minutes', 5),

-- Pending batch notifications
(1, 2, 'ticket_summary', 'email', 'low', 'Daily Ticket Summary', '<p>Your daily ticket summary for Acme Corporation:</p><ul><li>5 new tickets</li><li>3 resolved</li><li>2 pending your review</li></ul>', 'pending', NOW(), NULL),
(1, 3, 'ticket_summary', 'email', 'low', 'Daily Ticket Summary', '<p>Your daily ticket summary for Acme Corporation:</p><ul><li>3 assigned to you</li><li>1 awaiting response</li></ul>', 'pending', NOW(), NULL),

-- Failed notification example
(2, 5, 'password_reset', 'email', 'urgent', 'Password Reset Request', '<p>You requested a password reset for your account.</p><p>Click the link below to reset your password:</p>', 'failed', NOW() - INTERVAL '1 day', NULL);

-- Add failure reason for the failed notification
UPDATE public.notifications SET 
    failed_at = NOW() - INTERVAL '1 day',
    failure_reason = 'SMTP connection timeout'
WHERE status = 'failed';

-- =============================================
-- User Notification Preferences
-- Sample preferences showing different user preferences
-- =============================================
INSERT INTO public.user_notification_preferences (user_id, notification_type, email_enabled, in_app_enabled, sms_enabled, push_enabled, created_at) VALUES
-- User 1 (John Admin) - Prefers email and in-app for everything
(1, 'workspace_invite', true, true, false, false, NOW()),
(1, 'ticket_assigned', true, true, false, false, NOW()),
(1, 'ticket_comment', true, true, false, false, NOW()),
(1, 'ticket_status_change', true, true, false, false, NOW()),

-- User 2 (Jane Smith) - Only wants urgent notifications via email
(2, 'workspace_invite', true, false, false, false, NOW()),
(2, 'ticket_assigned', true, true, false, false, NOW()),
(2, 'ticket_summary', false, true, false, false, NOW()),
(2, 'mention', true, true, true, false, NOW()),

-- User 3 (Bob Johnson) - Prefers in-app only, no emails
(3, 'ticket_assigned', false, true, false, false, NOW()),
(3, 'ticket_comment', false, true, false, false, NOW()),
(3, 'ticket_status_change', false, true, false, false, NOW()),
(3, 'mention', false, true, false, false, NOW()),

-- User 4 (Alice Williams) - All channels enabled for critical items
(4, 'workspace_invite', true, true, true, true, NOW()),
(4, 'ticket_assigned', true, true, false, true, NOW()),
(4, 'mention', true, true, true, true, NOW()),
(4, 'password_reset', true, true, true, false, NOW()),

-- User 5 (Charlie Brown) - Mixed preferences
(5, 'ticket_assigned', true, true, false, false, NOW()),
(5, 'ticket_comment', false, true, false, false, NOW()),
(5, 'report_completed', true, false, false, false, NOW()),
(5, 'ticket_summary', true, false, false, false, NOW());

-- Note: Users with no preferences will use system defaults:
-- email_enabled=true, in_app_enabled=true, sms_enabled=false, push_enabled=false

-- =============================================
