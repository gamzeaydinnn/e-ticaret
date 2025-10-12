using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System;

namespace ECommerce.Entities.Concrete
{
public class Courier
{
    public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
}
}