namespace falling_nobody_task.Models;

public class Reservation
{
    public Guid Id { get; set; }
    public int RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}