using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AgiVysSystem.Api.Models.People;

namespace AgiVysSystem.Api.Models.Financial;

[Table("Payments")]
public class Payment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string AsaasId { get; set; }

    [StringLength(100)]
    public string? AsaasSubscriptionId { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual AgiVysSystem.Api.Models.User.User User { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetValue { get; set; }

    [Required]
    [StringLength(30)]
    public string BillingType { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; }

    public DateTime? PaymentDate { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}