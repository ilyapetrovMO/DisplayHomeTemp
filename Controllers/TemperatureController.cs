using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using DisplayHomeTemp.Models;
using Microsoft.EntityFrameworkCore;
using DisplayHomeTemp.Util;
using WebPush;

namespace DisplayHomeTemp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemperatureController : ControllerBase
    {
        public record TempDto(double temperature, double humidity, string timestamp);

        private readonly TempsDbContext _db;
        private readonly WebPushService _wp;

        private const double LOWTEMP = 9d;

        public TemperatureController(TempsDbContext db, WebPushService webPushService)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> PostTemperature([FromBody] TempDto tempReadingDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var tempReading = new TempReading
            {
                Temp = tempReadingDto.temperature,
                Humidity = tempReadingDto.humidity,
                Time = Convert.ToDateTime(tempReadingDto.timestamp)
            };

            _db.Temps.Add(tempReading);

            if (tempReading.Temp < LOWTEMP)
            {
                var subs = await _db.Subscriptions.ToArrayAsync();

                foreach (var sub in subs)
                {
                    try
                    {
                        await _wp.SendNotification(sub, $"Verbilki low temp alert: {tempReading.Temp} °C");
                    }
                    catch (WebPushException exception)
                    {
                        if (exception.StatusCode == System.Net.HttpStatusCode.NotFound || exception.StatusCode == System.Net.HttpStatusCode.Gone)
                        {
                            _db.Subscriptions.Remove(sub);
                            await _db.SaveChangesAsync();
                        }

                        Console.Error.WriteLine("SendNotification error with status code: " + exception.StatusCode);
                    }
                }

            }

            try
            {
                return Ok(await _db.SaveChangesAsync());
            }
            catch (DbUpdateException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error while creating new temperature reading." });
            }
        }
    }
}