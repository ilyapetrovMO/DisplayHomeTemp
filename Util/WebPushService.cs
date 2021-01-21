using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using WebPush;
using System.Threading.Tasks;
using DisplayHomeTemp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DisplayHomeTemp.Util
{
    public class WebPushService
    {
        private readonly IDbContextFactory<TempsDbContext> _db;
        private readonly IMemoryCache _mc;
        private readonly ILogger<WebPushService> _logger;
        private readonly Dictionary<string, object> _options;
        private readonly TimeSpan NotificationCooldown = TimeSpan.FromHours(8);

        public WebPushClient WebPushClient { get; init; }
        public string VapidPublicKey { get; init; }
        private string VapidPrivateKey { get; init; }
        public DateTime? LastSentUtc { get; private set; }

        private bool IsOnCooldown()
        {
            if (_mc.TryGetValue("LastSentUtc", out DateTime? lastSentUtc))
            {
                LastSentUtc = lastSentUtc;
            }

            return (LastSentUtc != null && ((DateTime.UtcNow - LastSentUtc) < NotificationCooldown));
        }

        public WebPushService(IConfiguration config, IDbContextFactory<TempsDbContext> db, IMemoryCache mc, ILogger<WebPushService> logger)
        {
            _logger = logger;
            _db = db;
            _mc = mc;

            WebPushClient = new WebPushClient();
            _options = new Dictionary<string, object>();

            var vapidPrivate = Environment.GetEnvironmentVariable("VAPID_PRIVATE_KEY");
            vapidPrivate = string.IsNullOrEmpty(config["VapidDetails:VapidPrivate"]) ? vapidPrivate : config["VapidDetails:VapidPrivate"];

            var vapidPublic = Environment.GetEnvironmentVariable("VAPID_PUBLIC_KEY");
            vapidPublic = string.IsNullOrEmpty(config["VapidDetails:VapidPublic"]) ? vapidPublic : config["VapidDetails:VapidPublic"];

            var vapidSubject = Environment.GetEnvironmentVariable("VAPID_SUBJECT");
            vapidSubject = string.IsNullOrEmpty(config["VapidDetails:VapidSubject"]) ? vapidSubject : config["VapidDetails:VapidSubject"];

            if (string.IsNullOrEmpty(vapidPrivate) || string.IsNullOrEmpty(vapidPublic) )
            {
                _logger.LogCritical("Vapid keys not in env or application.json");
                throw new VapidDetailsNotDefinedException("One or more Vapid keys were empty or null.");
            }
            else if (string.IsNullOrEmpty(vapidSubject))
            {
                _logger.LogCritical("Vapid subject not in env or application.json");
                throw new VapidDetailsNotDefinedException("Vapid subject not in config or env variable.");
            }
            else
            {
                _options["vapidDetails"] = new VapidDetails(vapidSubject, vapidPublic, vapidPrivate);
                _options["TTL"] = 60 * 60; //in seconds
                VapidPublicKey = vapidPublic;
                VapidPrivateKey = vapidPrivate;
            }
        }

        public async Task SendNotificationToAll(string payload, bool immediate = false)
        {
            if (!immediate && IsOnCooldown())
                return;

            using (var dbContext = _db.CreateDbContext())
            {
                var subs = await dbContext.Subscriptions.ToArrayAsync();

                foreach (var sub in subs)
                {
                    try
                    {
                        await WebPushClient.SendNotificationAsync(sub, payload, _options);
                    }
                    catch (WebPushException exception)
                    {
                        if (exception.StatusCode == System.Net.HttpStatusCode.NotFound || exception.StatusCode == System.Net.HttpStatusCode.Gone)
                        {
                            dbContext.Subscriptions.Remove(sub);
                            await dbContext.SaveChangesAsync();
                        }

                        _logger.LogInformation($"SendNotification error with status code: {exception.StatusCode}, deleting subscription.");
                    }
                }
            }

            _logger.LogInformation($"Last sent notification: {DateTime.UtcNow} (UTC)");
            _mc.Set("LastSentUtc", DateTime.UtcNow);
        }

        public async Task TestNotifications(string payload, string vapidPrivateKey)
        {
            if (vapidPrivateKey != VapidPrivateKey)
                throw new WrongVapidPrivateKeyException("Wrong VAPID private key.");

            using var dbContext = _db.CreateDbContext();
            var subs = await dbContext.Subscriptions.ToArrayAsync();

            foreach (var sub in subs)
            {
                await WebPushClient.SendNotificationAsync(sub, payload + $" Last sent: {LastSentUtc}", _options);
            }
        }
    }
}
