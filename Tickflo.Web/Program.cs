using System.Web;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Admin;
using Tickflo.Core.Services.Authentication;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Contacts;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Services.Export;
using Tickflo.Core.Services.Inventory;
using Tickflo.Core.Services.Locations;
using Tickflo.Core.Services.Notifications;
using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Roles;
using Tickflo.Core.Services.Teams;
using Tickflo.Core.Services.Tickets;
using Tickflo.Core.Services.Users;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Web;
using Tickflo.Core.Services.Workspace;
using Tickflo.Web;
using Tickflo.Web.Authentication;
using Tickflo.Web.Middleware;
using Tickflo.Web.Realtime;
using Tickflo.Web.Services;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables();

var appConfig = new TickfloConfig();
builder.Configuration.Bind(appConfig);

var settingsConfig = new SettingsConfig();
builder.Configuration.GetSection("SETTINGS").Bind(settingsConfig);

var connectionString = $"Host={appConfig.PostgresHost};Port=5432;Database={appConfig.PostresDatabase};Username={appConfig.PostgresUser};Password={appConfig.PostgresPassword}";

builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton(settingsConfig);
builder.Services.AddScoped<IPasswordHasher, Argon2idPasswordHasher>();
builder.Services.AddScoped<IPasswordValidationService, PasswordValidationService>();
builder.Services.AddSignalR();
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(180);
    options.IncludeSubDomains = true;
    options.Preload = true;
});
builder.Services.AddScoped<Tickflo.Core.Services.Authentication.IAuthenticationService, Tickflo.Core.Services.Authentication.AuthenticationService>();
builder.Services.AddScoped<IPasswordSetupService, PasswordSetupService>();
builder.Services.AddScoped<IPasswordResetRequestService, PasswordResetRequestService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IWorkspaceAccessService, WorkspaceAccessService>();
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();

// Phase 1: Critical business logic services (Dashboard, Tickets)
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ITicketManagementService, TicketManagementService>();
builder.Services.AddScoped<ITicketFilterService, TicketFilterService>();
builder.Services.AddScoped<IWorkspaceDashboardViewService, WorkspaceDashboardViewService>();
builder.Services.AddScoped<IWorkspaceTicketsViewService, WorkspaceTicketsViewService>();
builder.Services.AddScoped<IWorkspaceTicketDetailsViewService, WorkspaceTicketDetailsViewService>();
builder.Services.AddScoped<IWorkspaceUsersViewService, WorkspaceUsersViewService>();
builder.Services.AddScoped<IWorkspaceReportsViewService, WorkspaceReportsViewService>();
builder.Services.AddScoped<IWorkspaceInventoryViewService, WorkspaceInventoryViewService>();
builder.Services.AddScoped<IWorkspaceTeamsViewService, WorkspaceTeamsViewService>();
builder.Services.AddScoped<IWorkspaceLocationsViewService, WorkspaceLocationsViewService>();
builder.Services.AddScoped<IWorkspaceContactsViewService, WorkspaceContactsViewService>();
builder.Services.AddScoped<IWorkspaceRolesViewService, WorkspaceRolesViewService>();
builder.Services.AddScoped<IWorkspaceLocationsEditViewService, WorkspaceLocationsEditViewService>();
builder.Services.AddScoped<IWorkspaceContactsEditViewService, WorkspaceContactsEditViewService>();
builder.Services.AddScoped<IWorkspaceInventoryEditViewService, WorkspaceInventoryEditViewService>();
builder.Services.AddScoped<IWorkspaceReportsEditViewService, WorkspaceReportsEditViewService>();
builder.Services.AddScoped<IWorkspaceRolesEditViewService, WorkspaceRolesEditViewService>();
builder.Services.AddScoped<IWorkspaceTeamsEditViewService, WorkspaceTeamsEditViewService>();
builder.Services.AddScoped<IWorkspaceSettingsViewService, WorkspaceSettingsViewService>();
builder.Services.AddScoped<IWorkspaceRolesAssignViewService, WorkspaceRolesAssignViewService>();
builder.Services.AddScoped<IWorkspaceTeamsAssignViewService, WorkspaceTeamsAssignViewService>();
builder.Services.AddScoped<IWorkspaceReportRunViewService, WorkspaceReportRunViewService>();
builder.Services.AddScoped<IWorkspaceReportRunDownloadViewService, WorkspaceReportRunDownloadViewService>();
builder.Services.AddScoped<IWorkspaceReportDeleteViewService, WorkspaceReportDeleteViewService>();
builder.Services.AddScoped<IWorkspaceFilesViewService, WorkspaceFilesViewService>();
builder.Services.AddScoped<IWorkspaceReportRunsBackfillViewService, WorkspaceReportRunsBackfillViewService>();
builder.Services.AddScoped<IWorkspaceReportRunExecuteViewService, WorkspaceReportRunExecuteViewService>();
builder.Services.AddScoped<IWorkspaceReportRunsViewService, WorkspaceReportRunsViewService>();
builder.Services.AddScoped<IWorkspaceUsersInviteViewService, WorkspaceUsersInviteViewService>();
builder.Services.AddScoped<IWorkspaceUsersManageViewService, WorkspaceUsersManageViewService>();
builder.Services.AddScoped<IWorkspaceTicketsSaveViewService, WorkspaceTicketsSaveViewService>();

// Phase 2 & 3: Domain entity services
builder.Services.AddScoped<IWorkspaceSettingsService, WorkspaceSettingsService>();
builder.Services.AddScoped<IUserInvitationService, UserInvitationService>();

// Behavior-focused services - organized by business workflow (Phase 3-5)
builder.Services.AddScoped<IContactRegistrationService, ContactRegistrationService>();
builder.Services.AddScoped<IInventoryAllocationService, InventoryAllocationService>();
builder.Services.AddScoped<IInventoryAdjustmentService, InventoryAdjustmentService>();
builder.Services.AddScoped<ILocationSetupService, LocationSetupService>();
builder.Services.AddScoped<ITicketAssignmentService, TicketAssignmentService>();
builder.Services.AddScoped<ITicketCommentService, TicketCommentService>();
builder.Services.AddScoped<ITicketClosingService, TicketClosingService>();
builder.Services.AddScoped<ITicketCreationService, TicketCreationService>();
builder.Services.AddScoped<ITicketUpdateService, TicketUpdateService>();
builder.Services.AddScoped<ITicketSearchService, TicketSearchService>();
builder.Services.AddScoped<IWorkspaceCreationService, WorkspaceCreationService>();
builder.Services.AddScoped<INotificationTriggerService, NotificationTriggerService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ITeamManagementService, TeamManagementService>();
builder.Services.AddScoped<IReportQueryService, ReportQueryService>();
builder.Services.AddScoped<IReportRunService, ReportRunService>();
builder.Services.AddScoped<IReportCommandService, ReportCommandService>();
builder.Services.AddScoped<IReportDefinitionValidator, ReportDefinitionValidator>();

// Listing services for filter/pagination/enrichment
builder.Services.AddScoped<IContactListingService, ContactListingService>();
builder.Services.AddScoped<IInventoryListingService, InventoryListingService>();
builder.Services.AddScoped<ILocationListingService, LocationListingService>();
builder.Services.AddScoped<ITeamListingService, TeamListingService>();

// RustFS file and image storage services (Web implementations)
builder.Services.AddScoped<Tickflo.Core.Services.Storage.IFileStorageService, RustFSStorageService>();
builder.Services.AddScoped<Tickflo.Core.Services.Storage.IImageStorageService, RustFSImageStorageService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IEmailSendService, EmailSendService>();
builder.Services.AddScoped<IInboundEmailHMACValidator, InboundEmailHMACValidator>();
builder.Services.AddScoped<IInboundEmailService, InboundEmailService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IAppContext, Tickflo.Web.AppContext>();
builder.Services.AddScoped<IEmailLogService, EmailLogService>();
builder.Services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();

// Temporary services (TODO: Move logic to Core)
builder.Services.AddScoped<ITempTeamService, TempTeamService>();
builder.Services.AddScoped<ITempRolePermissionService, TempRolePermissionService>();

builder.Services.AddScoped<Tickflo.Core.Jobs.IBatchEmailSendService, Tickflo.Core.Jobs.MailgunEmailSendService>();

builder.Services.AddDbContext<TickfloDbContext>(options =>
    options.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention());

builder.Services.AddRazorPages(options =>
{
    // Removed legacy '/new' route mappings; use unified edit/details routes.
});
builder.Services.AddControllers();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(appConfig.SessionTimeoutMinutes);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication("TokenAuth")
    .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>("TokenAuth", options => options.TimeProvider = TimeProvider.System);

builder.Services.AddAuthorizationBuilder()
    .AddDefaultPolicy("AuthenticationPolicy", new AuthorizationPolicyBuilder("TokenAuth")
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddMiniProfiler().AddEntityFramework();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestOriginService, RequestOriginService>();

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var config = sp.GetRequiredService<TickfloConfig>();
    var s3Config = new AmazonS3Config
    {
        ServiceURL = config.S3EndPoint,
        ForcePathStyle = true,
        AuthenticationRegion = config.S3Region,
    };
    return new AmazonS3Client(config.S3AccessKey, config.S3SecretKey, s3Config);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TickfloDbContext>();
    await dbContext.Database.MigrateAsync();

    var demoDataSeeder = scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>();
    if (!await demoDataSeeder.DemoWorkspaceExistsAsync())
    {
        await demoDataSeeder.SeedDemoDataAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseMiniProfiler();
}

app.UseStatusCodePages(context =>
{
    var response = context.HttpContext.Response;

    if (response.StatusCode == 401)
    {
        var returnUrl = HttpUtility.UrlEncode(context.HttpContext.Request.Path + context.HttpContext.Request.QueryString);
        response.Redirect($"/login?returnUrl={returnUrl}");
    }

    return Task.CompletedTask;
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<HttpExceptionMiddleware>();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AppContextMiddleware>();

app.MapRazorPages();
app.MapControllers();
app.MapHub<TicketsHub>("/hubs/tickets");

app.Run();

