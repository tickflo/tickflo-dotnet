using System.Web;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
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
using Tickflo.Core.Services.Workspace;
using Tickflo.Web;
using Tickflo.Web.Authentication;
using AuthenticationService = Tickflo.Core.Services.Authentication.AuthenticationService;
using IAuthenticationService = Tickflo.Core.Services.Authentication.IAuthenticationService;

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
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, Argon2idPasswordHasher>();
builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
builder.Services.AddScoped<IUserWorkspaceRepository, UserWorkspaceRepository>();
builder.Services.AddScoped<IUserWorkspaceRoleRepository, UserWorkspaceRoleRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportRunRepository, ReportRunRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketCommentRepository, TicketCommentRepository>();
builder.Services.AddScoped<ITicketStatusRepository, TicketStatusRepository>();
builder.Services.AddScoped<ITicketPriorityRepository, TicketPriorityRepository>();
builder.Services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
builder.Services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IUserNotificationPreferenceRepository, UserNotificationPreferenceRepository>();
builder.Services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IFileStorageRepository, FileStorageRepository>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IPasswordSetupService, PasswordSetupService>();
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
builder.Services.AddScoped<IUserOnboardingService, UserOnboardingService>();
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
builder.Services.AddScoped<Tickflo.Core.Services.Storage.IFileStorageService, Tickflo.Web.Services.RustFSStorageService>();
builder.Services.AddScoped<Tickflo.Core.Services.Storage.IImageStorageService, Tickflo.Web.Services.RustFSImageStorageService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IEmailSendService, EmailSendService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IAppContext, Tickflo.Web.AppContext>();

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
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseMiniProfiler();
}

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    if (response.StatusCode == 401)
    {
        var returnUrl = HttpUtility.UrlEncode(context.HttpContext.Request.Path + context.HttpContext.Request.QueryString);
        response.Redirect($"/login?returnUrl={returnUrl}");
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AppContextMiddleware>();

app.MapRazorPages();
app.MapControllers();

app.Run();

