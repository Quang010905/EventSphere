using EventSphere.Models.entities;


namespace EventSphere.Models.ModelViews
{
    public class ProfileViewModel
    {
        public TblUser? User { get; set; }
        public TblUserDetail? Detail { get; set; }
    }


    public class ProfileEditModel
    {
        public int UserId { get; set; }


        // TblUser fields you want to allow editing
        public string? Email { get; set; }
        public int? Role { get; set; }
        public int? Status { get; set; }


        // TblUserDetail fields
        public string? Fullname { get; set; }
        public string? Department { get; set; }
        public string? EnrollmentNo { get; set; }
        public string? Phone { get; set; }


        // Image upload
        public IFormFile? ImageFile { get; set; }


        // existing image path (hidden field)
        public string? ExistingImage { get; set; }
    }
}