using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using DisplayHomeTemp.Models;
using System.Linq;
using DisplayHomeTemp.Util;
using Microsoft.EntityFrameworkCore;

namespace DisplayHomeTemp.Controllers
{
    [ApiController]
    public class WebPushController : ControllerBase
    {
        private readonly WebPushService _wp;
        private readonly IDbContextFactory<TempsDbContext> _db;

        public WebPushController(WebPushService pushService, IDbContextFactory<TempsDbContext> dbContext)
        {
            _wp = pushService;
            _db = dbContext;
        }

        [HttpPost("api/[controller]/isSubscriptionActive")]
        public async Task<IActionResult> IsSubscriptionActive(WebPushSubscriptionJSON subscriptionDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            using var dbContext = _db.CreateDbContext();
            var sub = await dbContext.Subscriptions.Where(s => s.Endpoint == subscriptionDTO.Endpoint).FirstOrDefaultAsync();

            if(string.IsNullOrEmpty(sub.Endpoint))
            {
                return StatusCode(StatusCodes.Status410Gone, new { message = "subscription was not found" });
            }
            else
            {
                return Ok();
            }
        }


        [HttpGet("api/[controller]/vapidPublicKey")]
        public IActionResult GetVapidPublicKey()
        {
            string vapidPublic = _wp.VapidPublicKey;

            if (string.IsNullOrEmpty(vapidPublic))
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return new JsonResult(new { VapidPublicKey = vapidPublic });
        }

        [HttpPost("api/[controller]/register")]
        public async Task<IActionResult> RegisterNewSubscription(WebPushSubscriptionJSON subscriptionDTO)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest();
            }

            var subscription = new WebPushSubscription
            {
                Endpoint = subscriptionDTO.Endpoint,
                ExpirationTime = subscriptionDTO.ExpirationTime ?? -1,
                P256DH = subscriptionDTO.Keys.P256dh,
                Auth = subscriptionDTO.Keys.Auth
            };

            using var dbContext = _db.CreateDbContext();
            dbContext.Subscriptions.Add(subscription);

            try
            {
                return Ok(await dbContext.SaveChangesAsync());
            }
            catch (DbUpdateException)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Error while creating new temperature reading." }
                );
            }
        }

        public sealed record NotificationTestJSON(string VapidPrivateKey, string Body);

        [HttpPost("api/[controller]/testnotify")]
        public async Task<IActionResult> NotificationTest([FromBody]NotificationTestJSON testNotification)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                await _wp.TestNotifications(testNotification.Body, testNotification.VapidPrivateKey);
            }
            catch (WrongVapidPrivateKeyException e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }

        [HttpDelete("api/[controller]/delete")]
        public async Task<IActionResult> DeleteSubscription(WebPushSubscriptionJSON subscriptionDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            using var dbContext = _db.CreateDbContext();
            WebPushSubscription sub = await dbContext.Subscriptions
                .Where(s => s.Endpoint == subscriptionDTO.Endpoint)
                .FirstOrDefaultAsync();

            if (sub == null)
            {
                return BadRequest("A subscription with the specified Endpoint does not exist.");
            }

            dbContext.Remove(sub);
            
            return Ok(await dbContext.SaveChangesAsync());
        }
    }
}
