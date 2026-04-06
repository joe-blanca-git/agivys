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

        if (webhook.@event == "PAYMENT_RECEIVED" || webhook.@event == "PAYMENT_CONFIRMED")
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.AsaasSubscriptionId == webhook.payment.subscription);

            if (payment != null)
            {
                payment.Status = webhook.payment.status; 
                payment.PaymentDate = DateTime.Now; 
                
                _context.Payments.Update(payment);

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.UserId == payment.UserId && o.Status == "Pending");

                if (order != null)
                {
                    order.Status = "Confirmed"; 
                    _context.Orders.Update(order);
                }

                await _context.SaveChangesAsync();
            }
        }

        return Ok();
    }
}