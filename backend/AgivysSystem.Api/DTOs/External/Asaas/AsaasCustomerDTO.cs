namespace AgiVysSystem.Api.DTOs.External.Asaas;

public class AsaasCustomerRequest
{
    public string name { get; set; } = string.Empty;
    public string cpfCnpj { get; set; } = string.Empty;
    public string? email { get; set; }
    public string? phone { get; set; }
    public string? address { get; set; }
    public string? addressNumber { get; set; }
    public string? province { get; set; } 
    public string? postalCode { get; set; }
}

public class AsaasCustomerResponse
{
    public string id { get; set; } = string.Empty;
}