using System;
using System.Collections.Generic;

namespace EventSphere.Models.entities;

public partial class TblEventShareLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? EventId { get; set; }

    public string? Platform { get; set; }

    public DateTime? ShareTimestamp { get; set; }

    public string? ShareMessage { get; set; }

    public virtual TblEvent? Event { get; set; }

    public virtual TblUser? User { get; set; }
}
