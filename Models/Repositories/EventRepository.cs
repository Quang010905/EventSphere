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

                int idItem = id;
                var q = db.TblEvents.Where(d => d.Id == idItem).Select(d => new EventView
                {
                    Id = d.Id,
                    Title = d.Title,
                    Description = d.Description,
                    Venue = d.Venue,
                    Category = d.Category,
                    OrganizerId = d.OrganizerId ??0,
                    Time = (TimeOnly)d.Time,
                    Date = (DateOnly)d.Date,
                    Image = d.Image,
                    status = d.Status ?? 0
                }).FirstOrDefault();
                if (q != null)
                {
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
