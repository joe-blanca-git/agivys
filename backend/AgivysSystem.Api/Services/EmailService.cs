using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using AgiVysSystem.Api.Interfaces;

namespace AgiVysSystem.Api.Service;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public EmailService(IConfiguration config, HttpClient httpClient)
    {
        _config = config;
        _httpClient = httpClient;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var apiKey = _config["EmailSettings:ApiKey"];
        var senderName = _config["EmailSettings:SenderName"] ?? "AgiVys System";
        var senderEmail = _config["EmailSettings:SenderEmail"] ?? "noreply@joederblanca.com.br";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            from = $"{senderName} <{senderEmail}>",
            to = new[] { email },
            subject = subject,
            html = htmlMessage
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.resend.com/emails", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"\n[ERRO RESEND] Falha ao enviar e-mail: {error}");
            throw new Exception($"Erro no envio de e-mail via Resend: {error}");
        }
    }
}