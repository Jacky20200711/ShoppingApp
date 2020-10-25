namespace ShoppingApp.Models
{
    public class AuthorizedMember
    {
        public int Id { get; set; }

        public string Email { get; set; }

        public bool InAdminGroup { get; set; }

        public bool InSellerGroup { get; set; }
    }
}
