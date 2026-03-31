using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Usando o plural para não conflitar com a classe Company
namespace AgiVysSystem.Api.Models.Companies; 

public class CompanyAddress
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Description { get; set; } 

    [Required]
    [StringLength(8)]
    public string ZipCode { get; set; }

    [Required]
    public string Street { get; set; }

    [Required]
    public string Number { get; set; }

    public string Complement { get; set; }

    [Required]
    public string Neighborhood { get; set; }

    [Required]
    public string City { get; set; }

    [Required]
    [StringLength(2)]
    public string State { get; set; }

    public int CompanyId { get; set; }
    
    [ForeignKey("CompanyId")]
    public virtual AgiVysSystem.Api.Models.Company.Company Company { get; set; } 
}