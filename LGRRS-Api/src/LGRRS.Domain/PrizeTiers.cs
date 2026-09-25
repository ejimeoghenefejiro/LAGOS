namespace LGRRS.Domain;

// Receipt amount decides which prize tier a reward entry can win — a small purchase can
// only win the small-tier prize, never the jackpot. Tuned for demo-scale amounts.
public static class PrizeTiers
{
    public const decimal SmallMax = 2000m;
    public const decimal MediumMax = 10000m;

    public const decimal SmallPrize = 2500m;
    public const decimal MediumPrize = 10000m;
    public const decimal LargePrize = 25000m;

    public static string TierFor(decimal amount) => amount switch
    {
        <= SmallMax => "Small",
        <= MediumMax => "Medium",
        _ => "Large"
    };

    public static decimal PrizeFor(string tier) => tier switch
    {
        "Small" => SmallPrize,
        "Medium" => MediumPrize,
        _ => LargePrize
    };

    public static readonly string[] AllTiers = { "Small", "Medium", "Large" };
}
