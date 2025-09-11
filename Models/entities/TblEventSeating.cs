using System;
using System.Collections.Generic;

namespace EventSphere.Models.entities;

public partial class TblEventSeating
{
    public int Id { get; set; }

    public int? EventId { get; set; }

    public int? TotalSeats { get; set; }

    public int? SeatsBooked { get; set; }

    public int? SeatsAvailable { get; set; }

    public bool? WaitlistEnabled { get; set; }

    public virtual TblEvent IdNavigation { get; set; } = null!;
}
