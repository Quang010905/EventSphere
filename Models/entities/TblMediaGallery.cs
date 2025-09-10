using System;
using System.Collections.Generic;

namespace EventSphere.Models.entities;

public partial class TblMediaGallery
{
    public int Id { get; set; }

    public int? EventId { get; set; }

    public int? FileType { get; set; }

    public string? FileUrl { get; set; }

    public int? UploadedBy { get; set; }

    public string? Caption { get; set; }

    public DateTime? UploadedOn { get; set; }

    public virtual TblEvent? Event { get; set; }

    public virtual TblUser? UploadedByNavigation { get; set; }
}
