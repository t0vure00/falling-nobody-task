using falling_nobody_task.Models;
using System.Collections.Generic;
using System.Linq;

namespace falling_nobody_task.Services;

public class ReservationService
{
    private readonly List<Room> _rooms;
    private readonly List<Reservation> _reservations = new();
    private readonly object _lock = new();

    public ReservationService()
    {
        _rooms = Enumerable.Range(1, 30).Select(i => new Room { Id = i, Name = $"Room {i}" }).ToList();
        SeedReservations();
    }

    private void SeedReservations()
    {
        var now = DateTimeOffset.UtcNow;
        lock (_lock)
        {
            _reservations.AddRange(new[]
            {
                new Reservation { Id = Guid.NewGuid(), RoomId = 1, StartTime = now.AddHours(1), EndTime = now.AddHours(2) },
                new Reservation { Id = Guid.NewGuid(), RoomId = 2, StartTime = now.AddHours(2), EndTime = now.AddHours(3) },
                new Reservation { Id = Guid.NewGuid(), RoomId = 3, StartTime = now.AddHours(3), EndTime = now.AddHours(4) },
                new Reservation { Id = Guid.NewGuid(), RoomId = 4, StartTime = now.AddHours(4), EndTime = now.AddHours(5) },
                new Reservation { Id = Guid.NewGuid(), RoomId = 5, StartTime = now.AddHours(5), EndTime = now.AddHours(6) },
                new Reservation { Id = Guid.NewGuid(), RoomId = 6, StartTime = now.AddHours(6), EndTime = now.AddHours(7) }
            });
        }
    }

    public IReadOnlyList<Room> GetAllRooms() => _rooms;

    public IReadOnlyList<Reservation> GetReservationsForRoom(int roomId)
    {
        lock (_lock)
        {
            return _reservations.Where(r => r.RoomId == roomId).ToList();
        }
    }

    public Reservation CreateReservation(int roomId, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var now = DateTimeOffset.UtcNow;

        if (startTime >= endTime)
            throw new ArgumentException("Start time must be before end time.");
        if (startTime <= now)
            throw new ArgumentException("Reservations cannot be made in the past.");
        if (!_rooms.Any(r => r.Id == roomId))
            throw new KeyNotFoundException("Room not found.");

        lock (_lock)
        {
            if (_reservations.Any(r => r.RoomId == roomId && !(endTime <= r.StartTime || startTime > r.EndTime)))
                throw new InvalidOperationException("Reservation overlaps with existing reservation.");

            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                StartTime = startTime,
                EndTime = endTime
            };
            _reservations.Add(reservation);
            return reservation;
        }
    }

    public bool CancelReservation(Guid id)
    {
        lock (_lock)
        {
            var reservation = _reservations.FirstOrDefault(r => r.Id == id);
            if (reservation == null) return false;
            return _reservations.Remove(reservation);
        }
    }
}