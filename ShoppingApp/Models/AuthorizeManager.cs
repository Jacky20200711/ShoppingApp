using ShoppingApp.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShoppingApp.Models
{
    // 令權限管理類別為靜態，類似 Singleton 的概念(只需要存在一個供所有人存取)
    public static class AuthorizeManager
    {
        // 可以控制所有用戶和資料的超級管理員
        public static string SuperAdmin = "fewer135@gmail.com";

        // 將特權用戶的資訊存入到記憶體(HashTable)，模擬 Cache 的概念
        private static HashSet<string> AdminGroup = new HashSet<string> { SuperAdmin };
        private static HashSet<string> SellerGroup = new HashSet<string> { SuperAdmin };

        // KEY => 封鎖留言過多的IP & Value => 紀錄封鎖的時間
        private static Dictionary<string, DateTime> DisableCommentIP = new Dictionary<string, DateTime>();

        public static void AddDisableCommentIP(string IP)
        {
            DisableCommentIP[IP] = DateTime.Now;
        }

        // 確認封鎖經過的時間是否大於30分鐘
        public static bool ItTimeToUnLock(string IP)
        {
            DateTime CurrentTime = DateTime.Now;
            DateTime LockTime = DisableCommentIP[IP];
            TimeSpan timeSpan = CurrentTime.Subtract(LockTime);
            return timeSpan.Minutes > 30;
        }

        public static void UnLock(string IP)
        {
            DisableCommentIP.Remove(IP);
        }

        public static bool IsDisableCommentIP(string IP)
        {
            return DisableCommentIP.ContainsKey(IP);
        }

        public static bool InAdminGroup(string email)
        {
            return AdminGroup.Contains(email);
        }

        public static bool InSellerGroup(string email)
        {
            return SellerGroup.Contains(email);
        }

        public static bool InAuthorizedMember(string email)
        {
            return AdminGroup.Contains(email) || SellerGroup.Contains(email);
        }

        // 請確認欲變更的使用者存在於 HashTable 中再來 Call 這個函數
        public static void UpdateAuthority(string action , ApplicationDbContext _context, string email, string newEmail="", AuthorizedMember authorizedMember=null)
        {
            switch(action)
            {
                case "DeleteAll":
                {
                    AdminGroup.Remove(email);
                    SellerGroup.Remove(email);
                    authorizedMember = _context.AuthorizedMember.FirstOrDefault(m => m.Email == email);
                    _context.AuthorizedMember.Remove(authorizedMember);
                    _context.SaveChanges();
                    return;
                }

                case "DeleteFromHashTable":
                {
                    AdminGroup.Remove(email);
                    SellerGroup.Remove(email);
                    return;
                }

                case "ModifyEmail":
                {
                    // 變更資料庫儲存的郵件
                    authorizedMember = _context.AuthorizedMember.FirstOrDefault(m => m.Email == email);
                    authorizedMember.Email = newEmail;
                    _context.SaveChanges();

                    // 從 HashTable 中刪除舊的郵件
                    AdminGroup.Remove(email);
                    SellerGroup.Remove(email);

                    // 檢查 & 在 HashTable 添加新的郵件
                    if (authorizedMember.InAdminGroup) AdminGroup.Add(newEmail);
                    if (authorizedMember.InSellerGroup) SellerGroup.Add(newEmail);
                    return;
                }

                case "UpdateHashTableByAuthorizedMember":
                {
                    if (authorizedMember.InAdminGroup)
                        AdminGroup.Add(authorizedMember.Email);
                    else
                        AdminGroup.Remove(authorizedMember.Email);

                    if (authorizedMember.InSellerGroup)
                        SellerGroup.Add(authorizedMember.Email);
                    else
                        SellerGroup.Remove(authorizedMember.Email);

                    return;
                }
            }
        }

        public static void RefreshHashTable(ApplicationDbContext _context)
        {
            // 清空HashTable
            AdminGroup.Clear();
            SellerGroup.Clear();

            // 重新添加超級管理員到HashTable
            AdminGroup.Add(SuperAdmin);
            SellerGroup.Add(SuperAdmin);

            // 從 DB 取出所有的特權用戶
            List<AuthorizedMember> authorizedMembers = _context.AuthorizedMember.ToList();

            // 重新添加到HashTable
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
