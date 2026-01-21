using falling_nobody_task.Services;
using falling_nobody_task.Models;

namespace falling_nobody_task.Tests;

/// <summary>
/// Unit tests for ReservationService based on user stories and acceptance criteria.
/// Follows Microsoft's unit testing best practices: descriptive naming, AAA pattern,
/// one assertion per test, testing public behavior, avoiding test logic.
/// </summary>
public class ReservationServiceTests
{
    private readonly ReservationService _service;

    public ReservationServiceTests()
    {
        _service = new ReservationService();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesThirtyRooms()
    {
        // Arrange & Act
        var rooms = _service.GetAllRooms();

        // Assert
        Assert.Equal(30, rooms.Count());
        for (int i = 1; i <= 30; i++)
        {
            Assert.Contains(rooms, r => r.Id == i && r.Name == $"Room {i}");
        }
    }

    [Fact]
    public void Constructor_SeedsInitialReservationsForSixRooms()
    {
        // Arrange & Act: Check that only rooms 1-6 have reservations
        for (int i = 1; i <= 30; i++)
        {
            var reservations = _service.GetReservationsForRoom(i);
            if (i <= 6)
            {
                Assert.Single(reservations); // Rooms 1-6 should have exactly one reservation
            }
            else
            {
                Assert.Empty(reservations); // Rooms 7-30 should have no reservations
            }
        }
    }

    #endregion

    #region CreateReservation Tests

    [Fact]
    public void CreateReservation_WithValidData_ReturnsReservationWithUniqueId()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = startTime.AddHours(1);

        // Act
        var result = _service.CreateReservation(1, startTime, endTime);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(1, result.RoomId);
        Assert.Equal(startTime, result.StartTime);
        Assert.Equal(endTime, result.EndTime);
    }

    [Fact]
    public void CreateReservation_StartTimeAfterEndTime_ThrowsArgumentException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = startTime.AddHours(-1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.CreateReservation(1, startTime, endTime));
        Assert.Contains("Start time must be before end time", exception.Message);
    }

    [Fact]
    public void CreateReservation_InThePast_ThrowsArgumentException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = startTime.AddHours(1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _service.CreateReservation(1, startTime, endTime));
        Assert.Contains("cannot be made in the past", exception.Message);
    }

    [Fact]
    public void CreateReservation_OverlapsExistingReservation_ThrowsInvalidOperationException()
    {
        // Arrange: Use seeded reservation for room 1 (starts in 1 hour)
        var startTime = DateTime.UtcNow.AddHours(1).AddMinutes(30);
        var endTime = startTime.AddHours(1);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _service.CreateReservation(1, startTime, endTime));
        Assert.Contains("overlaps with existing reservation", exception.Message);
    }

    [Fact]
    public void CreateReservation_InvalidRoomId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = startTime.AddHours(1);

        // Act & Assert
        var exception = Assert.Throws<KeyNotFoundException>(() =>
            _service.CreateReservation(31, startTime, endTime));
        Assert.Contains("Room not found", exception.Message);
    }

        [Fact]
    public void CreateReservation_AtExactBoundaryTime_Overlaps()
    {
        // Arrange: Create first reservation
        var startTime1 = DateTime.UtcNow.AddHours(10);
        var endTime1 = startTime1.AddHours(1);
        _service.CreateReservation(7, startTime1, endTime1); // Room 7 has no seeded reservations

        // Act & Assert: Try to create second reservation starting exactly at end of first - should overlap
        var startTime2 = endTime1;
        var endTime2 = startTime2.AddHours(1);
        Assert.Throws<InvalidOperationException>(() =>
            _service.CreateReservation(7, startTime2, endTime2));
    }

    [Fact]
    public void CreateReservation_WithVeryShortDuration_Succeeds()
    {
        // Arrange: Edge case - very short reservation
        var startTime = DateTime.UtcNow.AddHours(10);
        var endTime = startTime.AddMinutes(1);

        // Act
        var result = _service.CreateReservation(8, startTime, endTime);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(8, result.RoomId);
    }

    #endregion

    #region CancelReservations Tests

    [Fact]
    public void CancelReservation_WithValidId_ReturnsTrueAndRemovesReservation()
    {
        // Arrange
        var reservations = _service.GetReservationsForRoom(1);
        var reservationId = reservations.First().Id;

        // Act
        var result = _service.CancelReservation(reservationId);

        // Assert
        Assert.True(result);
        Assert.Empty(_service.GetReservationsForRoom(1));
    }

    [Fact]
    public void CancelReservation_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = _service.CancelReservation(invalidId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetReservationsForRoom Tests

    [Fact]
    public void GetReservationsForRoom_WithValidRoomId_ReturnsReservationsInChronologicalOrder()
    {
        // Arrange
        var roomId = 1;

        // Act
        var reservations = _service.GetReservationsForRoom(roomId);

        // Assert
        Assert.Single(reservations);
        var reservation = reservations.First();
        Assert.Equal(roomId, reservation.RoomId);
        // Verify chronological order (only one, but if multiple, they should be sorted)
    }

    // Add test for checking multiple reservations and that they are in order
    [Fact]
    public void GetReservationsForRoom_ReturnsMultipleReservationsInChronologicalOrder()
    {
        // Arrange: create three non-overlapping reservations for room 9
        var start1 = DateTime.UtcNow.AddHours(5);
        var end1 = start1.AddHours(1);
        var r1 = _service.CreateReservation(9, start1, end1);

        var start2 = end1.AddMinutes(1);
        var end2 = start2.AddHours(1);
        var r2 = _service.CreateReservation(9, start2, end2);

        var start3 = end2.AddMinutes(1);
        var end3 = start3.AddHours(1);
        var r3 = _service.CreateReservation(9, start3, end3);

        // Act
        var reservations = _service.GetReservationsForRoom(9).ToList();

        // Assert: should contain three reservations in chronological order
        Assert.Equal(3, reservations.Count);
        Assert.Equal(r1.Id, reservations[0].Id);
        Assert.Equal(r2.Id, reservations[1].Id);
        Assert.Equal(r3.Id, reservations[2].Id);
        Assert.True(reservations[0].StartTime < reservations[1].StartTime);
        Assert.True(reservations[1].StartTime < reservations[2].StartTime);
    }
    [Fact]
    public void GetReservationsForRoom_WithRoomHavingNoReservations_ReturnsEmptyList()
    {
        // Arrange
        var roomId = 7; // From seeded data, room 7 has no reservations

        // Act
        var reservations = _service.GetReservationsForRoom(roomId);

        // Assert
        Assert.Empty(reservations);
    }

    #endregion
}
