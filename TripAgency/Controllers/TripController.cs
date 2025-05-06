using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TripAgency.Exceptions;
using TripAgency.Models.DTOs;
using TripAgency.Services;

namespace TripAgency.Controllers;

[ApiController]
[Route("api")]
public class TripController(IDbService dbService) : ControllerBase
{
    [HttpGet]
    [Route("trips")]
    //Ten endpoint będzie pobierał wszystkie dostępne wycieczki wraz z ich podstawowymi informacjami.
    public async Task<IActionResult> GetAllTrips()
    {
        return Ok(await dbService.GetTripsDetailsAsync());
    }

    [HttpGet]
    [Route("clients/{id}/trips")]
    //Ten endpoint będzie pobierał wszystkie wycieczki powiązane z konkretnym klientem.
    public async Task<IActionResult> GetTripsByClientId([FromRoute] int id)
    {
        try
        {
            return Ok(await dbService.GetTripsDetailsByClientIdAsync(id));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (NotFoundTripsException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    [Route("clients")]
    //Ten endpoint utworzy nowy rekord klienta.
    public async Task<IActionResult> CreateClient([FromBody] ClientCreateDTO body)
    {
        var client = await dbService.CreateClientAsync(body);
        return Created($"clients/{client.Id}", client);
    }

    [HttpPut]
    [Route("clients/{id}/trips/{tripId}")]
    //Ten endpoint zarejestruje klienta na konkretną wycieczkę.
    public async Task<IActionResult> AddClientToTrip([FromRoute] int id, int tripId)
    {
        try
        {
            await dbService.AddClientToTripAsync(id, tripId);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (NoPlaceException e)
        {
            return Conflict(e.Message);
        }
    }
    
    [HttpDelete]
    [Route("clients/{id}/trips/{tripId}")]
    //Ten endpoint usunie rejestrację klienta z wycieczki.
    public async Task<IActionResult> DeleteClientFromTrip([FromRoute] int id, int tripId)
    {
        try
        {
            await dbService.DeleteClientFromTripAsync(id, tripId);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
}