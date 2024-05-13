public class Movie
{
    public int MovieId { get; set; }
    public string Title { get; set; }
    public string Director { get; set; }
    public int Year { get; set; }
    public string Language { get; set; }
    public int Duration { get; set; }
    public int Pegi { get; set; }
    public string ImageURL { get; set; }
    public string TrailerURL { get; set; }
    public DateTime CreatedAt { get; set; }
}