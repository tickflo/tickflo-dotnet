-- migrate:up

-- Update Template Type 2: Workspace Invite - New User to remove temporary password
INSERT INTO public.email_templates (template_type_id, version, subject, body, created_at, created_by) VALUES
(2, 2, 'You''re invited to {{WORKSPACE_NAME}}',
'<div style="font-family:Arial,sans-serif">
    <h2 style="color:#333">Workspace Invitation</h2>
    <p>You have been invited to the workspace ''<b>{{WORKSPACE_NAME}}</b>''.</p>
    <p>To get started, please confirm your email address and set your password:</p>
    <p><a href="{{CONFIRMATION_LINK}}" style="background-color:#007bff;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;display:inline-block">Confirm Email &amp; Set Password</a></p>
    <p>Or copy and paste this link: <br/>{{CONFIRMATION_LINK}}</p>
    <hr/>
    <p style="color:#777;font-size:12px">If you did not expect this invitation, you can safely ignore this email.</p>
</div>', NOW(), 1);

-- migrate:down

DELETE FROM public.email_templates WHERE template_type_id = 2 AND version = 2;
