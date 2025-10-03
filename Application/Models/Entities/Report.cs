using System.ComponentModel.DataAnnotations;

namespace Application.Models.Entities;

public class Report
{
    public DateTime Timestamp { get; set; }
    public double Index { get; set; }
    public double Cpm { get; set; }
    public double WeightedAverage { get; set; }
    public double Staking { get; set; }
    public double Median { get; set; }
    public int Day { get; set; }
}