using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AgiVysSystem.Api.Services.Financial;
using AgiVysSystem.Api.DTOs.Financial;
using AgiVysSystem.Api.DTOs.External.Asaas;
using System.Security.Claims;

namespace AgiVysSystem.Api.Controllers.Financial;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly CheckoutService _checkoutService;

    public CheckoutController(CheckoutService checkoutService)
    {
        _checkoutService = checkoutService;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] CheckoutRequestDTO request)
    {
        try
        {
            // Pega o ID do usuário logado via Token JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Usuário não identificado.");

            int userId = int.Parse(userIdClaim);

            // Mapeia os dados para o request do Asaas
            var asaasRequest = new AsaasSubscriptionRequest
            {
                creditCard = request.CreditCard,
                creditCardHolderInfo = request.CreditCardHolderInfo,
                remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
            };

            var success = await _checkoutService.ProcessSubscriptionAsync(
                userId, 
                request.PlanValue, 
                request.PlanId, 
                asaasRequest
            );

            if (success)
                return Ok(new { message = "Assinatura criada com sucesso!" });

            return BadRequest("Não foi possível processar a assinatura.");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}