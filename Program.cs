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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthAPI", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
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
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"])),
        NameClaimType = ClaimTypes.Name,
        ClockSkew = TimeSpan.Zero
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
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"OnChallenge: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

// Register custom services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ProductService>();

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

app.UseHttpsRedirection();

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
