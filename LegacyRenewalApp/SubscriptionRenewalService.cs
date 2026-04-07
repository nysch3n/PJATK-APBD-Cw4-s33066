using System;
using LegacyRenewalApp.Calculators;
using LegacyRenewalApp.Infrastructure;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly CustomerRepository _customerRepository;
        private readonly SubscriptionPlanRepository _planRepository;
        private readonly IBillingGatewayAdapter _billingAdapter;
        private readonly IDiscountCalculator _discountCalculator;
        private readonly ISupportFeeCalculator _supportFeeCalculator;
        private readonly IPaymentFeeCalculator _paymentFeeCalculator;
        private readonly ITaxCalculator _taxCalculator;

        public SubscriptionRenewalService() 
            : this(new CustomerRepository(), 
                   new SubscriptionPlanRepository(), 
                   new BillingGatewayAdapter(), 
                   new DiscountCalculator(), 
                   new SupportFeeCalculator(), 
                   new PaymentFeeCalculator(), 
                   new TaxCalculator())
        {
        }

        public SubscriptionRenewalService(
            CustomerRepository customerRepository,
            SubscriptionPlanRepository planRepository,
            IBillingGatewayAdapter billingAdapter,
            IDiscountCalculator discountCalculator,
            ISupportFeeCalculator supportFeeCalculator,
            IPaymentFeeCalculator paymentFeeCalculator,
            ITaxCalculator taxCalculator)
        {
            _customerRepository = customerRepository;
            _planRepository = planRepository;
            _billingAdapter = billingAdapter;
            _discountCalculator = discountCalculator;
            _supportFeeCalculator = supportFeeCalculator;
            _paymentFeeCalculator = paymentFeeCalculator;
            _taxCalculator = taxCalculator;
        }

        public RenewalInvoice CreateRenewalInvoice(
            int customerId, string planCode, int seatCount, string paymentMethod, bool includePremiumSupport, bool useLoyaltyPoints)
        {
            if (customerId <= 0) throw new ArgumentException("Customer id must be positive");
            if (string.IsNullOrWhiteSpace(planCode)) throw new ArgumentException("Plan code is required");
            if (seatCount <= 0) throw new ArgumentException("Seat count must be positive");
            if (string.IsNullOrWhiteSpace(paymentMethod)) throw new ArgumentException("Payment method is required");

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();

            var customer = _customerRepository.GetById(customerId);
            var plan = _planRepository.GetByCode(normalizedPlanCode);

            if (!customer.IsActive) throw new InvalidOperationException("Inactive customers cannot renew subscriptions");

            string notes = string.Empty;

            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;

            var discountResult = _discountCalculator.Calculate(customer, plan, baseAmount, seatCount, useLoyaltyPoints);
            notes += discountResult.Notes;

            decimal subtotalAfterDiscount = baseAmount - discountResult.Discount;
            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                notes += "minimum discounted subtotal applied; ";
            }

            var supportResult = _supportFeeCalculator.CalculateFee(normalizedPlanCode, includePremiumSupport);
            notes += supportResult.Note;

            var paymentResult = _paymentFeeCalculator.CalculateFee(normalizedPaymentMethod, subtotalAfterDiscount + supportResult.Fee);
            notes += paymentResult.Note;

            decimal taxRate = _taxCalculator.GetTaxRate(customer.Country);
            decimal taxBase = subtotalAfterDiscount + supportResult.Fee + paymentResult.Fee;
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = taxBase + taxAmount;

            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes += "minimum invoice amount applied; ";
            }

            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountResult.Discount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportResult.Fee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentResult.Fee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };

            _billingAdapter.SaveInvoice(invoice);

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                string subject = "Subscription renewal invoice";
                string body = $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} has been prepared. Final amount: {invoice.FinalAmount:F2}.";
                _billingAdapter.SendEmail(customer.Email, subject, body);
            }

            return invoice;
        }
    }
}