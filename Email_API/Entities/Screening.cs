public class Screening
{
    public DateTime Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Hall Hall { get; set; }
    public Movie Movie { get; set; }
    public int ScreeningId { get; set; }
    public DateTime CreatedAt { get; set; }

}