namespace EventSphere.Models.ModelViews
{
    public class FeedbackView
    {
        public int Id { get; set; } = 0;
        public int EventId { get; set; } = 0;
        public int StudentId { get; set; } = 0; 
        public int Rating { get; set; } = 0;
        public string Comments { get; set; } = "";
        public DateTime SubmittedOn { get; set; } = DateTime.Now;
        public int Status { get; set; } = 0;
    }
}
