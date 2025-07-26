using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Auth;
using AuthenticationService = Tickflo.Core.Services.AuthenticationService;
using IAuthenticationService = Tickflo.Core.Services.IAuthenticationService;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables();

var appConfig = new TickfloConfig();
builder.Configuration.Bind(appConfig);

var connectionString = $"Host={appConfig.POSTGRES_HOST};Port=5432;Database={appConfig.POSTGRES_DB};Username={appConfig.POSTGRES_USER};Password={appConfig.POSTGRES_PASSWORD}";

// Add services to the container.
builder.Services.AddSingleton(appConfig);
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddScoped<IPasswordHasher, Argon2idPasswordHasher>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddDbContext<TickfloDbContext>(options =>
    options.UseNpgsql(connectionString!)
        .UseSnakeCaseNamingConvention());

builder.Services.AddRazorPages();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(appConfig.SESSION_TIMEOUT_MINUTES);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication("TokenAuth")
    .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>("TokenAuth", options =>
    {
        options.TimeProvider = TimeProvider.System;
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder("TokenAuth")
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddMiniProfiler().AddEntityFramework();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.UseStatusCodePages(context =>
{
    if (context.HttpContext.Response.StatusCode == 401)
    {
        var path = context.HttpContext.Request.Path;
        context.HttpContext.Response.Redirect($"/login{(path != "/" ? $"?returnUrl={HttpUtility.UrlEncode(path)}" : "")}");
    }
    return Task.CompletedTask;
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
