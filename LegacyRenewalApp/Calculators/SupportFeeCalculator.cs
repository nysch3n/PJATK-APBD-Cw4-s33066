namespace LegacyRenewalApp.Calculators;

public interface ISupportFeeCalculator
{
    (decimal Fee, string Note) CalculateFee(string planCode, bool includeSupport);
}

public class SupportFeeCalculator : ISupportFeeCalculator
{
    public (decimal Fee, string Note) CalculateFee(string planCode, bool includeSupport)
    {
        if (!includeSupport) return (0m, string.Empty);

        decimal fee = planCode switch
        {
            "START" => 250m,
            "PRO" => 400m,
            "ENTERPRISE" => 700m,
            _ => 0m
        };
        return (fee, "premium support included; ");
    }
}