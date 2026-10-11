namespace Shared.Models;

public record GetPantrySummaryResponse(
    int Total,
    int Expired,
    int Today,
    int Soon,
    int Fresh,
    int Attention,
    Dictionary<string, int> ByLocation);
