using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgiVysSystem.Api.Data;
using AgiVysSystem.Api.DTOs.External.Asaas;

namespace AgiVysSystem.Api.Controllers.External;

[ApiController]
[Route("api/webhooks/asaas")]
public class AsaasWebhookController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AsaasWebhookController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] AsaasWebhookDTO webhook)
    {
        var authToken = Request.Headers["asaas-access-token"].ToString();
        var myWebhookToken = _configuration["Asaas:WebhookToken"];

        if (string.IsNullOrEmpty(myWebhookToken) || authToken != myWebhookToken)
            return Unauthorized();

        // Lógica para Pagamento Confirmado ou Recebido
        if (webhook.@event == "PAYMENT_RECEIVED" || webhook.@event == "PAYMENT_CONFIRMED")
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.AsaasId == webhook.payment.id || p.AsaasSubscriptionId == webhook.payment.id);

            if (payment != null)
            {
                payment.Status = "CONFIRMED";
                payment.PaymentDate = webhook.payment.paymentDate ?? DateTime.UtcNow;
                payment.NetValue = webhook.payment.netValue ?? 0;

                // Aqui você também pode buscar a Order vinculada e dar baixa nela
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.UserId == payment.UserId && o.Status == "Pending");
                if (order != null) order.Status = "Paid";

                await _context.SaveChangesAsync();
            }
        }

        return Ok(); // Sempre retorne 200 para o Asaas não ficar tentando reenviar
    }
}