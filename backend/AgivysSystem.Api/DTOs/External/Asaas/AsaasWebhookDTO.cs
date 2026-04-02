namespace AgiVysSystem.Api.DTOs.External.Asaas;

public class AsaasWebhookDTO
{
    public string @event { get; set; } = string.Empty; // PAYMENT_CONFIRMED, PAYMENT_RECEIVED, etc.
    public PaymentWebhookData payment { get; set; } = new();
}

public class PaymentWebhookData
{
    public string id { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public string subscription { get; set; } = string.Empty;
    public decimal value { get; set; }
    public decimal? netValue { get; set; }
    public DateTime? paymentDate { get; set; }
}