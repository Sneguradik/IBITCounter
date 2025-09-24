namespace Application.Models.Entities;

public class CurrentPriceOfMarket
{ 
    public double Price { get; set; }
    public int Amount { get; set; }
    public DateTime TradeTime { get; set; }
}