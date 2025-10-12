using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System;

namespace ECommerce.Entities.Concrete
{
    //Migros gibi sitelerde “bugün 16:00–18:00 arası teslimat” seçeneği olur.
public class DeliverySlot : BaseEntity
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAvailable { get; set; } = true;

        public virtual ICollection<Order> Orders { get; set; } = new HashSet<Order>();
    }


}