using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ReportSchedulerApi.Helpers;
using ReportSchedulerApi.Repositories.Interfaces;
using ReportSchedulerApi.Repositories.Notification;
using ReportSchedulerApi.Repositories.Services;
using ReportSchedulerApi.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IDapperHelper, DapperHelper>();
builder.Services.AddScoped<IReportScheduleRepository, ReportScheduleRepository>();
builder.Services.AddScoped<ISchedulerLookupRepository, SchedulerLookupRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddHttpClient<INotificationApiService, NotificationApiService>();

builder.Services.AddScoped<IScheduleExecutorRepository, ScheduleExecutorRepository>();

//builder.Services.AddHostedService<ReportScheduleWorker>();

// Hangfire job class
builder.Services.AddScoped<ReportSchedulerJob>();

// Liveness record + the service that registers the recurring sweep and
// keeps a heartbeat in the log.
builder.Services.AddSingleton<SchedulerHealthState>();
builder.Services.AddHostedService<SchedulerBootstrapService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReportSchedulerUi", policy =>
    {
        policy.AllowAnyOrigin()
            //WithOrigins("http://localhost:4201")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey!)
            ),

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Hangfire uses SAME database
builder.Services.AddHangfire(config =>
{
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            builder.Configuration.GetConnectionString("ReportSchedulerDb"),
            new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(15),
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            });
});

// Hangfire server starts inside API
builder.Services.AddHangfireServer(options =>
{
    options.ServerName = "ReportSchedulerApi";
    options.WorkerCount = 1;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
//app.UseCors("AllowAnyOrigin");
app.UseCors("ReportSchedulerUi");


app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard
app.UseHangfireDashboard("/hangfire");

app.MapControllers();

// Liveness probe for the external watchdog. Deliberately anonymous and
// cheap: the watchdog calls it every few minutes both to confirm the
// scheduler is still dispatching and to keep the IIS worker process warm.
app.MapGet("/health/scheduler", (SchedulerHealthState health, IConfiguration config) =>
{
    var now = DateTime.UtcNow;
    var lastRun = health.LastJobRunUtc;

    // The sweep runs every minute; allow generous slack before calling it
    // stalled so a single slow run does not trip the watchdog.
    var stalledAfter = TimeSpan.FromMinutes(
        config.GetValue<int?>("SchedulerWorker:StalledAfterMinutes") ?? 10);

    var startupGrace = health.ProcessStartedUtc.AddMinutes(3) > now;

    var healthy = health.RecurringJobRegistered
                  && (startupGrace || (lastRun.HasValue && now - lastRun.Value < stalledAfter));

    var payload = new
    {
        status = healthy ? "Healthy" : "Stalled",
        processStartedUtc = health.ProcessStartedUtc,
        recurringJobRegistered = health.RecurringJobRegistered,
        lastJobRunUtc = lastRun,
        lastHeartbeatUtc = health.LastHeartbeatUtc,
        secondsSinceLastJobRun = lastRun.HasValue
            ? (int?)(now - lastRun.Value).TotalSeconds
            : null,
        serverTimeUtc = now
    };

    return healthy ? Results.Ok(payload) : Results.Json(payload, statusCode: 503);
}).AllowAnonymous();

// NOTE: the recurring job is registered by SchedulerBootstrapService rather
// than here. Registering inline meant an unreachable SQL Server at startup
// took the whole application down instead of retrying.

app.Run();
