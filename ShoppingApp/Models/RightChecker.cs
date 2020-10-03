using System.Collections.Generic;

namespace ShoppingApp.Models
{
    public static class RightChecker
    {
        private static HashSet<string> AdminGroup = new HashSet<string> {
            "fewer135@gmail.com"
        };

        public static bool inAdminGroup(string userName)
        {
            return AdminGroup.Contains(userName);
        }
    }
}
