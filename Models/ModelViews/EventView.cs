namespace EventSphere.Models.ModelViews
{
    public class EventView
    {
        public int Id { get; set; } = 0;
        public string Title { get; set; } = "";
        public string Description { get; set; }
        public string Venue { get; set; } = "";
        public string Category { get; set; } = "";
        public int OrganizerId { get; set; } = 0;
        public DateOnly? Date {  get; set; } 
        public TimeOnly? Time { get; set; }
        public string Image { get; set; } = "";
        public int status { get; set; } = 0;
        public string OrganizerName { get; set; } = ""; 
        public string OrganizerEmail { get; set; } = "";
    }
}
