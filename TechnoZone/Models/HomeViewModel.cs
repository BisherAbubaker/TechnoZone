namespace TechnoZone.Models
{
    public class HomeViewModel
    {
        public List<Product> Bestsellers { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Testimonial> Testimonials { get; set; } = new();
        public List<Partner> Partners { get; set; } = new();
        public List<TrustFeature> TrustFeatures { get; set; } = new();
        public List<TechSpotFeature> TechFeatures { get; set; } = new();
    }
}