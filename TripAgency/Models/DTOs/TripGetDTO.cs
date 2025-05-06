namespace TripAgency.Models.DTOs;

public class TripGetDTO
{
    public int Id { get; set; }
    public string CountryName { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
}