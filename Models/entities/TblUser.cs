using System;
using System.Collections.Generic;

namespace EventSphere.Models.entities;

public partial class TblUser
{
    public int Id { get; set; }

    public string? Password { get; set; }

    public string? Email { get; set; }

    public int? Role { get; set; }

    public int? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<TblAttendance> TblAttendances { get; set; } = new List<TblAttendance>();

    public virtual ICollection<TblCalendarSync> TblCalendarSyncs { get; set; } = new List<TblCalendarSync>();

    public virtual ICollection<TblCertificate> TblCertificates { get; set; } = new List<TblCertificate>();

    public virtual ICollection<TblEventShareLog> TblEventShareLogs { get; set; } = new List<TblEventShareLog>();

    public virtual ICollection<TblEventWaitlist> TblEventWaitlists { get; set; } = new List<TblEventWaitlist>();

    public virtual ICollection<TblEvent> TblEvents { get; set; } = new List<TblEvent>();

    public virtual ICollection<TblFeedback> TblFeedbacks { get; set; } = new List<TblFeedback>();

    public virtual ICollection<TblMediaGallery> TblMediaGalleries { get; set; } = new List<TblMediaGallery>();

    public virtual ICollection<TblRegistration> TblRegistrations { get; set; } = new List<TblRegistration>();

    public virtual ICollection<TblUserDetail>? TblUserDetails { get; set; } = new List<TblUserDetail>();
}
