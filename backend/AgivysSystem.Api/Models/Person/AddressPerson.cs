using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AgiVysSystem.Api.Models.People;

namespace AgiVysSystem.Api.Models.People;

public class AddressPerson
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Description { get; set; } // Ex: Casa, Trabalho, Entrega

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

    // Relacionamento com a Pessoa (Dona do endereço)
    public int PersonId { get; set; }
    
    [ForeignKey("PersonId")]
    public virtual Person Person { get; set; }
}