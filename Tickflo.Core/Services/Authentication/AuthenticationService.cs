namespace Tickflo.Core.Services.Authentication;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Exceptions;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Services.Workspace;

public partial class AuthenticationService(
    TickfloDbContext db,
    IPasswordHasher passwordHasher,
    IEmailSendService emailSendService,
    TickfloConfig config,
    IWorkspaceCreationService workspaceCreationService
    ) : IAuthenticationService
{
    private readonly TickfloDbContext db = db;
    private readonly IPasswordHasher passwordHasher = passwordHasher;
    private readonly IEmailSendService emailSendService = emailSendService;
    private readonly IWorkspaceCreationService workspaceCreationService = workspaceCreationService;
    private readonly TickfloConfig config = config;

    public async Task<AuthenticationResult> AuthenticateAsync(string email, string password)
    {
        var user = await this.db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            this.PreventTimingAttack();
            throw new UnauthorizedException("Invalid credentials");
        }

        if (user.PasswordHash == null)
        {
            this.PreventTimingAttack();
            throw new UnauthorizedException("No password set for this user");
        }

        if (!this.passwordHasher.Verify($"{email}{password}", user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid credentials");
        }

        var token = new Token(user.Id, this.config.SessionTimeoutMinutes * 60);
        await this.db.Tokens.AddAsync(token);
        await this.db.SaveChangesAsync();

        return new AuthenticationResult
        {
            UserId = user.Id,
            Token = token.Value,
        };
    }

    public async Task<AuthenticationResult> SignupAsync(string name, string email, string recoveryEmail, string workspaceName, string password)
    {
        if (await this.db.Users.AnyAsync(user => user.Email == email))
        {
            throw new BadRequestException("User with this email already exists");
        }

        await using var transaction = await this.db.Database.BeginTransactionAsync();

        try
        {

            var user = new User(name, email, recoveryEmail, this.passwordHasher.Hash($"{email}{password}"));
            this.db.Users.Add(user);
            await this.db.SaveChangesAsync();

            await this.SendEmailConfirmationAsync(user);
            await this.workspaceCreationService.CreateWorkspaceAsync(workspaceName, user.Id);

            var token = new Token(user.Id, this.config.SessionTimeoutMinutes * 60);
            await this.db.Tokens.AddAsync(token);
            await this.db.SaveChangesAsync();

            await transaction.CommitAsync();

            System.Diagnostics.Debug.WriteLine($"DbContext Hash: ${this.db.GetHashCode()}");

            return new AuthenticationResult
            {
                UserId = user.Id,
                Token = token.Value,
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AuthenticationResult> SignupInviteeAsync(string name, string email, string recoveryEmail, string password)
    {
        var user = await this.db.Users.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new NotFoundException("User not found");

        var pendingInvites = await this.db.UserWorkspaces.Where(uw => uw.UserId == user.Id && !uw.Accepted).ToListAsync();

        if (pendingInvites.Count == 0)
        {
            throw new BadRequestException("User has no workspace invitations");
        }

        foreach (var invite in pendingInvites)
        {
            invite.Accepted = true;
            invite.UpdatedAt = DateTime.UtcNow;
            invite.UpdatedBy = user.Id;
            this.db.UserWorkspaces.Update(invite);
        }

        user.Name = name;
        user.RecoveryEmail = recoveryEmail;
        user.PasswordHash = this.passwordHasher.Hash($"{email}{password}");
        this.db.Users.Update(user);

        await this.SendEmailConfirmationAsync(user);

        var token = new Token(user.Id, this.config.SessionTimeoutMinutes * 60);
        await this.db.Tokens.AddAsync(token);
        await this.db.SaveChangesAsync();

        return new AuthenticationResult
        {
            UserId = user.Id,
            Token = token.Value,
        };
    }

    public async Task ResendEmailConfirmationAsync(int userId)
    {
        var user = await this.db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User not found");

        if (user.EmailConfirmed)
        {
            throw new BadRequestException("Email is already confirmed");
        }

        await this.SendEmailConfirmationAsync(user);
        await this.db.SaveChangesAsync();
    }

    private async Task SendEmailConfirmationAsync(User user) => await this.emailSendService.AddToQueueAsync(user.Email,
            EmailTemplateType.Signup,
            new Dictionary<string, string>
            {
                { "confirmation_link", $"{this.config.BaseUrl}/confirm-email?email={Uri.EscapeDataString(user.Email)}&code={user.EmailConfirmationCode}" }
            });

    private void PreventTimingAttack() => this.passwordHasher.Verify("password", "$argon2id$v=19$m=16,t=2,p=1$NlJRdlBSbDZhRVUzdTFYcQ$FbtOcbMs2IMTMHFE8WcSiQ");
}
