using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Services;
using Tickflo.Core.Services.Auth;

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
builder.Services.AddScoped<IPasswordHasher, Argon2idPasswordHasher>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddDbContext<TickfloDbContext>(options =>
    options.UseNpgsql(connectionString!)
        .UseSnakeCaseNamingConvention());

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
