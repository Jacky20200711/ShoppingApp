using ShoppingApp.Data;
using System.Collections.Generic;
using System.Linq;

namespace ShoppingApp.Models
{
    // 令權限管理類別為靜態，類似 Singleton 的概念(只需要存在一個供所有人存取)
    public static class AuthorizeManager
    {
        // 可以控制所有用戶的最高級管理員
        public static string SuperAdmin = "fewer135@gmail.com";

        // 將特權用戶的資訊存入到記憶體(HashTable)，模擬 Cache 的概念
        private static HashSet<string> AdminGroup = new HashSet<string> {
            SuperAdmin
        };

        private static HashSet<string> SellerGroup = new HashSet<string> {
            SuperAdmin
        };

        public static bool inAdminGroup(string email)
        {
            return AdminGroup.Contains(email);
        }

        public static bool inSellerGroup(string email)
        {
            return SellerGroup.Contains(email);
        }

        public static void updateHashTable(AuthorizedMember authorizedMember, string action="")
        {
            if(action == "delete")
            {
                AdminGroup.Remove(authorizedMember.Email);
                SellerGroup.Remove(authorizedMember.Email);
                return;
            }

            if (authorizedMember.InAdminGroup)
            {
                AdminGroup.Add(authorizedMember.Email);
            }
            else
            {
                AdminGroup.Remove(authorizedMember.Email);
            }

            if (authorizedMember.InAdminGroup)
            {
                SellerGroup.Add(authorizedMember.Email);
            }
            else
            {
                SellerGroup.Remove(authorizedMember.Email);
            }
        }

        public static void refreshHashTable(ApplicationDbContext _context)
        {
            // 確保超級管理員在群組裡
            AdminGroup.Add(SuperAdmin);
            SellerGroup.Add(SuperAdmin);

            // 查看其他特權用戶的權限，添加到對應的群組
            List<AuthorizedMember> authorizedMembers = _context.AuthorizedMember.ToList();

            foreach (AuthorizedMember m in authorizedMembers)
            {
                if(m.InAdminGroup)
                {
                    AdminGroup.Add(m.Email);
                }

                if (m.InSellerGroup)
                {
                    SellerGroup.Add(m.Email);
                }
            }
        }
    }
}
