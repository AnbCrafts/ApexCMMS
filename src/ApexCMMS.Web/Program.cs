using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ApexCMMS.Application.Interfaces;
using ApexCMMS.Domain.Entities;
using ApexCMMS.Infrastructure;
using ApexCMMS.Infrastructure.Data;
using ApexCMMS.Web.Middleware;
using ApexCMMS.Web.Security;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Infrastructure Services (EF Core, Identity, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Add Distributed Memory Cache and Session Handling
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".ApexCMMS.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// 3. Configure Hybrid Authentication (Cookies for MVC UI + JWT Bearer for REST API)
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "ApexCMMS_SuperSecretKey_ProductionGrade_2026_Minimum32CharsLong!@#$";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "ApexCMMS";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "ApexCMMS-FieldApp";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(options =>
{
    // Default authentication scheme for Razor MVC
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = ".ApexCMMS.Auth";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 4. Configure RBAC & Policy-Based Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("SuperAdmin", "OperationsManager"));
    options.AddPolicy("TechnicianOrAdmin", policy => policy.RequireRole("SuperAdmin", "OperationsManager", "FieldTechnician"));
    options.AddPolicy("AuditorOrAdmin", policy => policy.RequireRole("SuperAdmin", "SafetyAuditor"));

    // Department Isolation Policy
    options.AddPolicy("DepartmentMatch", policy => policy.Requirements.Add(new DepartmentRequirement()));
});

builder.Services.AddScoped<IAuthorizationHandler, DepartmentAuthorizationHandler>();

// 5. Anti-Forgery Token Configuration
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = ".ApexCMMS.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// 6. Controllers and Views with Auto Anti-Forgery Validation
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

// 7. Seed Database on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        await DbInitializer.SeedDatabaseAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the ApexCMMS database.");
    }
}

// 8. HTTP Pipeline Configuration
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSecurityHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Route Mapping
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
