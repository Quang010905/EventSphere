namespace EventSphere.Models.ModelViews
{
    public class RegistrationView
    {
        public int Id { get; set; } = 0;
        public int StudentId { get; set; } = 0;
        public int EventId {  get; set; } = 0;
        public DateTime RegisterOn { get; set; } = DateTime.Now;
        public int Status {  get; set; } = 0;
        public string EventName { get; set; }
        public string EventImage { get; set; }
        public string Venue { get; set; }
        public DateOnly? EventDate { get; set; }
        public TimeOnly EventTime { get; set; }
         public string StudentEmail { get; set; }
        public virtual EventView Event { get; set; }
        public virtual UserView User{ get; set; }
    }
}
