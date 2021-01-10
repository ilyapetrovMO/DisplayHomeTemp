using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using DisplayHomeTemp.Models;

namespace DisplayHomeTemp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemperatureController : ControllerBase
    {
        public record TempDto(double temperature, double humidity, string timestamp);
        public TempsDbContext _db { get; set; }

        public TemperatureController(TempsDbContext db)
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

            try
            {
                return Ok(await _db.SaveChangesAsync());
            }
            catch (System.Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}