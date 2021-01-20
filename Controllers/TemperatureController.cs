using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using DisplayHomeTemp.Models;
using Microsoft.EntityFrameworkCore;
using DisplayHomeTemp.Util;

namespace DisplayHomeTemp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemperatureController : ControllerBase
    {
        private readonly IDbContextFactory<TempsDbContext> _db;
        private readonly WebPushService _wp;
        private const double LOWTEMP = 10d;

        public record TempDto(double Temperature, double Humidity, string Timestamp);

        public TemperatureController(IDbContextFactory<TempsDbContext> db, WebPushService wp)
        {
            _db = db;
            _wp = wp;
        }

        [HttpPost]
        public async Task<IActionResult> PostTemperature(TempDto tempReadingDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var tempReading = new TempReading
            {
                Temp = tempReadingDto.Temperature,
                Humidity = tempReadingDto.Humidity,
                Time = Convert.ToDateTime(tempReadingDto.Timestamp)
            };

            using var dbContext = _db.CreateDbContext();

            dbContext.Temps.Add(tempReading);

            if (tempReading.Temp < LOWTEMP)
            {
                _ = _wp.SendNotificationToAll($"Low temp: {tempReading.Temp}");
            }

            try
            {
                return Ok(await dbContext.SaveChangesAsync());
            }
            catch (DbUpdateException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error while creating new temperature reading." });
            }
        }
    }
}