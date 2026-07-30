using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using AgiVysSystem.Api.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using AgiVysSystem.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AgiVysSystem.Api.Service;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly IServiceProvider _serviceProvider;

    public EmailService(IConfiguration config, HttpClient httpClient, IServiceProvider serviceProvider)
    {
        _config = config;
        _httpClient = httpClient;
        _serviceProvider = serviceProvider;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage, int? appSystemId = null)
    {
        var apiKey = _config["EmailSettings:ApiKey"];
        var senderName = _config["EmailSettings:SenderName"] ?? "AgiVys System";
        var senderEmail = _config["EmailSettings:SenderEmail"] ?? "noreply@agivyssystem.com.br";

        if (appSystemId.HasValue)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var emailIntegration = await dbContext.Integrations
                .Include(i => i.Parameters)
                .FirstOrDefaultAsync(i => i.AppSystemId == appSystemId.Value && i.Type.ToLower() == "email");

            if (emailIntegration != null)
            {
                var dbApiKey = emailIntegration.Parameters.FirstOrDefault(p => p.Key.ToLower() == "apikey")?.Value;
                var dbSenderName = emailIntegration.Parameters.FirstOrDefault(p => p.Key.ToLower() == "sendername")?.Value;
                var dbSenderEmail = emailIntegration.Parameters.FirstOrDefault(p => p.Key.ToLower() == "senderemail")?.Value;

                if (!string.IsNullOrEmpty(dbApiKey)) apiKey = dbApiKey;
                if (!string.IsNullOrEmpty(dbSenderName)) senderName = dbSenderName;
                if (!string.IsNullOrEmpty(dbSenderEmail)) senderEmail = dbSenderEmail;
            }
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Erro na integração Resend, verifique as configurações da integração");
        }

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