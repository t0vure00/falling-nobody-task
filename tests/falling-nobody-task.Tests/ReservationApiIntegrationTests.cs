using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using falling_nobody_task.Models;

namespace falling_nobody_task.Tests;

public class ReservationApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ReservationApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    #region GetRooms Tests

    [Fact]
    public async Task GetRooms_Returns200With30Rooms()
    {
        // Act
        var response = await _client.GetAsync("/rooms");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rooms = await response.Content.ReadFromJsonAsync<IEnumerable<Room>>();
        Assert.Equal(30, rooms.Count());
    }

    #endregion

    #region GetReservations Tests

    [Fact]
    public async Task GetReservations_WithValidRoomId_Returns200WithReservations()
    {
        // Act
        var response = await _client.GetAsync("/reservations?roomId=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reservations = await response.Content.ReadFromJsonAsync<IEnumerable<Reservation>>();
        Assert.Single(reservations);
    }

    [Fact]
    public async Task GetReservations_WithInvalidRoomId_Returns400()
    {
        // Act
        var response = await _client.GetAsync("/reservations?roomId=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region PostReservations Tests

    [Fact]
    public async Task PostReservations_WithValidData_Returns201()
    {
        // Arrange
        var request = new { roomId = 7, startTime = "2026-01-22T10:00:00Z", endTime = "2026-01-22T11:00:00Z" };

        // Act
        var response = await _client.PostAsJsonAsync("/reservations", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reservation = await response.Content.ReadFromJsonAsync<Reservation>();
        Assert.NotEqual(Guid.Empty, reservation.Id);
        Assert.Equal(7, reservation.RoomId);
    }

    [Fact]
    public async Task PostReservations_WithOverlappingReservation_Returns409()
    {
        // Arrange: First create a reservation
        var request1 = new { roomId = 8, startTime = "2026-01-22T10:00:00Z", endTime = "2026-01-22T11:00:00Z" };
        await _client.PostAsJsonAsync("/reservations", request1);

        // Now try overlapping
        var request2 = new { roomId = 8, startTime = "2026-01-22T10:30:00Z", endTime = "2026-01-22T11:30:00Z" };

        // Act
        var response = await _client.PostAsJsonAsync("/reservations", request2);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostReservations_WithInvalidRoomId_Returns404()
    {
        // Arrange
        var request = new { roomId = 31, startTime = "2026-01-22T10:00:00Z", endTime = "2026-01-22T11:00:00Z" };

        // Act
        var response = await _client.PostAsJsonAsync("/reservations", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostReservations_WithInvalidDateFormat_Returns400()
    {
        // Arrange
        var request = new { roomId = 9, startTime = "invalid-date", endTime = "2026-01-22T11:00:00Z" };

        // Act
        var response = await _client.PostAsJsonAsync("/reservations", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region DeleteReservations Tests

    [Fact]
    public async Task DeleteReservations_WithValidId_Returns204()
    {
        // Arrange: Create a reservation first
        var createRequest = new { roomId = 10, startTime = "2026-01-22T10:00:00Z", endTime = "2026-01-22T11:00:00Z" };
        var createResponse = await _client.PostAsJsonAsync("/reservations", createRequest);
        var reservation = await createResponse.Content.ReadFromJsonAsync<Reservation>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/reservations/{reservation.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteReservations_WithInvalidId_Returns404()
    {
        // Act
        var response = await _client.DeleteAsync($"/reservations/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}