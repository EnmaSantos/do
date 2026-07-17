using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpsFlow.Api.Endpoints;
using OpsFlow.Api.Security;
using OpsFlow.Application.Security;
using OpsFlow.Infrastructure.Auditing;
using OpsFlow.Infrastructure.Identity;
using OpsFlow.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

var connectionString = builder.Configuration.GetConnectionString("OpsFlow")
    ?? throw new InvalidOperationException("Connection string 'OpsFlow' is required.");
builder.Services.AddDbContext<OpsFlowDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddIdentityCore<OpsUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<OpsFlowDbContext>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("InventoryWrite", policy => policy.RequireRole(Roles.Administrator, Roles.InventoryManager))
    .AddPolicy("SupportWrite", policy => policy.RequireRole(Roles.Administrator, Roles.SupportEngineer))
    .AddPolicy("KnowledgeWrite", policy => policy.RequireRole(Roles.Administrator, Roles.SupportEngineer))
    .AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Administrator));

builder.Services.AddCors(options => options.AddPolicy("Client", policy => policy
    .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader().AllowAnyMethod()));
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuditRecorder>();

var app = builder.Build();

app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var status = exception is InvalidOperationException ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
    await Results.Problem(
        statusCode: status,
        title: status == 400 ? "The operation could not be completed" : "An unexpected error occurred",
        detail: app.Environment.IsDevelopment() || status == 400 ? exception?.Message : null,
        extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier })
        .ExecuteAsync(context);
}));
app.UseSerilogRequestLogging();
app.UseCors("Client");
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "OpsFlow API v1");
    options.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapGet("/health", async (OpsFlowDbContext db, CancellationToken cancellationToken) =>
{
    var connected = await db.Database.CanConnectAsync(cancellationToken);
    return connected
        ? Results.Ok(new { status = "healthy", database = "connected", timestamp = DateTimeOffset.UtcNow })
        : Results.Problem(statusCode: 503, title: "Database unavailable");
}).WithTags("System").AllowAnonymous();

app.MapAuthEndpoints();
app.MapAssetEndpoints();
app.MapIncidentEndpoints();
app.MapServiceEndpoints();
app.MapKnowledgeEndpoints();
app.MapDashboardEndpoints();
app.MapAuditEndpoints();

if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<UserManager<OpsUser>>(),
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>());
}

await app.RunAsync();

public partial class Program;
