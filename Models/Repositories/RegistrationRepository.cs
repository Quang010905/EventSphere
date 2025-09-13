using EventSphere.Models.entities;
using EventSphere.Models.ModelViews;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Runtime.Intrinsics.Arm;
using System.Text;

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
        public List<RegistrationView> GetAll()
        {
            var db = new EventSphereContext();
            var ls = new List<RegistrationView>();
            try
            {
                ls = db.TblRegistrations
                       .Include(x => x.Event)   // load dữ liệu Event
                       .Include(x => x.Student)    // load dữ liệu User trực tiếp
                       .Select(x => new RegistrationView
                       {
                           Id = x.Id,
                           EventId = x.EventId ?? 0,
                           Status = x.Status ?? 0,
                           Venue = x.Event.Venue,
                           EventImage = x.Event.Image,
                           EventName = x.Event.Title,
                           EventDate = x.Event.Date,
                           EventTime = (TimeOnly)x.Event.Time,
                           StudentEmail = x.Student.Email  // email trực tiếp từ User
                       })
                       .ToList();
            }
            catch (Exception)
            {
                throw;
            }
            return ls;
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
                    RegisteredOn = DateTime.Now,
                    Status = 0,
                };
                db.TblRegistrations.Add(item);
                db.SaveChanges();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool Delete(int eventId, int userId)
        {
            using (var db = new EventSphereContext())
            {
                var item = db.TblRegistrations
                             .FirstOrDefault(r => r.EventId == eventId && r.StudentId == userId);

                if (item != null)
                {
                    db.TblRegistrations.Remove(item);
                    return db.SaveChanges() > 0;
                }
            }
            return false;
        }

        public int? GetRegistrationStatus(int studentId, int eventId)
        {
            using (var db = new EventSphereContext())
            {
                var registration = db.TblRegistrations
                                     .FirstOrDefault(r => r.StudentId == studentId && r.EventId == eventId);
                if (registration != null)
                {
                    return registration.Status;
                }
                else
                {
                    return null;
                }
            }
        }



        public bool CheckRegistered(int stuId, int eventId)
        {
            var db = new EventSphereContext();
            return db.TblRegistrations
                     .Any(r => r.StudentId == stuId && r.EventId == eventId);
        }

        public List<RegistrationView> GetRegistrationByStuId(int? id)
        {
            var db = new EventSphereContext();
            var ls = new List<RegistrationView>();
            try
            {
                ls = db.TblRegistrations.Where(x => x.StudentId == id).Include(x => x.Event).Select(x => new RegistrationView
                {
                    Id = x.Id,
                    EventId = x.EventId ??0,
                    Status = x.Status?? 0,
                    Venue = x.Event.Venue,
                    EventImage = x.Event.Image,
                    EventName = x.Event.Title,
                    EventDate = x.Event.Date,
                    EventTime = (TimeOnly)x.Event.Time,
                }).ToList();
            }
            catch (Exception)
            {

                throw;
            }
            return ls;
        }
        public string NormalizeSearch(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";


            string lower = input.ToLowerInvariant();

            // 2. Dùng FormD để tách dấu ra khỏi chữ
            string normalized = lower.Normalize(NormalizationForm.FormD);

            // 3. Loại bỏ các ký tự dấu (non-spacing mark)
            StringBuilder sb = new StringBuilder();
            foreach (char c in normalized)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return new string(sb.ToString()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }
    }
}
