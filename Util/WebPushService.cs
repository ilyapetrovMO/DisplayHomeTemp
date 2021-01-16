using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using WebPush;
using System.Threading.Tasks;

namespace DisplayHomeTemp.Util
{
    public class WebPushService
    {
        public WebPushClient WebPushClient { get; init; }
        public string VapidPublicKey { get; set; }

        private readonly Dictionary<string, object> options;
        private readonly TimeSpan MinTimeBetweenNotifications = TimeSpan.FromHours(12);
        private DateTime? LastSentUtc = null;

        public WebPushService(IConfiguration config)
        {
            WebPushClient = new WebPushClient();
            options = new Dictionary<string, object>();
            DateTime.UtcNow.AddHours(12);

            var vapidPrivate = Environment.GetEnvironmentVariable("VapidPrivateKey");
            var vapidPublic = Environment.GetEnvironmentVariable("VapidPublicKey");

            string vapidSubject = Environment.GetEnvironmentVariable("VapidSubject");
            vapidSubject = string.IsNullOrEmpty(config["VapidSubject"]) ? vapidSubject : config["VapidSubject"];

            if (string.IsNullOrEmpty(vapidPrivate) || string.IsNullOrEmpty(vapidPublic) )
            {
                throw new VapidDetailsNotDefinedException("One or more Vapid keys were empty or null.");
            }
            else if (string.IsNullOrEmpty(vapidSubject))
            {
                throw new VapidDetailsNotDefinedException("Vapid subject not in config or env variable.");
            }
            else
            {
                options["VapidDetails"] = new VapidDetails(vapidSubject, vapidPublic, vapidPrivate);
                VapidPublicKey = vapidPublic;
            }
        }

        public async Task SendNotification(PushSubscription subscription, string payload)
        {
            if (LastSentUtc != null && ((DateTime.UtcNow - LastSentUtc) < MinTimeBetweenNotifications))
            {
                return;
            }

            await WebPushClient.SendNotificationAsync(subscription, payload, options);

            LastSentUtc = DateTime.UtcNow;
        }
    }
}