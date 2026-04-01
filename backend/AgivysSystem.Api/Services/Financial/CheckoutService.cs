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
            // Pega o endereço da pessoa (ajuste os nomes das propriedades se necessário)
            var addr = await _context.AddressPeople.FirstOrDefaultAsync(a => a.PersonId == user.PersonId);

            var customerRequest = new AsaasCustomerRequest
            {
                name = user.Person.Name,
                cpfCnpj = user.Person.Document.Replace(".", "").Replace("-", "").Replace("/", "").Trim(),
                email = user.Email,
                phone = user.PhoneNumber ?? "",
                address = addr?.Street,
                addressNumber = addr?.Number,
                province = addr?.State, // "SP"
                postalCode = addr?.ZipCode?.Replace("-", "")
            };

            var asaasId = await _asaasService.CreateCustomerAsync(customerRequest);
            
            if (string.IsNullOrEmpty(asaasId))
                throw new Exception("O Asaas não retornou um ID de cliente válido.");

            user.AsaasCustomerId = asaasId;
            _context.Users.Update(user);
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