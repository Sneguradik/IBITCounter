using Application.Models.Entities;

namespace Application.Services;

public static class Counter
{
    public static double Count(double price, CoefficientStorage coefficients) =>
        price / coefficients.Median * (1 + coefficients.StakingFee * coefficients.Day/365);
    
    public static double CountCpm(IEnumerable<CurrentPriceOfMarket> cpm, DateTime dt)
    {
        var startTime = dt.ToUniversalTime();
      
        var currCpm = cpm.Where(c => 
                c.TradeTime.Year == startTime.Year && 
                c.TradeTime.Month == startTime.Month &&
                c.TradeTime.Day == startTime.Day && 
                c.TradeTime.Hour == startTime.Hour &&
                c.TradeTime.Minute == startTime.Minute).ToList();

        return currCpm.Count == 0
            ? cpm.LastOrDefault()?.Price??cpm.First(x => x.TradeTime < startTime).Price
            : currCpm.Average(x => x.Price);
    }
}