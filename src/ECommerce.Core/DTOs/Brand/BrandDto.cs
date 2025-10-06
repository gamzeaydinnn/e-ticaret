namespace ECommerce.Core.DTOs.Brand
{
    public class BrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
    }

    public class BrandCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
    }

    public class BrandUpdateDto : BrandCreateDto { }
}
