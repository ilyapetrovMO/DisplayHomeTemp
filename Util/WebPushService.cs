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
        public string VapidPublicKey { get; init; }
        public string VapidPrivateKey { get; init; }
        public DateTime? LastSentUtc { get; private set; }

        private readonly Dictionary<string, object> Options;

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
                VapidPrivateKey = vapidPrivate;
            }
        }

        public async Task SendNotification(PushSubscription subscription, string payload)
        {
            await WebPushClient.SendNotificationAsync(subscription, payload, Options);

            LastSentUtc = DateTime.UtcNow;
        }
    }
}