using System.Diagnostics;
using EventSphere.Models.entities;
using EventSphere.Models.ViewModels;
using EventSphere.Models.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace EventSphere.Repositories
{
    public class CertificateRepository : Repository<TblCertificate>
    {
        public CertificateRepository(EventSphereContext context) : base(context) { }

        public async Task<(IEnumerable<TblCertificate> data, int totalCount)> GetPagedCertificatesAsync(
            int pageIndex, int pageSize,
            int? eventId = null, int? studentId = null,
            DateTime? issuedFrom = null, DateTime? issuedTo = null,
            string? keyword = null)
        {
            var query = _dbSet
                .Include(c => c.Event)
                .Include(c => c.Student)
                    .ThenInclude(s => s.TblUserDetails)
                .AsQueryable();

            if (eventId.HasValue)
                query = query.Where(c => c.EventId == eventId.Value);

            if (studentId.HasValue)
                query = query.Where(c => c.StudentId == studentId.Value);

            if (issuedFrom.HasValue)
                query = query.Where(c => c.IssuedOn >= issuedFrom.Value);

            if (issuedTo.HasValue)
                query = query.Where(c => c.IssuedOn <= issuedTo.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(c =>
                    (c.Event != null && EF.Functions.Like(c.Event.Title, $"%{keyword}%")) ||
                    (c.Student != null && c.Student.TblUserDetails.Any(d => EF.Functions.Like(d.Fullname, $"%{keyword}%")))
                );
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(c => c.IssuedOn)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, total);
        }

        public async Task<TblCertificate?> GetByIdWithRelationsAsync(int id)
        {
            return await _dbSet
                .Include(c => c.Event)
                .Include(c => c.Student)
                    .ThenInclude(s => s.TblUserDetails)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
