-- migrate:up
DELETE FROM
    public.emails;

DELETE FROM
    public.email_templates;

INSERT INTO
    public.email_templates (template_type_id, version, subject, body)
VALUES
    (
        1,
        1,
        'Welcome to Tickflo! Confirm Your Email',
        'Hello,

Thank you for signing up! Please confirm your email address by clicking the link below:

{{confirmation_link}}

If you did not sign up, you can ignore this email.

Best regards,
Tickflo Team'
    ),
    (
        2,
        1,
        'Reset Your Password',
        'Hello,

We received a request to reset your password. Click the link below to set a new password:

{{reset_link}}

If you did not request this, you can ignore this email.

Best regards,
Tickflo Team'
    ),
    (
        3,
        1,
        'Confirm your new email address',
        'Hello,

We received a request to change your email address. Please confirm this change by clicking the link below:

{{confirmation_link}}

If you did not request this, please ignore this email.

Best regards,
Tickflo Team'
    ),
    (
        4,
        1,
        'Your email address was changed',
        'Hello,

Your Tickflo account email was changed to {{new_email}}. If you made this change, no further action is needed.

If you did NOT request this change, you have until {{expires_at}} to undo this change by clicking the link below:

{{revert_link}}

After this period, you will need to contact support.

Best regards,
Tickflo Team'
    ),
    (
        5,
        1,
        'You’re Invited! Join Our Workspace',
        'Hello {{name}},

You’ve been invited to join {{workspace_name}}. Simply login and click accept to join {{workspace_name}}:

{{login_link}}

If you weren’t expecting this invitation, you can ignore this email.

Best regards,
Tickflo Team'
    ),
    (
        6,
        1,
        'You’re Invited! Join Our Workspace',
        'Hello {{name}},

You’ve been invited to join {{workspace_name}}. Click the link below to create your account and get started:

{{signup_link}}

If you weren’t expecting this invitation, you can ignore this email.

Best regards,
Tickflo Team'
    ),
    (
        7,
        1,
        'Your access to {{workspace_name}} has been removed',
        'Hello {{name}},

You’ve been removed from {{workspace_name}}.

Contact your administrator if you belive this is a mistake.

Best regards,
Tickflo Team'
    );

-- migrate:down
