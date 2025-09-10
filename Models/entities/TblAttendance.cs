using System;
using System.Collections.Generic;

namespace EventSphere.Models.entities;

public partial class TblAttendance
{
    public int Id { get; set; }

    public int? EventId { get; set; }

    public int? StudentId { get; set; }

    public bool? Attended { get; set; }

    public DateTime? MarkedOn { get; set; }

    public virtual TblEvent? Event { get; set; }

    public virtual TblUser? Student { get; set; }
}
