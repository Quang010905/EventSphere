using System.Collections.Generic;
using EventSphere.Models.entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventSphere.Models.ViewModels
{
    public class CertificatesIndexViewModel
    {
        public IEnumerable<TblCertificate> Certificates { get; set; } = new List<TblCertificate>();

        // Dropdowns
        public SelectList? EventList { get; set; }
        public SelectList? StudentList { get; set; }

        // Filters
        public int? EventId { get; set; }
        public int? StudentId { get; set; }
        public string? Keyword { get; set; }

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)System.Math.Ceiling((double)TotalCount / PageSize);
    }

    public class CertificateGenerateViewModel
    {
        public int? EventId { get; set; }
        public int? StudentId { get; set; }

        public SelectList? EventList { get; set; }
        public SelectList? StudentList { get; set; }
    }
}
