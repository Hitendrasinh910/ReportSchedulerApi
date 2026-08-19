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

// Run job every minute
RecurringJob.AddOrUpdate<ReportSchedulerJob>(
    "execute-due-report-schedules",
    job => job.ExecuteDueSchedulesAsync(),
    Cron.Minutely);

app.Run();
