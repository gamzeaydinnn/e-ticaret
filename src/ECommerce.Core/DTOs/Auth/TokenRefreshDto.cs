using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Auth
{
    public class TokenRefreshDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }


}
