using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Data.Repositories
{
    public class WeightReportRepository : BaseRepository<WeightReport>, IWeightReportRepository
    {
        public WeightReportRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<WeightReport?> GetByExternalReportIdAsync(string externalReportId)
        {
            return await _context.WeightReports
                .Include(wr => wr.Order)
                .Include(wr => wr.OrderItem)
                .FirstOrDefaultAsync(wr => wr.ExternalReportId == externalReportId);
        }

        public async Task<IEnumerable<WeightReport>> GetByOrderIdAsync(int orderId)
        {
            return await _context.WeightReports
                .Include(wr => wr.Order)
                .Include(wr => wr.OrderItem)
                .Include(wr => wr.ApprovedBy)
                .Where(wr => wr.OrderId == orderId)
                .OrderByDescending(wr => wr.ReceivedAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<WeightReport> Reports, int TotalCount)> GetByStatusAsync(
            WeightReportStatus status,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.WeightReports
                .Include(wr => wr.Order)
                .Include(wr => wr.OrderItem)
                .Include(wr => wr.ApprovedBy)
                .Where(wr => wr.Status == status);

            var totalCount = await query.CountAsync();

            var reports = await query
                .OrderByDescending(wr => wr.ReceivedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (reports, totalCount);
        }

        public async Task<IEnumerable<WeightReport>> GetPendingReportsAsync()
        {
            return await _context.WeightReports
                .Include(wr => wr.Order)
                .Include(wr => wr.OrderItem)
                .Where(wr => wr.Status == WeightReportStatus.Pending)
                .OrderBy(wr => wr.ReceivedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateStatusAsync(int reportId, WeightReportStatus newStatus)
        {
            var report = await _context.WeightReports.FindAsync(reportId);
            if (report == null) return false;

            report.Status = newStatus;
            report.ProcessedAt = DateTimeOffset.UtcNow;
            report.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<WeightReportStats> GetStatsAsync()
        {
            var reports = await _context.WeightReports.ToListAsync();

            return new WeightReportStats
            {
                TotalReports = reports.Count,
                PendingCount = reports.Count(r => r.Status == WeightReportStatus.Pending),
                ApprovedCount = reports.Count(r => r.Status == WeightReportStatus.Approved || r.Status == WeightReportStatus.AutoApproved),
                RejectedCount = reports.Count(r => r.Status == WeightReportStatus.Rejected),
                ChargedCount = reports.Count(r => r.Status == WeightReportStatus.Charged),
                FailedCount = reports.Count(r => r.Status == WeightReportStatus.Failed),
                TotalOverageAmount = reports.Sum(r => r.OverageAmount),
                TotalChargedAmount = reports.Where(r => r.Status == WeightReportStatus.Charged).Sum(r => r.OverageAmount)
            };
        }
    }
}
