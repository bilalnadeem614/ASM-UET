using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using ASM_UET.Models;
using ASM_UET.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// HttpClient Factory
builder.Services.AddHttpClient();

// DbContext - Improved configuration with better error handling
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<ASM>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Add connection resilience for production deployments
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
    
    // Enable sensitive data logging in development only
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IStudentService, StudentService>();

// JWT Configuration
var jwt = builder.Configuration.GetSection("Jwt");
var key = jwt.GetValue<string>("Key");
var issuer = jwt.GetValue<string>("Issuer");
var audience = jwt.GetValue<string>("Audience");

// Authentication Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)),
        ClockSkew = TimeSpan.Zero
    };

    // Read JWT from cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["ASM_TOKEN"];
            System.Diagnostics.Debug.WriteLine($"OnMessageReceived - Path: {context.Request.Path}");
            System.Diagnostics.Debug.WriteLine($"OnMessageReceived - Has cookie: {!string.IsNullOrEmpty(token)}");
            System.Diagnostics.Debug.WriteLine($"OnMessageReceived - Token length: {token?.Length ?? 0}");
            
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
                System.Diagnostics.Debug.WriteLine("? Token set from cookie");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("? No token in cookie");
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            System.Diagnostics.Debug.WriteLine($"? Authentication FAILED: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var claims = string.Join(", ", context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}") ?? new List<string>());
            System.Diagnostics.Debug.WriteLine($"? Token VALIDATED - Claims: {claims}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            System.Diagnostics.Debug.WriteLine($"?? Challenge triggered - Error: {context.Error}, ErrorDescription: {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=LoginPage}/{id?}");

app.Run();
