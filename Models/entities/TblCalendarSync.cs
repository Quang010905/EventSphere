using System;
using System.Collections.Generic;

namespace EventSphere.Models.entities;

public partial class TblCalendarSync
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? EventId { get; set; }

    public string? CalendarType { get; set; }

    public DateTime? SyncTimestamp { get; set; }

    public string? CalendarUrl { get; set; }

    public virtual TblEvent? Event { get; set; }

    public virtual TblUser? User { get; set; }
}
