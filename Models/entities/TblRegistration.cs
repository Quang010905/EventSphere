using System;
using System.Collections.Generic;

namespace EventSphere.Models.entities;

public partial class TblRegistration
{
    public int Id { get; set; }

    public int? EventId { get; set; }

    public int? StudentId { get; set; }

    public DateTime? RegisteredOn { get; set; }

    public int? Status { get; set; }

    public virtual TblEvent? Event { get; set; }

    public virtual TblUser? Student { get; set; }
 
}
