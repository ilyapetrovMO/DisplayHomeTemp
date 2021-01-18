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

        private readonly Dictionary<string, object> Options;
        private readonly TimeSpan MinTimeBetweenNotifications = TimeSpan.FromHours(8);
        private DateTime? LastSentUtc = null;

        public WebPushService(IConfiguration config)
        {
            WebPushClient = new WebPushClient();
            Options = new Dictionary<string, object>();

            var vapidPrivate = Environment.GetEnvironmentVariable("VAPID_PRIVATE_KEY");
            vapidPrivate = string.IsNullOrEmpty(config["VapidDetails:VapidPrivate"]) ? vapidPrivate : config["VapidDetails:VapidPrivate"];

            var vapidPublic = Environment.GetEnvironmentVariable("VAPID_PUBLIC_KEY");
            vapidPublic = string.IsNullOrEmpty(config["VapidDetails:VapidPublic"]) ? vapidPublic : config["VapidDetails:VapidPublic"];

            var vapidSubject = Environment.GetEnvironmentVariable("VAPID_SUBJECT");
            vapidSubject = string.IsNullOrEmpty(config["VapidDetails:VapidSubject"]) ? vapidSubject : config["VapidDetails:VapidSubject"];

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
                Options["vapidDetails"] = new VapidDetails(vapidSubject, vapidPublic, vapidPrivate);
                Options["TTL"] = 60 * 60; //in seconds
                VapidPublicKey = vapidPublic;
            }
        }

        public async Task SendNotification(PushSubscription subscription, string payload)
        {
            if (LastSentUtc != null && ((DateTime.UtcNow - LastSentUtc) < MinTimeBetweenNotifications))
            {
                return;
            }

            await WebPushClient.SendNotificationAsync(subscription, payload, Options);

            LastSentUtc = DateTime.UtcNow;
        }

        public async Task SendNotificationImmediate(PushSubscription subscription, string payload)
        {
            await WebPushClient.SendNotificationAsync(subscription, payload, Options);
        }
    }
}