using Microsoft.Data.SqlClient;
using TripAgency.Exceptions;
using TripAgency.Models;
using TripAgency.Models.DTOs;

namespace TripAgency.Services;

public interface IDbService
{
    public Task<IEnumerable<TripGetDTO>> GetTripsDetailsAsync();
    public Task<IEnumerable<TripByClientIdDTO>> GetTripsDetailsByClientIdAsync(int id);
    public Task<Client> CreateClientAsync(ClientCreateDTO client);
    public Task AddClientToTripAsync(int id, int tripId);
    public Task DeleteClientFromTripAsync(int id, int tripId);
}
public class DbService(IConfiguration configuration) : IDbService
{
    private readonly string? _connectionString = configuration.GetConnectionString("Default");
    
    public async Task<IEnumerable<TripGetDTO>> GetTripsDetailsAsync()
    {
        var result = new List<TripGetDTO>();
        await using var connection = new SqlConnection(_connectionString);
        //Odczyt danych z Trip + Country
        const string sql = "select Trip.idtrip, C.Name, Trip.name, description, datefrom, dateto, maxpeople from trip join s30071.Country_Trip CT on trip.IdTrip = CT.IdTrip join s30071.Country C on C.IdCountry = CT.IdCountry";
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add( new TripGetDTO
                {
                    Id = reader.GetInt32(0),
                    CountryName = reader.GetString(1),
                    Name = reader.GetString(2),
                    Description = reader.GetString(3),
                    DateFrom = reader.GetDateTime(4),
                    DateTo = reader.GetDateTime(5),
                    MaxPeople = reader.GetInt32(6)
                });
        }
        return result;
    }
    
    public async Task<IEnumerable<TripByClientIdDTO>> GetTripsDetailsByClientIdAsync(int id)
    {
        var result = new List<TripByClientIdDTO>();

        await using var connection = new SqlConnection(_connectionString);
        //Sprawdzamy czy istnieje klient o podanym id
        const string sqlTest = "select * from client where IdClient = @id";
        //Pobieramy dane z Trip + Client_Trip
        const string sql = "select Trip.idtrip, Trip.name, description, datefrom, dateto, maxpeople, RegisteredAt, PaymentDate from trip join s30071.Client_Trip CT on trip.IdTrip = CT.IdTrip where IdClient = @id";

        await using var commandTest = new SqlCommand(sqlTest, connection);
        await using var command = new SqlCommand(sql, connection);
        
        commandTest.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@id", id);
        
        await connection.OpenAsync();
        await using var readerTest = await commandTest.ExecuteReaderAsync();

        if (!await readerTest.ReadAsync())
        {
            throw new NotFoundException($"Client with id: {id} does not exist");
        }
        
        readerTest.Close();
        await using var reader = await command.ExecuteReaderAsync();
        
        if (!reader.HasRows)
        {
            throw new NotFoundTripsException($"Client with id: {id} has not trips");
        }

        while (await reader.ReadAsync())
        {
            result.Add(new TripByClientIdDTO
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                RegisteredAt = reader.GetInt32(6),
                PaymentDate = reader.IsDBNull(7) ? 0 : reader.GetInt32(7)

            });
        }
        return result;
    }

    public async Task<Client> CreateClientAsync(ClientCreateDTO client)
    {
        await using var connection = new SqlConnection(_connectionString);
        //Tworzymy nowego klienta
        const string sql = "INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel) VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel); SELECT scope_identity()";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);
        await connection.OpenAsync();
        var id = Convert.ToInt32(await command.ExecuteScalarAsync());

        return new Client
        {
            Id = id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            Telephone = client.Telephone,
            Pesel = client.Pesel
        };
    }

    public async Task AddClientToTripAsync(int id, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        
        //Sprawdzamy czy istnieje klient o podanym id
        const string sqlTest1 = "select * from client where IdClient = @id";
        //Sprawdzamy czy istnieje podróż o podanym id i jeżeli istnieje to pobieramy MaxPeople
        const string sqlTest2 = "select MaxPeople from trip where IdTrip = @tripId";
        //Odczytujemy ilość już zarejestrowanych klientów na tą wycieczkę
        const string sqlTest3 = "select count(*) from Client_Trip where IdTrip = @tripId";
        //Rejestrujemy klienta na wycieczkę
        const string sql = "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate) VALUES (@IdClient, @IdTrip, @RegisteredAt, @PaymentDate)";
        
        await using var commandTest1 = new SqlCommand(sqlTest1, connection);
        await using var commandTest2 = new SqlCommand(sqlTest2, connection);
        await using var commandTest3 = new SqlCommand(sqlTest3, connection);
        await using var command = new SqlCommand(sql, connection);

        commandTest1.Parameters.AddWithValue("@id", id);
        commandTest2.Parameters.AddWithValue("@tripId", tripId);
        commandTest3.Parameters.AddWithValue("@tripId", tripId);
        command.Parameters.AddWithValue("@idClient", id);
        command.Parameters.AddWithValue("@IdTrip", tripId);
        command.Parameters.AddWithValue("@RegisteredAt", int.Parse(DateTime.Now.ToString("yyyyMMdd")));
        command.Parameters.AddWithValue("@PaymentDate", DBNull.Value);
        await connection.OpenAsync();
        
        await using var readerTest1 = await commandTest1.ExecuteReaderAsync();
        if (!await readerTest1.ReadAsync())
        {
            throw new NotFoundException($"Client with id: {id} does not exist");
        }
        readerTest1.Close();

        var result = await commandTest2.ExecuteScalarAsync();
        if (result == null)
        {
            throw new NotFoundException($"Trip with id: {tripId} does not exist");
        }
        var maxPeople = Convert.ToInt32(result);
        
        var nowPeople = Convert.ToInt32(await commandTest3.ExecuteScalarAsync());
        if (nowPeople + 1 > maxPeople)
        {
            throw new NoPlaceException("There are too many people");
        }
        
        await command.ExecuteNonQueryAsync();
        
    }

    public async Task DeleteClientFromTripAsync(int id, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        //Sprawdzamy,czy rejestracja istnieje
        const string sql = "select * from Client_Trip where IdClient = @id and IdTrip = @tripId";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id",id);
        command.Parameters.AddWithValue("@tripId", tripId);
        await connection.OpenAsync();
        
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new NotFoundException($"There is no trip {tripId} for client {id}");
        }
        reader.Close();
        
        //Usuwamy record rejestracji
        const string sql2 = "delete from Client_Trip where IdClient = @id and IdTrip = @tripId";
        await using var command2 = new SqlCommand(sql2, connection);
        command2.Parameters.AddWithValue("@id",id);
        command2.Parameters.AddWithValue("@tripId", tripId);
        await command2.ExecuteNonQueryAsync();
    }
}