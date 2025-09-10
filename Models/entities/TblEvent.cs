using System;
using System.Collections.Generic;

namespace EventSphere.Models.entities;

public partial class TblEvent
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Venue { get; set; }

    public string? Category { get; set; }

    public int? OrganizerId { get; set; }

    public DateOnly? Date { get; set; }

    public TimeOnly? Time { get; set; }

    public int? Status { get; set; }

    public string? Image { get; set; }

    public virtual TblUser? Organizer { get; set; }

    public virtual ICollection<TblAttendance> TblAttendances { get; set; } = new List<TblAttendance>();

    public virtual ICollection<TblCalendarSync> TblCalendarSyncs { get; set; } = new List<TblCalendarSync>();

    public virtual ICollection<TblCertificate> TblCertificates { get; set; } = new List<TblCertificate>();

    public virtual TblEventSeating? TblEventSeating { get; set; }

    public virtual ICollection<TblEventShareLog> TblEventShareLogs { get; set; } = new List<TblEventShareLog>();

    public virtual ICollection<TblEventWaitlist> TblEventWaitlists { get; set; } = new List<TblEventWaitlist>();

    public virtual ICollection<TblFeedback> TblFeedbacks { get; set; } = new List<TblFeedback>();

    public virtual ICollection<TblMediaGallery> TblMediaGalleries { get; set; } = new List<TblMediaGallery>();

    public virtual ICollection<TblRegistration> TblRegistrations { get; set; } = new List<TblRegistration>();
}
