using System.Net.Http.Json;
using System.Text.Json;
using AgiVysSystem.Api.DTOs.External.Asaas;
using Microsoft.Extensions.Configuration;

namespace AgiVysSystem.Api.Services.External;

public class AsaasService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public AsaasService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Asaas:ApiKey"] ?? throw new ArgumentNullException("Asaas ApiKey não configurada.");
        
        // Configura o Header de Autenticação padrão do Asaas
        if (!_httpClient.DefaultRequestHeaders.Contains("access_token"))
        {
            _httpClient.DefaultRequestHeaders.Add("access_token", _apiKey);
        }
    }

    /// <summary>
    /// Cria um novo cliente no Asaas e retorna o ID (cus_...)
    /// </summary>
    public async Task<string?> CreateCustomerAsync(AsaasCustomerRequest request)
    {
        // GERA O LOG DO JSON EXATO ANTES DE ENVIAR
        var jsonPayload = JsonSerializer.Serialize(request);
        Console.WriteLine("\n=== LOG ASAAS: PAYLOAD ENVIADO ===");
        Console.WriteLine(jsonPayload);
        Console.WriteLine("==================================\n");

        var response = await _httpClient.PostAsJsonAsync("customers", request);

        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<AsaasCustomerResponse>();
            return data?.id;
        }

        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"Erro ao criar cliente no Asaas: {error}");
    }

    /// <summary>
    /// Cria uma assinatura recorrente no cartão de crédito
    /// </summary>
    public async Task<AsaasSubscriptionResponse?> CreateSubscriptionAsync(AsaasSubscriptionRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("subscriptions", request);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AsaasSubscriptionResponse>();
        }

        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"Erro ao criar assinatura no Asaas: {error}");
    }
}