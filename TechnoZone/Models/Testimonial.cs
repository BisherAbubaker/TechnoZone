namespace TechnoZone.Models
{
    public class Testimonial
    {
        public int Id { get; set; }
        public string Quote { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorTitle { get; set; } = string.Empty;
        public int StarRating { get; set; } = 5;
    }
}