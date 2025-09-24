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

builder.Services.AddCors(cors=>
    cors.AddDefaultPolicy(policy=>
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()
            .AllowCredentials()
        ));
builder.Services.AddControllers();

builder.Services.AddDbContextPool<PosttradeDbContext>(opt=>
    opt
        .UseNpgsql(Environment.GetEnvironmentVariable("POSTTRADE_DB"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton(
    builder.Configuration.GetSection("BotConfig:Coefficients").Get<CoefficientStorage>()??
    throw new SerializationException("Coefficients not found in  configuration"));

builder.Services.AddHttpClient<IIndexSender, IndexSender>(opt =>
{
    opt.BaseAddress = new Uri(IndexSender.BaseAddress);
    opt.DefaultRequestHeaders.TryAddWithoutValidation("APIKEY", Environment.GetEnvironmentVariable("API_KEY"));
});

builder.Services.AddScoped<ICpmRepo>();
builder.Services.Configure<BotConfig>(builder.Configuration.GetSection("BotConfig"));

builder.Services.AddQuartzHostedService();

builder.Services.AddQuartz(quartz =>
{
    var indexJobKey = JobKey.Create(nameof(IndexJob));
    quartz.AddJob<IndexJob>(indexJobKey).AddTrigger(trigger => trigger
        .ForJob(indexJobKey)
        .WithIdentity(nameof(IndexJob)+".Trigger")
        .WithSimpleSchedule(x=>x.WithIntervalInMinutes(1))
    );
    var dailyJobKey = JobKey.Create(nameof(DailyJob));
    quartz.AddJob<DailyJob>(JobKey.Create(nameof(DailyJob))).AddTrigger(trigger => trigger
        .ForJob(dailyJobKey)
        .WithIdentity(nameof(DailyJob) + ".Trigger")
        .WithSimpleSchedule(x => x.WithIntervalInHours(24)));
});

var app = builder.Build();



app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();