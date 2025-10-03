//teslimat ataması.
//Task<int> GetCourierCountAsync();
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface ICourierService
    {
        // Tüm kuryeleri listele
        Task<IEnumerable<Courier>> GetAllAsync();

        // Belirli bir kurye getir
        Task<Courier?> GetByIdAsync(int id);

        // Yeni kurye ekle
        Task AddAsync(Courier courier);

        // Kurye güncelle
        Task UpdateAsync(Courier courier);

        // Kurye sil
        Task DeleteAsync(Courier courier);

        // Toplam kurye sayısını döndür
        Task<int> GetCourierCountAsync();
        
    }
}
