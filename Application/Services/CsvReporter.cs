using System.Globalization;
using System.Text;
using Application.Models.Entities;
using Microsoft.Extensions.Options;

namespace Application.Services;

public interface ICsvReporter
{
    Task LogAsync(Report report, CancellationToken ct = default);
}

public class CsvReporter(IOptions<BotConfig> config) : ICsvReporter
{
    public async Task LogAsync(Report report, CancellationToken ct = default)
    {
        var path = Path.Combine(config.Value.LogPath, $"{report.Timestamp:yyyy-MM-dd}.csv");
        var exists = File.Exists(path);
        var stream = new FileStream(path, FileMode.Append);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        
        if (!exists) await writer.WriteLineAsync("Timestamp,Index,Cpm,WeightedAverage,Staking,Median,Day");

        await writer
            .WriteLineAsync($"{report.Timestamp},{report.Index.ToString(CultureInfo.InvariantCulture)},{report.Cpm.ToString(CultureInfo.InvariantCulture)},{report.WeightedAverage.ToString(CultureInfo.InvariantCulture)},{report.Staking.ToString(CultureInfo.InvariantCulture)},{report.Median.ToString(CultureInfo.InvariantCulture)},{report.Day.ToString(CultureInfo.InvariantCulture)}");
    }
}