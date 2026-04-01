namespace AgiVysSystem.Api.DTOs.External.Asaas;

public class AsaasCustomerRequest
{
    public string name { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string cpfCnpj { get; set; } = string.Empty;
    public string? mobilePhone { get; set; }
}

public class AsaasCustomerResponse
{
    public string id { get; set; } = string.Empty;
}