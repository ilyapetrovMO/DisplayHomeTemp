using System;

namespace DisplayHomeTemp.Models
{
    public class TempReading
    {
        public int Id { get; set; }

        public double Temp { get; set; }
        
        public double Humidity { get; set; }

        public DateTime Time { get; set; } = new DateTime();
    }
}