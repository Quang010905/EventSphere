using System;
using System.Collections.Generic;

namespace EventSphere.Models.entities;

public partial class TblFeedback
{
    public int Id { get; set; }

    public int? EventId { get; set; }

    public int? StudentId { get; set; }

    public int? Rating { get; set; }

    public string? Comments { get; set; }

    public DateTime? SubmittedOn { get; set; }

    public virtual TblEvent? Event { get; set; }

    public virtual TblUser? Student { get; set; }
}
