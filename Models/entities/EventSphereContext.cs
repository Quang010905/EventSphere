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
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(local);Database=EventSphere;uid=sa;pwd=123456789;Trusted_Connection=True;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblAttendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_atte__DED88B1C0B7B084D");

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
                .HasConstraintName("FK__tbl_atten___even__46E78A0C");

            entity.HasOne(d => d.Student).WithMany(p => p.TblAttendances)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__tbl_atten___stud__47DBAE45");
        });

        modelBuilder.Entity<TblCalendarSync>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_cale__DED88B1CCE77BDEA");

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
                .HasConstraintName("FK__tbl_calen___even__619B8048");

            entity.HasOne(d => d.User).WithMany(p => p.TblCalendarSyncs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__tbl_calen___user__60A75C0F");
        });

        modelBuilder.Entity<TblCertificate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_cert__DED88B1C3F3FABBC");

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
                .HasConstraintName("FK__tbl_certi___even__5070F446");

            entity.HasOne(d => d.Student).WithMany(p => p.TblCertificates)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__tbl_certi___stud__5165187F");
        });

        modelBuilder.Entity<TblEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_even__DED88B1CF3943B98");

            entity.ToTable("tbl_event");

            entity.Property(e => e.Id).HasColumnName("_id");
            entity.Property(e => e.Category)
                .HasMaxLength(250)
                .HasColumnName("_category");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("_date");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("_description");
            entity.Property(e => e.Image)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("_image");
            entity.Property(e => e.OrganizerId).HasColumnName("_organizer_id");
            entity.Property(e => e.Status).HasColumnName("_status");
            entity.Property(e => e.Time)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("_time");
            entity.Property(e => e.Title)
                .HasMaxLength(250)
                .HasColumnName("_title");
            entity.Property(e => e.Venue)
                .HasMaxLength(250)
                .HasColumnName("_venue");

            entity.HasOne(d => d.Organizer).WithMany(p => p.TblEvents)
                .HasForeignKey(d => d.OrganizerId)
                .HasConstraintName("FK__tbl_event___orga__3D5E1FD2");
        });

        modelBuilder.Entity<TblEventSeating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_even__DED88B1CF7280286");

            entity.ToTable("tbl_eventSeating");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("_id");
            entity.Property(e => e.SeatsAvailable).HasColumnName("_seats_available");
            entity.Property(e => e.SeatsBooked).HasColumnName("_seats_booked");
            entity.Property(e => e.TotalSeats).HasColumnName("_total_seats");
            entity.Property(e => e.WaitlistEnabled).HasColumnName("_waitlist_enabled");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.TblEventSeating)
                .HasForeignKey<TblEventSeating>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tbl_eventSe___id__59063A47");
        });

        modelBuilder.Entity<TblEventShareLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_even__DED88B1CB8440688");

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
                .HasConstraintName("FK__tbl_event___even__66603565");

            entity.HasOne(d => d.User).WithMany(p => p.TblEventShareLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__tbl_event___user__656C112C");
        });

        modelBuilder.Entity<TblEventWaitlist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_even__DED88B1CFF851FCA");

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
                .HasConstraintName("FK__tbl_event___even__5CD6CB2B");

            entity.HasOne(d => d.User).WithMany(p => p.TblEventWaitlists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__tbl_event___user__5BE2A6F2");
        });

        modelBuilder.Entity<TblFeedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_feed__DED88B1C747B1511");

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
                .HasConstraintName("FK__tbl_feedb___even__4BAC3F29");

            entity.HasOne(d => d.Student).WithMany(p => p.TblFeedbacks)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__tbl_feedb___stud__4CA06362");
        });

        modelBuilder.Entity<TblMediaGallery>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_medi__DED88B1CB057BD5A");

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
                .HasConstraintName("FK__tbl_media___even__5441852A");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.TblMediaGalleries)
                .HasForeignKey(d => d.UploadedBy)
                .HasConstraintName("FK__tbl_media___uplo__5535A963");
        });

        modelBuilder.Entity<TblRegistration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_regi__DED88B1C9706B73C");

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
                .HasConstraintName("FK__tbl_regis___even__4222D4EF");

            entity.HasOne(d => d.Student).WithMany(p => p.TblRegistrations)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__tbl_regis___stud__4316F928");
        });

        modelBuilder.Entity<TblUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tbl_user__DED88B1CD893E234");

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
            entity.HasKey(e => e.Id).HasName("PK__tbl_user__DED88B1CEC0423E2");

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
