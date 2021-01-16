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

        [HttpGet]
        [Route("api/[controller]/vapidPublicKey")]
        public IActionResult GetVapidPublicKey()
        {
            string vapidPublic = _wp.VapidPublicKey;

            if (string.IsNullOrEmpty(vapidPublic))
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return new JsonResult(new { VapidPublicKey = vapidPublic });
        }

        [HttpPost]
        [Route("api/[controller]/register")]
        public async Task<IActionResult> RegisterNewSubscription(WebPushSubscriptionJSON subscriptionDTO)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest();
            }

            var subscription = new WebPushSubscription
            {
                Endpoint = subscriptionDTO.Endpoint,
                ExpirationTime = subscriptionDTO.ExpirationTime,
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

        //[HttpGet]
        //[Route("api/[controller]/notify")]
        //public IActionResult SendTestNotification()
        //{
        //    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        //    {
        //        return NotFound();
        //    }

        //}

        [HttpDelete]
        [Route("api/[controller]/delete")]
        public async Task<IActionResult> DeleteSubscription(string Endpoint)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            WebPushSubscription sub = await _db.Subscriptions
                .Where(s => s.Endpoint == Endpoint)
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
