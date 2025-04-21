using AspNetCore.Identity.MongoDbCore.Extensions;
using AspNetCore.Identity.MongoDbCore.Infrastructure;
using AuthApi.Models;
using AuthApi.Services;
using AuthApi.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using MongoDbSettings = AuthApi.Settings.MongoDbSettings;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add OpenAPI/Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { 
        Title = "Auth API", 
        Version = "v1",
        Description = "A secure authentication API with JWT token support",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@authapi.com"
        }
    });
});

// Configure MongoDB settings
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection(nameof(MongoDbSettings)));

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(nameof(JwtSettings)));

// Configure MongoDB Identity
var mongoDbIdentityConfig = new MongoDbIdentityConfiguration
{
    MongoDbSettings = new AspNetCore.Identity.MongoDbCore.Infrastructure.MongoDbSettings
    {
        ConnectionString = builder.Configuration.GetValue<string>("MongoDbSettings:ConnectionString"),
        DatabaseName = builder.Configuration.GetValue<string>("MongoDbSettings:DatabaseName")
    },
    IdentityOptionsAction = options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
    }
};

builder.Services.ConfigureMongoDbIdentity<ApplicationUser, ApplicationRole, Guid>(mongoDbIdentityConfig)
    .AddUserManager<UserManager<ApplicationUser>>()
    .AddSignInManager<SignInManager<ApplicationUser>>()
    .AddRoleManager<RoleManager<ApplicationRole>>()
    .AddDefaultTokenProviders();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = false;
    options.RequireHttpsMetadata = false;
    
    // Disable HTTPS validation for development
    options.BackchannelHttpHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "AuthApi",
        ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "AuthApiClient",
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"] ?? "ThisIsTheVerySecureKeyThatShouldBeStoredInASecureVault")),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.FromMinutes(5)
    };
    
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token successfully validated");
            
            // Check if the token exists in UserSessionService
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var userSessionService = context.HttpContext.RequestServices.GetRequiredService<UserSessionService>();
                if (!userSessionService.IsUserLoggedIn(userId))
                {
                    // Token is no longer valid, user has logged out or another user is logged in
                    context.Fail("Token is no longer valid (user logged out or session expired)");
                }
            }
            
            return Task.CompletedTask;
        }
    };
});

// Register custom services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddSingleton<UserSessionService>();

// 2. Build the app
var app = builder.Build();

// 3. Test MongoDB connection
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        Console.WriteLine("Testing MongoDB connection...");
        var connectionString = builder.Configuration["MongoDbSettings:ConnectionString"];
        var databaseName = builder.Configuration["MongoDbSettings:DatabaseName"];

        Console.WriteLine($"Connection string: {connectionString}");
        Console.WriteLine($"Database name: {databaseName}");

        if (!string.IsNullOrEmpty(connectionString))
        {
            var client = new MongoDB.Driver.MongoClient(connectionString);
            var database = client.GetDatabase(databaseName ?? "test");
            Console.WriteLine("MongoDB connection successful!");
        }
        else
        {
            Console.WriteLine("ERROR: Connection string is empty!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"MongoDB connection error: {ex.Message}");
    }
}

// 4. Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 5. Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRoles(roleManager);
        await SeedAdminUser(userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

// Seed Methods
async Task SeedRoles(RoleManager<ApplicationRole> roleManager)
{
    if (roleManager.Roles.Any())
        return;

    await roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
    await roleManager.CreateAsync(new ApplicationRole { Name = "User" });
}

async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
{
    var adminUser = await userManager.FindByEmailAsync("admin@example.com");
    if (adminUser != null)
        return;

    var admin = new ApplicationUser
    {
        UserName = "admin@example.com",
        Email = "admin@example.com",
        FirstName = "Admin",
        LastName = "User",
        EmailConfirmed = true
    };

    var result = await userManager.CreateAsync(admin, "Admin123!");
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}
