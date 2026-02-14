using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using SiPassHealth.Security;
using SiPassHealth.SiPass;
using SiPassHealth.StateEngine;
using SiPassHealth.Storage;
using SiPassHealth.Web;
using SiPassHealth.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.User.RequireUniqueEmail = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.LoginPath = "/api/auth/login";
        options.AccessDeniedPath = "/api/auth/denied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Viewer, policy => policy.RequireRole(Roles.Viewer, Roles.Admin));
    options.AddPolicy(Policies.Admin, policy => policy.RequireRole(Roles.Admin));
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

builder.Services.AddOptions<SipassOptions>()
    .Bind(builder.Configuration.GetSection("Sipass"))
    .ValidateDataAnnotations();

builder.Services.AddOptions<StateEngineOptions>()
    .Bind(builder.Configuration.GetSection("StateEngine"))
    .ValidateDataAnnotations();

builder.Services.AddHttpClient<SiPassClient>();

builder.Services.AddSingleton<StateEngine>();
builder.Services.AddSingleton<SseBroker>();
builder.Services.AddSingleton<SipassConnectionState>();

builder.Services.AddHostedService<SiPassWorker>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/api/health");

AuthEndpoints.Map(app);
OverviewEndpoints.Map(app);
SseEndpoint.Map(app);

app.MapFallbackToFile("index.html");

app.Run();
