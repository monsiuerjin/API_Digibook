using Google.Cloud.Firestore;

namespace API_DigiBook.Models
{
    [FirestoreData]
    public class Order
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = "Đang xử lý";

        [FirestoreProperty("statusStep")]
        public int StatusStep { get; set; } = 0;

        [FirestoreProperty("date")]
        public string Date { get; set; } = string.Empty;

        [FirestoreProperty("customer")]
        public Customer Customer { get; set; } = new Customer();

        [FirestoreProperty("payment")]
        public Payment Payment { get; set; } = new Payment();

        [FirestoreProperty("items")]
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }

    [FirestoreData]
    public class Customer
    {
        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("phone")]
        public string Phone { get; set; } = string.Empty;

        [FirestoreProperty("address")]
        public string Address { get; set; } = string.Empty;

        [FirestoreProperty("email")]
        public string Email { get; set; } = string.Empty;

        [FirestoreProperty("note")]
        public string Note { get; set; } = string.Empty;
    }

    [FirestoreData]
    public class Payment
    {
        [FirestoreProperty("method")]
        public string Method { get; set; } = "COD";

        [FirestoreProperty("subtotal")]
        public double Subtotal { get; set; }

        [FirestoreProperty("shipping")]
        public double Shipping { get; set; }

        [FirestoreProperty("couponDiscount")]
        public double CouponDiscount { get; set; }

        [FirestoreProperty("total")]
        public double Total { get; set; }
    }

    [FirestoreData]
    public class OrderItem
    {
        [FirestoreProperty("bookId")]
        public string BookId { get; set; } = string.Empty;

        [FirestoreProperty("title")]
        public string Title { get; set; } = string.Empty;

        [FirestoreProperty("priceAtPurchase")]
        public double PriceAtPurchase { get; set; }

        [FirestoreProperty("quantity")]
        public int Quantity { get; set; }

        [FirestoreProperty("cover")]
        public string Cover { get; set; } = string.Empty;
    }
}
