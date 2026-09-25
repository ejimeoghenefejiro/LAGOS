using System.Text;
using LGRRS.Api.Auth;
using LGRRS.Infrastructure.Persistence;
using LGRRS.Infrastructure.Providers;
using LGRRS.Infrastructure.Seed;
using LGRRS.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port)) builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
if (!builder.Environment.IsDevelopment())
{
    var signingKey = builder.Configuration["Jwt:SigningKey"];
    if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 48 || signingKey.Contains("REPLACE_"))
        throw new InvalidOperationException("Configure a unique Jwt__SigningKey of at least 48 characters.");
    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Default")))
        throw new InvalidOperationException("Configure ConnectionStrings__Default for hosted SQL Server.");
    if (string.IsNullOrWhiteSpace(builder.Configuration["DataProtection:KeysPath"]))
        throw new InvalidOperationException("Configure DataProtection__KeysPath on a persistent volume.");
    if (!Uri.TryCreate(builder.Configuration["PublicWebUrl"], UriKind.Absolute, out var publicUrl) || publicUrl.Scheme != "https")
        throw new InvalidOperationException("Configure PublicWebUrl with the public HTTPS website address.");
    if (builder.Configuration.GetValue<bool>("Demo:SeedData"))
        throw new InvalidOperationException("Synthetic data and default demo credentials cannot be seeded outside Development.");
    if (!builder.Configuration.GetValue<bool>("Demo:AllowConsoleOtp"))
        throw new InvalidOperationException("Real SMS delivery is not implemented. For a restricted hosted demo only, explicitly set Demo__AllowConsoleOtp=true.");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LGRRS API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditActorInterceptor>();
builder.Services.AddDbContext<LgrrsDbContext>((services, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
        .AddInterceptors(services.GetRequiredService<AuditActorInterceptor>()));

var protection = builder.Services.AddDataProtection();
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    Directory.CreateDirectory(keysPath);
    protection.PersistKeysToFileSystem(new DirectoryInfo(keysPath)).SetApplicationName("LGRRS");
}
builder.Services.AddSingleton<IDataProtectionCodec, DataProtectionCodec>();
builder.Services.AddSingleton<IBusinessVerificationProvider, DemoBusinessVerificationProvider>();
builder.Services.AddScoped<IOtpProvider, DatabaseOtpProvider>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ??
            (builder.Environment.IsDevelopment() ? ["http://localhost:5173", "http://localhost:5175", "http://127.0.0.1:5173"] : [builder.Configuration["PublicWebUrl"]!]))
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LgrrsDbContext>();
    db.Database.Migrate();
    var codec = scope.ServiceProvider.GetRequiredService<IDataProtectionCodec>();
    if (app.Configuration.GetValue<bool>("Demo:SeedData")) DemoDataSeeder.Seed(db, codec);
    if (!app.Environment.IsDevelopment() && !db.AppUsers.Any())
    {
        var email = app.Configuration["BootstrapAdmin:Email"];
        var password = app.Configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 16)
            throw new InvalidOperationException("A fresh database requires BootstrapAdmin__Email and BootstrapAdmin__Password (16+ characters).");
        db.AppUsers.Add(new LGRRS.Domain.Entities.AppUser {
            Email = email, DisplayName = "LGA Administrator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Role = LGRRS.Domain.Enums.AppRole.LgaAdmin
        });
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Railway terminates HTTPS at its edge; the container listens on its assigned HTTP port.
if (app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", async (LgrrsDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503));
app.Map("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
