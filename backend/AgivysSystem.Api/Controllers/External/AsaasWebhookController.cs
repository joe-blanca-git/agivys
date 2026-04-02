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
            // Dentro do if que verifica "PAYMENT_CONFIRMED" ou "PAYMENT_RECEIVED":

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.AsaasSubscriptionId == payload.payment.subscription);

            if (payment != null)
            {
                // 1. Atualiza o status na tabela Payments
                payment.Status = payload.payment.status; 
                _context.Payments.Update(payment);

                // 2. Acha a Order pendente desse mesmo usuário para atualizar
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.UserId == payment.UserId && o.Status == "Pending");

                if (order != null)
                {
                    // Atualiza para o status de aprovado que você preferir (ex: "Confirmed", "Paid", "Active")
                    order.Status = "Confirmed"; 
                    _context.Orders.Update(order);
                }

                // Salva as duas tabelas de uma vez
                await _context.SaveChangesAsync();
            }
        }

        return Ok(); // Sempre retorne 200 para o Asaas não ficar tentando reenviar
    }
}