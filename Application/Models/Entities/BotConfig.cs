namespace Application.Models.Entities;

public class BotConfig
{
    public int InstrumentId { get; set; }
    public string Endpoint { get; set; }

    public CoefficientStorage Coefficients { get; set; } = null!;
}