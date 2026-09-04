using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using TechnoZone.Data;
using TechnoZone.Models;

namespace TechnoZone.Controllers
{
    public class HomeController : Controller
    {
        private readonly DatabaseConnection _db;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IConfiguration configuration, ILogger<HomeController> logger)
        {
            _db = new DatabaseConnection(configuration);
            _logger = logger;
        }

        /// <summary>
        /// Called by the newsletter form in wwwroot/js/site.js.
        /// Always answers with JSON so the page never has to reload.
        /// </summary>
        // POST: /Home/Subscribe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Subscribe(string email)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                !Regex.IsMatch(email.Trim(), @"^[^\s@]+@[^\s@]+\.[a-zA-Z]{2,}$"))
            {
                return Json(new { success = false, message = "Enter an email address in the form name@example.com" });
            }

            try
            {
                var added = _db.SubscribeNewsletter(email.Trim());

                return Json(new
                {
                    success = true,
                    message = added
                        ? "You are on the list. Watch your inbox."
                        : "That address is already subscribed."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Newsletter sign-up failed");
                return Json(new { success = false, message = "The sign-up service is unavailable. Try again shortly." });
            }
        }

        public IActionResult Index()
        {
            var model = new HomeViewModel
            {
                TrustFeatures = GetTrustFeatures(),
                Categories = GetCategories(),
                Bestsellers = GetBestsellers(),
                TechFeatures = GetTechFeatures(),
                Partners = GetPartners(),
                Testimonials = GetTestimonials()
            };
            return View(model);
        }

        private static List<TrustFeature> GetTrustFeatures()
        {
            return new List<TrustFeature>
            {
                new() { Id = 1, IconClass = "icon-shield", Title = "3-Year Custom Warranty", Description = "No-hassle parts & labor coverage" },
                new() { Id = 2, IconClass = "icon-wrench", Title = "Tuned & Overclocked", Description = "Professionally calibrated thermal profiles" },
                new() { Id = 3, IconClass = "icon-truck", Title = "Crated Transit Protection", Description = "Shipped in bespoke shockproof timber crates" },
                new() { Id = 4, IconClass = "icon-headset", Title = "Lifetime Support", Description = "Talk directly to our engineers" }
            };
        }

        private static List<Category> GetCategories()
        {
            return new List<Category>
            {
                new() { Id = 1, Title = "Custom Desktops", Description = "Limitless expansion & raw processing capacity.", ImageUrl = "/images/cat-desktop.jpg", CtaText = "EXPLORE LINE" },
                new() { Id = 2, Title = "Pro Laptops", Description = "Desktop caliber silicon on the move.", ImageUrl = "/images/cat-laptop.jpg", CtaText = "EXPLORE LINE" },
                new() { Id = 3, Title = "Gaming Rigs", Description = "Maximum frames, minimum acoustics.", ImageUrl = "/images/cat-gaming.jpg", CtaText = "EXPLORE LINE" },
                new() { Id = 4, Title = "Curated Accessories", Description = "Tactile peripherals of absolute caliber.", ImageUrl = "/images/cat-accessories.jpg", CtaText = "EXPLORE LINE" }
            };
        }

        private static List<Product> GetBestsellers()
        {
            return new List<Product>
            {
                new()
                {
                    Id = 1,
                    Name = "TechnoZone Horizon Pro",
                    Badge = "HOTTEST BUILD",
                    ImageUrl = "/images/product-horizon.jpg",
                    CurrentPrice = 4199m,
                    OriginalPrice = 4499m,
                    Specs = new() { "AMD Ryzen 9 7950X3D", "NVIDIA RTX 4090 24GB", "64GB DDR5 6000MHz" }
                },
                new()
                {
                    Id = 2,
                    Name = "TechnoBook Pro I6",
                    Badge = "PORTABLE BEAST",
                    ImageUrl = "/images/product-nestbook.jpg",
                    CurrentPrice = 2899m,
                    OriginalPrice = null,
                    Specs = new() { "Intel Core i9-14900HX", "NVIDIA RTX 4080 Mobile", "32GB DDR5 5600MHz" }
                },
                new()
                {
                    Id = 3,
                    Name = "Horizon Architect",
                    Badge = "SILENT WORKHORSE",
                    ImageUrl = "/images/product-architect.jpg",
                    CurrentPrice = 6799m,
                    OriginalPrice = null,
                    Specs = new() { "Intel Xeon W5-3425", "NVIDIA RTX 6000 Ada", "128GB ECC DDR5" }
                }
            };
        }

        private static List<TechSpotFeature> GetTechFeatures()
        {
            return new List<TechSpotFeature>
            {
                new() { Id = 1, Title = "Direct-die Liquid Metal Application", Description = "Lowers CPU core temperatures up to 14°C over standard paste." },
                new() { Id = 2, Title = "Custom Dual-chamber Airflow Guidance", Description = "No stagnant hot air pockets around memory or storage pools." },
                new() { Id = 3, Title = "Individually Sleeved Heavy-gauge Cables", Description = "Perfect signal integrity, clean layout routing, optimal airflow." }
            };
        }

        private static List<Partner> GetPartners()
        {
            return new List<Partner>
            {
                new() { Id = 1, Name = "Intel Core Ultra" },
                new() { Id = 2, Name = "AMD Ryzen" },
                new() { Id = 3, Name = "NVIDIA GeForce RTX" },
                new() { Id = 4, Name = "Noctua Silent Systems" },
                new() { Id = 5, Name = "Seasonic Energy" },
                new() { Id = 6, Name = "ASUS ROG Pro" }
            };
        }

        private static List<Testimonial> GetTestimonials()
        {
            return new List<Testimonial>
            {
                new()
                {
                    Id = 1,
                    Quote = "The acoustic performance is wizardry. Rendering heavy 4D architectural frames used to sound like a jet engine. This system remains at a silent whisper, locked at 4.2GHz.",
                    AuthorName = "Marcus Vane",
                    AuthorTitle = "Creative Director at Vane Studios",
                    StarRating = 5
                },
                new()
                {
                    Id = 2,
                    Quote = "Our dev team handles gigantic compile suites. TechnoZone built custom dual-Xeon racks for our workspace. Build times plummeted by 42%. Customer service is unmatched.",
                    AuthorName = "Elena Rostov",
                    AuthorTitle = "Principal Systems Architect, Axiom",
                    StarRating = 5
                },
                new()
                {
                    Id = 3,
                    Quote = "Absolutely beautiful construction. Cable management is art. I had a small question about memory timings, and an actual engineer hopped on a call within 5 minutes to guide me.",
                    AuthorName = "Toby Thorne",
                    AuthorTitle = "Competitive Simulation Pilot",
                    StarRating = 5
                }
            };
        }

        public IActionResult Privacy() => View();
        public IActionResult Error() => View();
    }
}