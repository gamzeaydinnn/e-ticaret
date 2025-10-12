using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Micro
{
    public class MicroProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // CS8618 uyarısı giderildi
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}