namespace EventSphere.Models.ModelViews
{
    public class UserView
    {
        public int Id { get; set; } = 0;
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public int Role { get; set; } = 1;
        public int Status { get; set; } = 1;
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public virtual UserDetailView UserDetail { get; set; }
    }
}
