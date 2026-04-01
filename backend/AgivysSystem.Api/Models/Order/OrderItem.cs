using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgiVysSystem.Api.Models.Order;

[Table("OrderItems")]
public class OrderItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }

    [ForeignKey("OrderId")]
    public virtual AgiVysSystem.Api.Models.Order.Order Order { get; set; } 

    [Required]
    public int ItemId { get; set; }

    [Required]
    [StringLength(50)]
    public string ItemType { get; set; } = "Plan";

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }
}