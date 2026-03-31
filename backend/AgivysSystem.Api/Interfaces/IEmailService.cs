namespace AgiVysSystem.Api.Interfaces; // Ajustado para sua pasta

public interface IEmailService
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}