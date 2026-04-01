namespace AgiVysSystem.Api.DTOs.External.Asaas;

public class AsaasSubscriptionRequest
{
    public string customer { get; set; } = string.Empty;
    public string billingType { get; set; } = "CREDIT_CARD";
    public decimal value { get; set; }
    public string nextDueDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    public string cycle { get; set; } = "MONTHLY";
    public string description { get; set; } = "Assinatura AgiVys System";
    public CreditCardDTO creditCard { get; set; } = new();
    public CreditCardHolderInfoDTO creditCardHolderInfo { get; set; } = new();
    public string remoteIp { get; set; } = string.Empty;
}

public class CreditCardDTO
{
    public string holderName { get; set; } = string.Empty;
    public string number { get; set; } = string.Empty;
    public string expiryMonth { get; set; } = string.Empty;
    public string expiryYear { get; set; } = string.Empty;
    public string ccv { get; set; } = string.Empty;
}

public class CreditCardHolderInfoDTO
{
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string cpfCnpj { get; set; } = string.Empty;
    public string postalCode { get; set; } = string.Empty;
    public string addressNumber { get; set; } = string.Empty;
    public string phone { get; set; } = string.Empty;
}

public class AsaasSubscriptionResponse
{
    public string id { get; set; } = string.Empty; // sub_...
    public string status { get; set; } = string.Empty;
}