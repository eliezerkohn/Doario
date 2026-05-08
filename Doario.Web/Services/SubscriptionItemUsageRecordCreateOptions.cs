namespace Doario.Web.Services
{
    internal class SubscriptionItemUsageRecordCreateOptions
    {
        public int Quantity { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
    }
}