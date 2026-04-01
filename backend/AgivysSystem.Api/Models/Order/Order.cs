using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AgiVysSystem.Api.Models.People;
using AgiVysSystem.Api.Models.Financial;

namespace AgiVysSystem.Api.Models.Order;

[Table("Orders")]
public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]

    public virtual AgiVysSystem.Api.Models.User.User User { get; set; } 

    [Required]
    public int Type { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalValue { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}