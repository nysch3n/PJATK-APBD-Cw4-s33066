namespace LegacyRenewalApp.Infrastructure;

public interface IBillingGatewayAdapter
{
    void SaveInvoice(RenewalInvoice invoice);
    void SendEmail(string email, string subject, string body);
}

public class BillingGatewayAdapter : IBillingGatewayAdapter
{
    public void SaveInvoice(RenewalInvoice invoice) => LegacyBillingGateway.SaveInvoice(invoice);
        
    public void SendEmail(string email, string subject, string body) => LegacyBillingGateway.SendEmail(email, subject, body);
}