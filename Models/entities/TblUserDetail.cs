using System;
using System.Collections.Generic;

namespace EventSphere.Models.entities;

public partial class TblUserDetail
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? Fullname { get; set; }

    public string? Department { get; set; }

    public string? Phone { get; set; }

    public string? EnrollmentNo { get; set; }

    public string? Image { get; set; }

    public virtual TblUser? User { get; set; }
}
