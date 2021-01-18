using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
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
        private readonly TempsDbContext _db;

        public WebPushController(WebPushService pushService, TempsDbContext dbContext)
        {
            _wp = pushService;
            _db = dbContext;
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
        public async Task<IActionResult> RegisterNewSubscription([Bind("expirationTime, keys, endpoint")]WebPushSubscriptionJSON subscriptionDTO)
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

            _db.Subscriptions.Add(subscription);

            try
            {
                return Ok(await _db.SaveChangesAsync());
            }
            catch (DbUpdateException)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Error while creating new temperature reading." }
                );
            }
        }

        public sealed record VapidPrivateKeyJSON(string VapidPrivateKey);

        [HttpPost("api/[controller]/testnotify")]
        public async Task<IActionResult> NotificationTest([FromBody]VapidPrivateKeyJSON vapidPrivateKey)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (vapidPrivateKey.VapidPrivateKey != Environment.GetEnvironmentVariable("VAPID_PRIVATE_KEY"))
            {
                Console.Error.WriteLine(Environment.GetEnvironmentVariable("VAPID_PRIVATE_KEY"));
                return BadRequest("wrong vapid key");
            }

            var subs = await _db.Subscriptions.ToArrayAsync();

            foreach (var sub in subs)
            {
                await _wp.SendNotificationImmediate(sub, $"Test Notification at {DateTime.UtcNow}");
            }

            return Ok();
        }

        [HttpDelete("api/[controller]/delete")]
        public async Task<IActionResult> DeleteSubscription([Bind("expirationTime, keys, endpoint")]WebPushSubscriptionJSON subscriptionDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            WebPushSubscription sub = await _db.Subscriptions
                .Where(s => s.Endpoint == subscriptionDTO.Endpoint)
                .FirstOrDefaultAsync();

            if (sub == null)
            {
                return BadRequest("A subscription with the specified Endpoint does not exist.");
            }

            _db.Remove(sub);
            
            return Ok(await _db.SaveChangesAsync());
        }
    }
}
