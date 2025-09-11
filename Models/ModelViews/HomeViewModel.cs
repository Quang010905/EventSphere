using EventSphere.Models.entities;

namespace EventSphere.Models.ModelViews
{
    public class HomeViewModel
    {
        public IEnumerable<TblEvent> UpcomingEvents { get; set; } = new List<TblEvent>();
        public IEnumerable<TblMediaGallery> LatestMedia { get; set; } = new List<TblMediaGallery>();
    }
}
