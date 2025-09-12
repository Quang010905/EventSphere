using EventSphere.Models.entities;
using EventSphere.Models.ModelViews;
using System.Runtime.Intrinsics.Arm;

namespace EventSphere.Models.Repositories
{
    public class RegistrationRepository
    {
        private static RegistrationRepository _instance = null;
        private RegistrationRepository() { }
        public static RegistrationRepository Instance
        {
            get
            {
                _instance = _instance ?? new RegistrationRepository();
                return _instance;
            }
        }
        public void Add(RegistrationView entity)
        {
            var db = new EventSphereContext();
            try
            {
                var item = new TblRegistration
                {
                    StudentId = entity.StudentId,
                    EventId = entity.EventId,
                    RegisteredOn = entity.RegisterOn,
                    Status = entity.Status,
                };
                db.TblRegistrations.Add(item);
                db.SaveChanges();
            }
            catch (Exception)
            {

                throw;
            }
        }
        public RegistrationView GetRegistrationByStuId(int id)
        {
            var db = new EventSphereContext();
            var reg = new RegistrationView();
            try
            {
                var idItem = id;
                var q = db.TblRegistrations.Where(r => r.StudentId == id).OrderByDescending(r => r.Id).Select(r => new RegistrationView
                {
                    EventId = r.EventId ?? 0,
                    StudentId = id,
                    Status = r.Status ?? 0,
                    RegisterOn = (DateTime)r.RegisteredOn,
                    EventName = r.Event.Title,
                    EventImage = r.Event.Image,
                    EventDate = (DateOnly)r.Event.Date,
                    EventTime = (TimeOnly)r.Event.Time
                }).FirstOrDefault();
                if (q != null)
                {
                    reg = q;
                }
            }
            catch (Exception)
            {

                throw;
            }
            return reg;
        }
    }
}
