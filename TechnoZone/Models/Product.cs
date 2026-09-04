namespace TechnoZone.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Specs { get; set; } = new();
        public decimal CurrentPrice { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string CtaText { get; set; } = "CONFIGURE";
    }
}