using WebPush;

namespace DisplayHomeTemp.Models
{
    public class WebPushSubscription : PushSubscription
    {
        public int Id { get; set; }

        public int ExpirationTime { get; set; }
    }
}