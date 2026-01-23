-- migrate:up

-- Template Type 10: Workspace Invite - Existing User
INSERT INTO public.email_templates (template_type_id, version, subject, body, created_at, created_by) VALUES
(10, 1, 'You''re invited to {{WORKSPACE_NAME}}',
'<div style="font-family:Arial,sans-serif">
    <h2 style="color:#333">Workspace Invitation</h2>
    <p>Hello {{USER_NAME}},</p>
    <p>You have been invited to join the workspace ''<b>{{WORKSPACE_NAME}}</b>''.</p>
    <p>Click the link below to accept the invitation:</p>
    <p><a href="{{ACCEPT_LINK}}" style="background-color:#007bff;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;display:inline-block">Accept Invitation</a></p>
    <p>Or copy and paste this link: <br/>{{ACCEPT_LINK}}</p>
    <hr/>
    <p style="color:#777;font-size:12px">If you did not expect this invitation, you can safely ignore this email.</p>
</div>', NOW(), 1);

-- migrate:down

DELETE FROM public.email_templates WHERE template_type_id = 10 AND version = 1;
