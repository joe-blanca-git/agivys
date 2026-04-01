using AgiVysSystem.Api.DTOs.External.Asaas;

namespace AgiVysSystem.Api.DTOs.Financial;

public class CheckoutRequestDTO
{
    public int PlanId { get; set; }
    public decimal PlanValue { get; set; }
    public CreditCardDTO CreditCard { get; set; } = new();
    public CreditCardHolderInfoDTO CreditCardHolderInfo { get; set; } = new();
}