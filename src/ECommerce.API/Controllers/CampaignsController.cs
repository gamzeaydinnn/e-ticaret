using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CampaignsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private const string ConfigFileName = "campaigns.json";

        public CampaignsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        public IActionResult GetCampaigns()
        {
            var campaigns = LoadCampaigns();
            return Ok(campaigns);
        }

        [HttpGet("{slug}")]
        public IActionResult GetCampaignBySlug(string slug)
        {
            var campaigns = LoadCampaigns();
            var campaign = campaigns.FirstOrDefault(c =>
                string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase));

            if (campaign == null)
            {
                return NotFound(new { message = $"Slug '{slug}' için kampanya bulunamadı." });
            }

            return Ok(campaign);
        }

        private IEnumerable<CampaignDto> LoadCampaigns()
        {
            try
            {
                var configDir = Path.Combine(_env.ContentRootPath, "Config");
                var filePath = Path.Combine(configDir, ConfigFileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return Array.Empty<CampaignDto>();
                }

                var json = System.IO.File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<IEnumerable<CampaignDto>>(json, options)
                       ?? Array.Empty<CampaignDto>();
            }
            catch
            {
                return Array.Empty<CampaignDto>();
            }
        }

        private sealed record CampaignDto
        {
            public int Id { get; init; }
            public string Slug { get; init; } = string.Empty;
            public string Title { get; init; } = string.Empty;
            public string Summary { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string? Image { get; init; }
            public string? Badge { get; init; }
            public string? ValidUntil { get; init; }
            public int? CategoryId { get; init; }
        }
    }
}
