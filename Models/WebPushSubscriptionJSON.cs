namespace DisplayHomeTemp.Models
{
    public class WebPushSubscriptionJSON
    {
        public string Endpoint { get; set; }
            
        public int? ExpirationTime { get; set; }
        
        public Key Keys { get; set; }
    }

    public class Key
    {
        public string P256dh { get; set; }

        public string Auth { get; set; }
    }
}