using System.ComponentModel.DataAnnotations;

namespace TripAgency.Models.DTOs;

public class ClientCreateDTO
{
    [Required]
    [MinLength(2)]
    public string FirstName { get; set; }
    [Required]
    [MinLength(2)]
    public string LastName { get; set; }
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    [Phone]
    public string Telephone { get; set; }
    [Required]
    [StringLength(11, MinimumLength = 11)]
    public string Pesel { get; set; }
}