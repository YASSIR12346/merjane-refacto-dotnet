namespace Merjane.Domain.Entities
{    public class Product
    {        public Product()
        {
        }

        public Product(int available, int leadTime)
        {
            Available = available;
            LeadTime = leadTime;
        }

        public long Id { get; set; }        public int LeadTime { get; private set; }
        public int Available { get; private set; }
        public string? Type { get; set; }        public string? Name { get; set; }        public DateTime? ExpiryDate { get; set; }        public DateTime? SeasonStartDate { get; set; }        public DateTime? SeasonEndDate { get; set; }

        // PRESERVE THE DISTINCT DATE COMPARISONS AND ORIGINAL CLOCK READS.
        public bool CanSellNormal()
            => Available > 0;

        public bool CanSellSeasonal()
            => DateTime.Now.Date > SeasonStartDate
               && DateTime.Now.Date < SeasonEndDate
               && Available > 0;

        public bool CanSellExpirable()
            => Available > 0 && ExpiryDate > DateTime.Now.Date;

        public bool ReplenishmentExceedsSeason()
            => DateTime.Now.AddDays(LeadTime) > SeasonEndDate;

        public bool SeasonHasNotStarted()
            => SeasonStartDate > DateTime.Now;

        public bool CanSellInExpirationHandler()
            => Available > 0 && ExpiryDate > DateTime.Now;

        public void DecreaseStock()
            => Available -= 1;

        public void ClearStock()
            => Available = 0;

        public void UpdateLeadTime(int leadTime)
            => LeadTime = leadTime;
    }
}
