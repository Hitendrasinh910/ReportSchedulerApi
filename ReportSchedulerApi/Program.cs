using ReportSchedulerApi.Helpers;
using ReportSchedulerApi.Repositories.Interfaces;
using ReportSchedulerApi.Repositories.Notification;
using ReportSchedulerApi.Repositories.Services;
using ReportSchedulerApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IDapperHelper, DapperHelper>();
builder.Services.AddScoped<IReportScheduleRepo, ReportScheduleRepository>();
builder.Services.AddScoped<ISchedulerLookupRepository, SchedulerLookupRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddHttpClient<INotificationApiService, NotificationApiService>();

builder.Services.AddScoped<IScheduleExecutorService, ScheduleExecutorService>();

builder.Services.AddHostedService<ReportScheduleWorker>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyOrigin", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAnyOrigin");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
