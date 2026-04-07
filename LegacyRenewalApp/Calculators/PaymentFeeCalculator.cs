using System;

namespace LegacyRenewalApp.Calculators;

public interface IPaymentFeeCalculator
{
    (decimal Fee, string Note) CalculateFee(string paymentMethod, decimal baseAmountWithSupport);
}

public class PaymentFeeCalculator : IPaymentFeeCalculator
{
    public (decimal Fee, string Note) CalculateFee(string paymentMethod, decimal baseAmountWithSupport)
    {
        return paymentMethod switch
        {
            "CARD" => (baseAmountWithSupport * 0.02m, "card payment fee; "),
            "BANK_TRANSFER" => (baseAmountWithSupport * 0.01m, "bank transfer fee; "),
            "PAYPAL" => (baseAmountWithSupport * 0.035m, "paypal fee; "),
            "INVOICE" => (0m, "invoice payment; "),
            _ => throw new ArgumentException("Unsupported payment method")
        };
    }
}