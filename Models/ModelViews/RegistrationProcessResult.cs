namespace EventSphere.Models.ModelViews
{
   public class RegistrationProcessResult
{
    public int RegistrationId { get; set; }
    public int? AttendanceId { get; set; }        
    public int? WaitlistId { get; set; }          
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateOnly? EventDate { get; set; }
    public TimeOnly? EventTime { get; set; }

    public int StudentId { get; set; }
    public string StudentEmail { get; set; } = "";
    public string StudentName { get; set; } = "";

    public bool IsWaitlisted { get; set; } = false;
    public bool AlreadyProcessed { get; set; } = false;

    public string? Message { get; set; }
}

}
