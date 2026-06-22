const fs = require('fs');
const path = 'c:/Users/GAMZE/Desktop/eticaret/src/ECommerce.API/Program.cs';

let content = fs.readFileSync(path, 'utf8');

const badBlock = `builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
                    var cookieToken = ctx.Request.Cookies["access_token"];
                    if (!string.IsNullOrEmpty(cookieToken))
                    {
                        ctx.Token = cookieToken;
                        logger.LogDebug("🔐 JWT: Token alındı httpOnly cookie'den");
                    }
                }`;

const goodBlock = `builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;

        // ŞİFRE POLİTİKASI
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredLength = 8;

        options.SignIn.RequireConfirmedEmail = true;

        // Hesap kilitleme
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<ECommerceDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddHttpContextAccessor();

// HANGFIRE
var hangfireEnabled = builder.Configuration.GetValue<bool>("MikroApi:Jobs:HangfireEnabled", true);
if (hangfireEnabled)
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true,
            PrepareSchemaIfNecessary = true,
            SchemaName = "Hangfire"
        }));

    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = Environment.ProcessorCount * 2;
        options.Queues = new[] { "mikro-orders", "mikro-sync", "default" };
        options.ServerName = $"ECommerce-{Environment.MachineName}";
    });
}

// JWT Auth
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration[ConfigKeys.JwtIssuer],
            ValidAudience = builder.Configuration[ConfigKeys.JwtAudience],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration[ConfigKeys.JwtKey] ?? throw new InvalidOperationException("JwtKey is required"))),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var authHeader = ctx.Request.Headers["Authorization"].ToString();
                
                // ÖNCELİK 1: Authorization Header (Bu, frontend'in doğru token'ı zorlamasına izin verir, Cookie ezmez)
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Token = authHeader.Substring("Bearer ".Length).Trim();
                }
                // ÖNCELİK 2: Güvenli httpOnly Cookie (Header yoksa kullanılır)
                else if (ctx.Request.Cookies.TryGetValue("jwt", out var cookieToken))
                {
                    ctx.Token = cookieToken;
                }
                else if (ctx.Request.Cookies.TryGetValue("access_token", out var legacyCookieToken))
                {
                    ctx.Token = legacyCookieToken;
                    logger.LogDebug("🔐 JWT: Token alındı httpOnly cookie'den");
                }
                `;

content = content.replace(badBlock, goodBlock);
fs.writeFileSync(path, content, 'utf8');
console.log('Fixed Program.cs');
