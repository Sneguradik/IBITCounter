using Application.Models.Entities;
using Quartz;

namespace IBITCounter.Jobs;

public class DailyJob(CoefficientStorage coefficientStorage, ILogger<DailyJob> logger) : IJob
{
    private readonly object _lock = new ();
    public Task Execute(IJobExecutionContext context)
    {
        lock (_lock)
        {
            coefficientStorage.Day+=1;
        }
        logger.LogInformation("Day increased: {CoefficientStorageDay}", coefficientStorage.Day);
        return Task.CompletedTask;
    }
}