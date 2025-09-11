using System.Threading.Tasks;
using EventSphere.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EventSphere.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShareLogController : ControllerBase
    {
        private readonly EventShareLogRepository _repo;

        public ShareLogController(EventShareLogRepository repo)
        {
            _repo = repo;
        }

        public class ShareRequest
        {
            public int UserId { get; set; }
            public int EventId { get; set; }
            public string Platform { get; set; } = "";
            public string? Message { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ShareRequest req)
        {
            if (req == null || req.UserId <= 0 || req.EventId <= 0 || string.IsNullOrWhiteSpace(req.Platform))
                return BadRequest(new { success = false, message = "Invalid payload" });

            var ent = await _repo.AddAsync(req.UserId, req.EventId, req.Platform, req.Message);
            return Ok(new { success = true, id = ent.Id });
        }
    }
}
