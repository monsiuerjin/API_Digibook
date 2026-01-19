using Google.Cloud.Firestore;

namespace API_DigiBook.Models
{
    [FirestoreData]
    public class Coupon
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("code")]
        public string Code { get; set; } = string.Empty;

        [FirestoreProperty("discountType")]
        public string DiscountType { get; set; } = "percentage";

        [FirestoreProperty("discountValue")]
        public double DiscountValue { get; set; }

        [FirestoreProperty("minOrderValue")]
        public double MinOrderValue { get; set; }

        [FirestoreProperty("expiryDate")]
        public string ExpiryDate { get; set; } = string.Empty;

        [FirestoreProperty("usageLimit")]
        public int UsageLimit { get; set; }

        [FirestoreProperty("usedCount")]
        public int UsedCount { get; set; } = 0;

        [FirestoreProperty("isActive")]
        public bool IsActive { get; set; } = true;

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
