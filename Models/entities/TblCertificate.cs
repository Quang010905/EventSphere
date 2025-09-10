using System;
using System.Collections.Generic;

namespace EventSphere.Models.entities;

public partial class TblCertificate
{
    public int Id { get; set; }

    public int? EventId { get; set; }

    public int? StudentId { get; set; }

    public string? CertificateUrl { get; set; }

    public DateTime? IssuedOn { get; set; }

    public virtual TblEvent? Event { get; set; }

    public virtual TblUser? Student { get; set; }
}
