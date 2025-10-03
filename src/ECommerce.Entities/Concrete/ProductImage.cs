using ECommerce.Entities.Concrete;
namespace ECommerce.Entities.Concrete
{
    public class ProductImage : BaseEntity
    {
        public int ProductId { get; set; }
        public string Url { get; set; }           // public url (S3/Blob)
        public string FileName { get; set; }      // prod_<uuid>_1024.webp
        public bool IsMain { get; set; }
        public string SizeTag { get; set; }       // original / 1024 / 800 / 400 / 200
        public Product Product { get; set; }
    }
}