using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace ECommerce.Data.Repositories
{
    public class ReviewRepository : BaseRepository<ProductReview>, IReviewRepository
    {
        public ReviewRepository(ECommerceDbContext context) : base(context) { }

        public async Task<IEnumerable<ProductReview>> GetByProductIdAsync(int productId)
        {
            return await _dbSet
                .Where(r => r.ProductId == productId && r.IsApproved) // Sadece onaylı yorumları göstermek daha mantıklı.
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<double> GetAverageRatingAsync(int productId)
        {
            var reviews = await _dbSet
                .Where(r => r.ProductId == productId && r.IsApproved) // Ortalama da onaylılardan hesaplanmalı.
                .ToListAsync();
                
            return reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        }

        // DEĞİŞİKLİK: Aşağıdaki tüm NotImplementedException fırlatan hatalı ve gereksiz kod blokları silindi.
        // BaseRepository<ProductReview> sınıfı IRepository<ProductReview> arayüzünün temel metotlarını (GetById, GetAll, Add, Update, Delete)
        // zaten implemente ediyor. Bu yüzden bu kodlara burada gerek yok.
    }
}