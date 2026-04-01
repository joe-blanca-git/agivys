using Microsoft.EntityFrameworkCore;
using AgiVysSystem.Api.Data;
using AgiVysSystem.Api.Models.Financial;
using AgiVysSystem.Api.Models.Order;
using AgiVysSystem.Api.Models.User;
using AgiVysSystem.Api.Services.External;
using AgiVysSystem.Api.DTOs.External.Asaas;

namespace AgiVysSystem.Api.Services.Financial;

public class CheckoutService
{
    private readonly AppDbContext _context;
    private readonly AsaasService _asaasService;

    public CheckoutService(AppDbContext context, AsaasService asaasService)
    {
        _context = context;
        _asaasService = asaasService;
    }

    public async Task<bool> ProcessSubscriptionAsync(int userId, decimal planValue, int planId, AsaasSubscriptionRequest checkoutData)
    {
        var user = await _context.Users
            .Include(u => u.Person)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.Person == null)
            throw new Exception("Usuário ou dados de pessoa não encontrados.");

        // 1. Garantir ID no Asaas
        if (string.IsNullOrEmpty(user.AsaasCustomerId))
        {
            var customerRequest = new AsaasCustomerRequest
            {
                // AJUSTE: Usei 'Name' em vez de 'FullName'. Verifique sua classe Person.
                name = user.Person.Name, 
                email = user.Email!,
                cpfCnpj = user.Person.Document,
                mobilePhone = user.PhoneNumber
            };

            var asaasId = await _asaasService.CreateCustomerAsync(customerRequest);
            user.AsaasCustomerId = asaasId;
            
            _context.Users.Update(user);
            // CORREÇÃO: Método correto é SaveChangesAsync
            await _context.SaveChangesAsync(); 
        }

        // 2. Criar Assinatura no Asaas
        checkoutData.customer = user.AsaasCustomerId!;
        checkoutData.value = planValue;

        var asaasResponse = await _asaasService.CreateSubscriptionAsync(checkoutData);

        if (asaasResponse != null)
        {
            // 3. Ordem Interna
            var order = new AgiVysSystem.Api.Models.Order.Order
            {
                UserId = userId,
                TotalValue = planValue,
                Status = "Pending",
                Type = 1,
                Items = new List<OrderItem> 
                { 
                    new OrderItem { ItemId = planId, ItemType = "Plan", Value = planValue } 
                }
            };

            _context.Orders.Add(order);

            // 4. Registro de Pagamento
            var payment = new Payment
            {
                UserId = userId,
                AsaasId = asaasResponse.id,
                AsaasSubscriptionId = asaasResponse.id,
                Value = planValue,
                Status = asaasResponse.status,
                BillingType = "CREDIT_CARD",
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return true;
        }

        return false;
    }
}