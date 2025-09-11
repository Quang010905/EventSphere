using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace EventSphere.Models.entities;

public partial class EventSphereContext : DbContext
{
    public EventSphereContext()
    {
    }

    public EventSphereContext(DbContextOptions<EventSphereContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblAttendance> TblAttendances { get; set; }

    public virtual DbSet<TblCalendarSync> TblCalendarSyncs { get; set; }

    public virtual DbSet<TblCertificate> TblCertificates { get; set; }

    public virtual DbSet<TblEvent> TblEvents { get; set; }

    public virtual DbSet<TblEventSeating> TblEventSeatings { get; set; }

    public virtual DbSet<TblEventShareLog> TblEventShareLogs { get; set; }

    public virtual DbSet<TblEventWaitlist> TblEventWaitlists { get; set; }

    public virtual DbSet<TblFeedback> TblFeedbacks { get; set; }

    public virtual DbSet<TblMediaGallery> TblMediaGalleries { get; set; }

    public virtual DbSet<TblRegistration> TblRegistrations { get; set; }

    public virtual DbSet<TblUser> TblUsers { get; set; }

    public virtual DbSet<TblUserDetail> TblUserDetails { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?linkid=723263.
        => optionsBuilder.UseSqlServer("Server=(local);Database=EventSphere;uid=sa;pwd=123456789;Trusted_Connection=True;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblAttendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_atte__DED88B1C333E8761");

            entity.ToTable("tbl_attendance");

            entity.Property(e => e.Id).HasColumnName("_id");
            entity.Property(e => e.Attended).HasColumnName("_attended");
            entity.Property(e => e.EventId).HasColumnName("_event_id");
            entity.Property(e => e.MarkedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("_marked_on");
            entity.Property(e => e.StudentId).HasColumnName("_student_id");

            entity.HasOne(d => d.Event).WithMany(p => p.TblAttendances)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__tbl_atten___even__4F7CD00D");

            entity.HasOne(d => d.Student).WithMany(p => p.TblAttendances)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__tbl_atten___stud__5070F446");
        });

        modelBuilder.Entity<TblCalendarSync>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_cale__DED88B1CC4905F2E");

            entity.ToTable("tbl_calendarSync");

            entity.Property(e => e.Id).HasColumnName("_id");
            entity.Property(e => e.CalendarType)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("_calendar_type");
            entity.Property(e => e.CalendarUrl)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("_calendar_url");
            entity.Property(e => e.EventId).HasColumnName("_event_id");
            entity.Property(e => e.SyncTimestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("_sync_timestamp");
            entity.Property(e => e.UserId).HasColumnName("_user_id");

            entity.HasOne(d => d.Event).WithMany(p => p.TblCalendarSyncs)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__tbl_calen___even__6A30C649");

            entity.HasOne(d => d.User).WithMany(p => p.TblCalendarSyncs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__tbl_calen___user__693CA210");
        });

        modelBuilder.Entity<TblCertificate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_cert__DED88B1CCA4A8061");

            entity.ToTable("tbl_certificate");

            entity.Property(e => e.Id).HasColumnName("_id");
            entity.Property(e => e.CertificateUrl)
                .HasMaxLength(255)
                .HasColumnName("_certificate_url");
            entity.Property(e => e.EventId).HasColumnName("_event_id");
            entity.Property(e => e.IssuedOn)
                .HasColumnType("datetime")
                .HasColumnName("_issued_on");
            entity.Property(e => e.StudentId).HasColumnName("_student_id");

            entity.HasOne(d => d.Event).WithMany(p => p.TblCertificates)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__tbl_certi___even__59063A47");

            entity.HasOne(d => d.Student).WithMany(p => p.TblCertificates)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__tbl_certi___stud__59FA5E80");
        });

        modelBuilder.Entity<TblEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_even__DED88B1CA9005906");
            entity.ToTable("tbl_event");

            entity.Property(e => e.Id).HasColumnName("_id");
            entity.Property(e => e.Category).HasMaxLength(250).HasColumnName("_category");
            entity.Property(e => e.Date).HasColumnName("_date");
            entity.Property(e => e.Time).HasColumnName("_time");
            entity.Property(e => e.Description).HasColumnType("text").HasColumnName("_description");
            entity.Property(e => e.Image).HasMaxLength(250).IsUnicode(false).HasColumnName("_image");
            entity.Property(e => e.OrganizerId).HasColumnName("_organizer_id");
            entity.Property(e => e.Status).HasColumnName("_status");
            entity.Property(e => e.Title).HasMaxLength(250).HasColumnName("_title");
            entity.Property(e => e.Venue).HasMaxLength(250).HasColumnName("_venue");

            entity.HasOne(d => d.Organizer)
                  .WithMany(p => p.TblEvents)
                  .HasForeignKey(d => d.OrganizerId)
                  .HasConstraintName("FK__tbl_event___orga__45F365D3");
        });

        // --- Modified mapping for TblEventSeating: map EventId -> _event_id and use IdNavigation as navigation to TblEvent ---
        modelBuilder.Entity<TblEventSeating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_even__DED88B1CB145A37E");

            entity.ToTable("tbl_eventSeating");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("_id");

            // Map the EventId property to the actual DB column _event_id
            entity.Property(e => e.EventId)
                .HasColumnName("_event_id");

            entity.Property(e => e.SeatsAvailable).HasColumnName("_seats_available");
            entity.Property(e => e.SeatsBooked).HasColumnName("_seats_booked");
            entity.Property(e => e.TotalSeats).HasColumnName("_total_seats");
            entity.Property(e => e.WaitlistEnabled).HasColumnName("_waitlist_enabled");

            // Configure relationship between seating and event via EventId -> tbl_event._id
            // Use existing navigation property IdNavigation on TblEventSeating and TblEvent.TblEventSeating
            entity.HasOne(d => d.IdNavigation)
                .WithOne(p => p.TblEventSeating)
                .HasForeignKey<TblEventSeating>(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tbl_eventSe___id__619B8048");
        });

        modelBuilder.Entity<TblEventShareLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_even__DED88B1C4CA01310");

            entity.ToTable("tbl_eventShareLog");

            entity.Property(e => e.Id).HasColumnName("_id");
            entity.Property(e => e.EventId).HasColumnName("_event_id");
            entity.Property(e => e.Platform)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("_platform");
            entity.Property(e => e.ShareMessage)
                .HasColumnType("text")
                .HasColumnName("_share_message");
            entity.Property(e => e.ShareTimestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("_share_timestamp");
            entity.Property(e => e.UserId).HasColumnName("_user_id");

            entity.HasOne(d => d.Event).WithMany(p => p.TblEventShareLogs)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__tbl_event___even__6EF57B66");

            entity.HasOne(d => d.User).WithMany(p => p.TblEventShareLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__tbl_event___user__6E01572D");
        });

        modelBuilder.Entity<TblEventWaitlist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_even__DED88B1CB6F52655");

            entity.ToTable("tbl_eventWaitlist");

            entity.Property(e => e.Id).HasColumnName("_id");
            entity.Property(e => e.EventId).HasColumnName("_event_id");
            entity.Property(e => e.Status).HasColumnName("_status");
            entity.Property(e => e.UserId).HasColumnName("_user_id");
            entity.Property(e => e.WaitlistTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("_waitlist_time");

            entity.HasOne(d => d.Event).WithMany(p => p.TblEventWaitlists)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__tbl_event___even__656C112C");

            entity.HasOne(d => d.User).WithMany(p => p.TblEventWaitlists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__tbl_event___user__6477ECF3");
        });

        modelBuilder.Entity<TblFeedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_feed__DED88B1C1CF2CFBE");

            entity.ToTable("tbl_feedback");

            entity.Property(e => e.Id).HasColumnName("_id");
            entity.Property(e => e.Comments)
                .HasColumnType("text")
                .HasColumnName("_comments");
            entity.Property(e => e.EventId).HasColumnName("_event_id");
            entity.Property(e => e.Rating).HasColumnName("_rating");
            entity.Property(e => e.StudentId).HasColumnName("_student_id");
            entity.Property(e => e.SubmittedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("_submitted_on");

            entity.HasOne(d => d.Event).WithMany(p => p.TblFeedbacks)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__tbl_feedb___even__5441852A");

            entity.HasOne(d => d.Student).WithMany(p => p.TblFeedbacks)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__tbl_feedb___stud__5535A963");
        });

        modelBuilder.Entity<TblMediaGallery>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_medi__DED88B1C272F0ED9");

            entity.ToTable("tbl_mediaGallery");

            entity.Property(e => e.Id).HasColumnName("_id");
            entity.Property(e => e.Caption)
                .HasMaxLength(150)
                .HasColumnName("_caption");
            entity.Property(e => e.EventId).HasColumnName("_event_id");
            entity.Property(e => e.FileType).HasColumnName("_file_type");
            entity.Property(e => e.FileUrl)
                .HasMaxLength(255)
                .HasColumnName("_file_url");
            entity.Property(e => e.UploadedBy).HasColumnName("_uploaded_by");
            entity.Property(e => e.UploadedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("_uploaded_on");

            entity.HasOne(d => d.Event).WithMany(p => p.TblMediaGalleries)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__tbl_media___even__5CD6CB2B");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.TblMediaGalleries)
                .HasForeignKey(d => d.UploadedBy)
                .HasConstraintName("FK__tbl_media___uplo__5DCAEF64");
        });

        modelBuilder.Entity<TblRegistration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_regi__DED88B1CC739A4E5");

            entity.ToTable("tbl_registration");

            entity.Property(e => e.Id).HasColumnName("_id");
            entity.Property(e => e.EventId).HasColumnName("_event_id");
            entity.Property(e => e.RegisteredOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("_registered_on");
            entity.Property(e => e.Status).HasColumnName("_status");
            entity.Property(e => e.StudentId).HasColumnName("_student_id");

            entity.HasOne(d => d.Event).WithMany(p => p.TblRegistrations)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__tbl_regis___even__4AB81AF0");

            entity.HasOne(d => d.Student).WithMany(p => p.TblRegistrations)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__tbl_regis___stud__4BAC3F29");
        });

        modelBuilder.Entity<TblUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_user__DED88B1CC436448E");

            entity.ToTable("tbl_user");

            entity.Property(e => e.Id).HasColumnName("_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("_created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("_email");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("_password");
            entity.Property(e => e.Role).HasColumnName("_role");
            entity.Property(e => e.Status).HasColumnName("_status");
        });

        modelBuilder.Entity<TblUserDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_user__DED88B1C0AE7FE37");

            entity.ToTable("tbl_userDetail");

            entity.Property(e => e.Id).HasColumnName("_id");
            entity.Property(e => e.Department)
                .HasMaxLength(100)
                .HasColumnName("_department");
            entity.Property(e => e.EnrollmentNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("_enrollment_no");
            entity.Property(e => e.Fullname)
                .HasMaxLength(100)
                .HasColumnName("_fullname");
            entity.Property(e => e.Image)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("_image");
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("_phone");
            entity.Property(e => e.UserId).HasColumnName("_user_id");

            entity.HasOne(d => d.User).WithMany(p => p.TblUserDetails)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__tbl_userD___user__3A81B327");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
