namespace falling_nobody_task.Models;

public class Reservation
{
    public Guid Id { get; set; }
    public int RoomId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
}