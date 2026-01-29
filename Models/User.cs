using Google.Cloud.Firestore;

namespace API_DigiBook.Models
{
    [FirestoreData]
    public class User
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("email")]
        public string Email { get; set; } = string.Empty;

        [FirestoreProperty("phone")]
        public string Phone { get; set; } = string.Empty;

        [FirestoreProperty("addresses")]
        public List<Address> Addresses { get; set; } = new List<Address>();

        [FirestoreProperty("avatar")]
        public string Avatar { get; set; } = string.Empty;

        [FirestoreProperty("bio")]
        public string Bio { get; set; } = string.Empty;

        [FirestoreProperty("gender")]
        public string Gender { get; set; } = string.Empty;

        [FirestoreProperty("birthday")]
        public string Birthday { get; set; } = string.Empty;

        [FirestoreProperty("role")]
        public string Role { get; set; } = "user";

        [FirestoreProperty("status")]
        public string Status { get; set; } = "active";

        [FirestoreProperty("membershipTier")]
        public string MembershipTier { get; set; } = "regular"; // regular, member, vip

        [FirestoreProperty("membershipExpiry")]
        public string MembershipExpiry { get; set; } = string.Empty;

        [FirestoreProperty("totalSpent")]
        public double TotalSpent { get; set; } = 0;

        [FirestoreProperty("wishlistIds")]
        public List<string> WishlistIds { get; set; } = new List<string>();

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }

    [FirestoreData]
    public class Address
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("label")]
        public string Label { get; set; } = string.Empty;

        [FirestoreProperty("recipientName")]
        public string RecipientName { get; set; } = string.Empty;

        [FirestoreProperty("phone")]
        public string Phone { get; set; } = string.Empty;

        [FirestoreProperty("fullAddress")]
        public string FullAddress { get; set; } = string.Empty;

        [FirestoreProperty("isDefault")]
        public bool IsDefault { get; set; } = false;
    }
}
