using System;
using System.Collections.Generic;

namespace EventSphere.Models.entities;

public partial class TblEventWaitlist
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? EventId { get; set; }

    public DateTime? WaitlistTime { get; set; }

    public int? Status { get; set; }

    public virtual TblEvent? Event { get; set; }

    public virtual TblUser? User { get; set; }
}
