using falling_nobody_task.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ReservationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Rooms endpoint
app.MapGet("/rooms", (ReservationService service) => Results.Ok(service.GetAllRooms()));

// View reservations for a room
app.MapGet("/reservations", (int roomId, ReservationService service) =>
{
    if (roomId < 1 || roomId > 30)
        return Results.BadRequest("Invalid room ID.");
    var reservations = service.GetReservationsForRoom(roomId);
    return Results.Ok(reservations);
});

// Create reservation
app.MapPost("/reservations", (CreateReservationRequest request, ReservationService service) =>
{
    try
    {
        if (!DateTime.TryParse(request.StartTime, out var startTime) ||
            !DateTime.TryParse(request.EndTime, out var endTime))
            return Results.BadRequest("Invalid date format. Use ISO 8601 UTC format.");

        var reservation = service.CreateReservation(request.RoomId, startTime, endTime);
        return Results.Created($"/reservations/{reservation.Id}", reservation);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(ex.Message);
    }
});

// Cancel reservation
app.MapDelete("/reservations/{id}", (Guid id, ReservationService service) =>
{
    var success = service.CancelReservation(id);
    return success ? Results.NoContent() : Results.NotFound("Reservation not found.");
});

app.Run();

public record CreateReservationRequest(int RoomId, string StartTime, string EndTime);

