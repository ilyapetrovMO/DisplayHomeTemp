using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using DisplayHomeTemp.Models;

namespace DisplayHomeTemp.Pages
{
    public class IndexModel : PageModel
    {
        public TempReading[] Temps { get; set; }
        private readonly ILogger<IndexModel> _logger;

        private readonly IDbContextFactory<TempsDbContext> _db;

        public IndexModel(ILogger<IndexModel> logger, IDbContextFactory<TempsDbContext> db)
        {
            _db = db;
            _logger = logger;
        }

        public async Task OnGet()
        {
            using var dbContext = _db.CreateDbContext();
            Temps = await (from t in dbContext.Temps
                    orderby t.Time
                    select t).OrderByDescending(t => t.Time).Take(10).ToArrayAsync();
        }
    }
}
