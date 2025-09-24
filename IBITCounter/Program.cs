using System.Runtime.Serialization;
using Application.Models.Entities;
using Application.Services;
using IBITCounter.Jobs;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddCors(cors=>
    cors.AddDefaultPolicy(policy=>
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()
            .AllowCredentials()
        ));
builder.Services.AddDbContext<PosttradeDbContext>(opt=>
    opt
        .UseNpgsql(Environment.GetEnvironmentVariable("POSTTRADE_DB"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton(
    builder.Configuration.GetSection("BotConfig:Coefficients").Get<CoefficientStorage>()??
    throw new SerializationException("Coefficients not found in  configuration"));

builder.Services.AddScoped<IIndexSender>();
builder.Services.AddScoped<ICpmRepo>();
builder.Services.Configure<BotConfig>(builder.Configuration.GetSection("BotConfig"));

builder.Services.AddQuartzHostedService();

builder.Services.AddQuartz(quartz =>
{
    quartz.AddJob<IndexJob>(JobKey.Create(nameof(IndexJob))).AddTrigger(trigger => trigger
        .WithIdentity(nameof(IndexJob)+".Trigger")
        .WithSimpleSchedule(x=>x.WithIntervalInMinutes(1))
    );
    quartz.AddJob<DailyJob>(JobKey.Create(nameof(DailyJob))).AddTrigger(trigger => trigger
        .WithIdentity(nameof(DailyJob) + ".Trigger")
        .WithSimpleSchedule(x => x.WithIntervalInHours(24)));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();