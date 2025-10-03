using System.Text.Json;

namespace Application.Services.Exchanges.Dto;

public class Result
{
    public string Symbol { get; set; }
    public string Category { get; set; }
    public List<List<JsonElement>> List { get; set; }
}

public class RetExtInfo
{
}

public class ByBitDto
{
    public int RetCode { get; set; }
    public string RetMsg { get; set; }
    public Result Result { get; set; }
    public RetExtInfo RetExtInfo { get; set; }
    public long Time { get; set; }
}

