using System.Runtime.Serialization;
using Application.Models.Entities;
using Application.Services;
using Application.Services.Exchanges;
using IBITCounter.Jobs;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;

static TimeZoneInfo Tz(string iana, string windows)
{
    try { return TimeZoneInfo.FindSystemTimeZoneById(iana); }
    catch { return TimeZoneInfo.FindSystemTimeZoneById(windows); }
}

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

builder.Services.AddScoped<ICpmRepo, CpmRepo>();
builder.Services.Configure<BotConfig>(builder.Configuration.GetSection("BotConfig"));

builder.Services.AddQuartzHostedService();

builder.Services.AddQuartz(quartz =>
{
    var tzMoscow = Tz("Europe/Moscow", "Russian Standard Time");
    var tzNewYork = Tz("America/New_York", "Eastern Standard Time");

// INDEX: каждую минуту с 08:00 до 23:59 МСК (будни)
    var indexJobKey = JobKey.Create(nameof(IndexJob));
    quartz.AddJob<IndexJob>(indexJobKey)
        .AddTrigger(t => t
            .ForJob(indexJobKey)
            .WithIdentity($"{nameof(IndexJob)}.Trigger")
            .WithSchedule(
                CronScheduleBuilder
                    // sec min hour dayOfMonth month dayOfWeek
                    .CronSchedule("0 0/1 8-23 ? * MON-FRI")
                    .InTimeZone(tzMoscow)
            )
        );

// DAILY: раз в сутки на старте торгов США (09:30 по Нью-Йорку, будни)
    var dailyJobKey = JobKey.Create(nameof(DailyJob));
    quartz.AddJob<DailyJob>(dailyJobKey)
        .AddTrigger(t => t
            .ForJob(dailyJobKey)
            .WithIdentity($"{nameof(DailyJob)}.Trigger")
            .WithSchedule(
                CronScheduleBuilder
                    .CronSchedule("0 30 9 ? * MON-FRI")
                    .InTimeZone(tzNewYork)
            )
        );
});

builder.Services.AddScoped<ICsvReporter, CsvReporter>();

#region CryptoExchanges

builder.Services.AddHttpClient<IExchangeRepo, BinanceRepo>(BinanceRepo.Name,opt =>
{
    opt.BaseAddress = new Uri(BinanceRepo.BaseAddress, UriKind.Absolute);
    opt.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
    opt.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
});

builder.Services.AddHttpClient<IExchangeRepo, ByBitRepo>(ByBitRepo.Name,opt =>
{
    opt.BaseAddress = new Uri(ByBitRepo.BaseAddress, UriKind.Absolute);
    opt.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
    opt.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
});


builder.Services.AddHttpClient<IExchangeRepo, BitGetRepo>(BitGetRepo.Name,opt =>
{
    opt.BaseAddress = new Uri(BitGetRepo.BaseAddress, UriKind.Absolute);
    opt.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
    opt.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
});

builder.Services.AddKeyedTransient<IExchangeRepo>(BinanceRepo.Name, (sp, _) =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(BinanceRepo.Name);
    return ActivatorUtilities.CreateInstance<BinanceRepo>(sp, http);
});
builder.Services.AddKeyedTransient<IExchangeRepo>(ByBitRepo.Name, (sp, _) =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(ByBitRepo.Name);
    return ActivatorUtilities.CreateInstance<ByBitRepo>(sp, http);
});
builder.Services.AddKeyedTransient<IExchangeRepo>(BitGetRepo.Name, (sp, _) =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(BitGetRepo.Name);
    return ActivatorUtilities.CreateInstance<BitGetRepo>(sp, http);
});

#endregion

builder.Services.AddSerilog();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();