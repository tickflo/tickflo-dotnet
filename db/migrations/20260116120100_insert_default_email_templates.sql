-- migrate:up

-- Delete any existing templates that might have been created from seed data
-- The old seed data templates had workspace_id which has now been removed
DELETE FROM public.email_templates;

-- Insert default email templates (version 1)
-- These templates are global (no workspace_id) and immutable
-- Changes should create new versions instead of updating

-- Template Type 1: Email Confirmation Thank You (shown on page after confirmation)
INSERT INTO public.email_templates (template_type_id, version, subject, body, created_at) VALUES
(1, 1, 'Email Confirmed', 
'<div class="min-h-[60vh] flex items-center justify-center">
    <div class="card w-full max-w-lg bg-base-100 shadow-xl border border-base-200/60 rounded-3xl">
        <div class="card-body space-y-4 text-center">
            <div class="inline-flex items-center justify-center w-14 h-14 rounded-full bg-success/10 text-success">
                <i class="fa fa-check text-2xl"></i>
            </div>
            <h1 class="text-2xl font-bold">Thank you for confirming</h1>
            <p class="text-base-content/70">Your email address has been confirmed. You can now continue to sign in and use your workspace.</p>
            <div class="pt-2 flex items-center justify-center gap-3">
                {{NAVIGATION_BUTTONS}}
            </div>
        </div>
    </div>
</div>', NOW());

-- Template Type 2: Workspace Invite - New User
INSERT INTO public.email_templates (template_type_id, version, subject, body, created_at) VALUES
(2, 1, 'You''re invited to {{WORKSPACE_NAME}}',
'<div style="font-family:Arial,sans-serif">
    <h2 style="color:#333">Workspace Invitation</h2>
    <p>You have been invited to the workspace ''<b>{{WORKSPACE_NAME}}</b>''.</p>
    <p>Temporary password: <code style="font-size:1.1em">{{TEMPORARY_PASSWORD}}</code></p>
    <p>Please confirm your email: <a href="{{CONFIRMATION_LINK}}">Confirm Email</a></p>
    <p>Then accept the invite: <a href="{{ACCEPT_LINK}}">Accept Invite</a></p>
    <p>Or set your password now: <a href="{{SET_PASSWORD_LINK}}">Set Password</a></p>
    <hr/><p style="color:#777">If you did not expect this email, you can ignore it.</p>
</div>', NOW());

-- Template Type 3: Email Confirmation Request
INSERT INTO public.email_templates (template_type_id, version, subject, body, created_at) VALUES
(3, 1, 'Confirm Your Email Address',
'<div style="font-family:Arial,sans-serif">
    <h2 style="color:#333">Email Confirmation</h2>
    <p>Hello {{USER_NAME}},</p>
    <p>Please confirm your email address by clicking the link below:</p>
    <p><a href="{{CONFIRMATION_LINK}}" style="background-color:#007bff;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;display:inline-block">Confirm Email</a></p>
    <p>Or copy and paste this link: <br/>{{CONFIRMATION_LINK}}</p>
    <hr/>
    <p style="color:#777;font-size:12px">If you did not request this email, you can ignore it.</p>
</div>', NOW());

-- Template Type 4: Workspace Invite Resend
INSERT INTO public.email_templates (template_type_id, version, subject, body, created_at) VALUES
(4, 1, 'Your invite to {{WORKSPACE_NAME}}',
'<p>Hello,</p>
<p>Here is your email confirmation link for workspace ''{{WORKSPACE_NAME}}''.</p>
<p><a href="{{CONFIRMATION_LINK}}">Confirm your email</a></p>
<p>Use your original temporary password to sign in.</p>', NOW());

-- Template Type 5: Signup Welcome
INSERT INTO public.email_templates (template_type_id, version, subject, body, created_at) VALUES
(5, 1, 'Welcome to Tickflo!',
'<div style="font-family:Arial,sans-serif">
    <h2 style="color:#333">Welcome to Tickflo!</h2>
    <p>Hello {{USER_NAME}},</p>
    <p>Thank you for signing up! Your workspace ''<b>{{WORKSPACE_NAME}}</b>'' has been created.</p>
    <p>Please confirm your email address to get started:</p>
    <p><a href="{{CONFIRMATION_LINK}}" style="background-color:#007bff;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;display:inline-block">Confirm Email</a></p>
    <p>Or copy and paste this link: <br/>{{CONFIRMATION_LINK}}</p>
    <hr/>
    <p style="color:#777;font-size:12px">If you did not create this account, please ignore this email.</p>
</div>', NOW());

-- Template Type 6: Forgot Password
INSERT INTO public.email_templates (template_type_id, version, subject, body, created_at) VALUES
(6, 1, 'Reset Your Password',
'<div style="font-family:Arial,sans-serif">
    <h2 style="color:#333">Password Reset Request</h2>
    <p>Hello {{USER_NAME}},</p>
    <p>We received a request to reset your password. Click the link below to reset it:</p>
    <p><a href="{{RESET_LINK}}" style="background-color:#007bff;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;display:inline-block">Reset Password</a></p>
    <p>Or copy and paste this link: <br/>{{RESET_LINK}}</p>
    <p>This link will expire in {{EXPIRY_TIME}}.</p>
    <hr/>
    <p style="color:#777;font-size:12px">If you did not request a password reset, please ignore this email or contact support if you have concerns.</p>
</div>', NOW());

-- Template Type 7: Confirm New Email (for email change requests)
INSERT INTO public.email_templates (template_type_id, version, subject, body, created_at) VALUES
(7, 1, 'Confirm Your New Email Address',
'<div style="font-family:Arial,sans-serif">
    <h2 style="color:#333">Email Change Request</h2>
    <p>Hello {{USER_NAME}},</p>
    <p>You requested to change your email address to this one. Please confirm this new email address:</p>
    <p><a href="{{CONFIRMATION_LINK}}" style="background-color:#007bff;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;display:inline-block">Confirm New Email</a></p>
    <p>Or copy and paste this link: <br/>{{CONFIRMATION_LINK}}</p>
    <hr/>
    <p style="color:#777;font-size:12px">If you did not request this change, please ignore this email.</p>
</div>', NOW());

-- Template Type 8: Revert Email Change (notification to old email)
INSERT INTO public.email_templates (template_type_id, version, subject, body, created_at) VALUES
(8, 1, 'Email Address Change Notice',
'<div style="font-family:Arial,sans-serif">
    <h2 style="color:#333">Email Address Change</h2>
    <p>Hello {{USER_NAME}},</p>
    <p>Your email address is being changed to: <b>{{NEW_EMAIL}}</b></p>
    <p>If you did not request this change, you can revert it by clicking the link below:</p>
    <p><a href="{{REVERT_LINK}}" style="background-color:#dc3545;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;display:inline-block">Cancel Email Change</a></p>
    <p>Or copy and paste this link: <br/>{{REVERT_LINK}}</p>
    <p>This link will expire in {{EXPIRY_TIME}}.</p>
    <hr/>
    <p style="color:#777;font-size:12px">If you initiated this change, you can ignore this email.</p>
</div>', NOW());

-- Template Type 9: Workspace Member Removal
INSERT INTO public.email_templates (template_type_id, version, subject, body, created_at) VALUES
(9, 1, 'Removed from {{WORKSPACE_NAME}}',
'<div style="font-family:Arial,sans-serif">
    <h2 style="color:#333">Workspace Access Removed</h2>
    <p>Hello {{USER_NAME}},</p>
    <p>You have been removed from the workspace ''<b>{{WORKSPACE_NAME}}</b>''.</p>
    <p>You no longer have access to this workspace and its resources.</p>
    <p>If you believe this was done in error, please contact the workspace administrator.</p>
    <hr/>
    <p style="color:#777;font-size:12px">This is an automated notification.</p>
</div>', NOW());

-- migrate:down

-- Remove all default templates
DELETE FROM public.email_templates WHERE version = 1 AND template_type_id IN (1, 2, 3, 4, 5, 6, 7, 8, 9);
