using EventSphere.Models.entities;
using EventSphere.Models.ModelViews;

namespace EventSphere.Models.Repositories
{
    public class EventRepository
    {
        private static EventRepository _instance = null;
        private EventRepository() { }
        public static EventRepository Instance
        {
            get
            {
                _instance = _instance ?? new EventRepository();
                return _instance;
            }
        }
        public EventView FindById(int id)
        {
            var db = new EventSphereContext();
            var eve = new EventView();

            try
            {
                // Lấy event
                var q = db.TblEvents
                    .Where(d => d.Id == id)
                    .Select(d => new EventView
                    {
                        Id = d.Id,
                        Title = d.Title,
                        Description = d.Description,
                        Venue = d.Venue,
                        Category = d.Category,
                        OrganizerId = d.OrganizerId ?? 0,
                        Time = (TimeOnly)d.Time,
                        Date = (DateOnly)d.Date,
                        Image = d.Image,
                        status = d.Status ?? 0
                    })
                    .FirstOrDefault();

                if (q != null)
                {
                    var user = db.TblUsers.FirstOrDefault(us => us.Id == q.OrganizerId);
                    if (user != null)
                    {
                        q.OrganizerEmail = user.Email;
                    }
                    // Query thêm để lấy UserDetail
                    var userDetail = db.TblUserDetails
                                       .FirstOrDefault(u => u.UserId == q.OrganizerId);

                    if (userDetail != null)
                    {
                        q.OrganizerName = userDetail.Fullname;
                    }

                    eve = q;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return eve;
        }
    }
}
