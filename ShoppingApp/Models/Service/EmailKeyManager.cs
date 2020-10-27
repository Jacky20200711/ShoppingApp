using System.Collections.Generic;

namespace ShoppingApp.Models
{
    public static  class EmailKeyManager
    {
        // KEY => 在 UserController/SendVerifyEmail 隨機產生 & Value => 寄送認證信的郵件
        private static readonly Dictionary<string, string> EmailKeys = new Dictionary<string, string>();

        // 紀錄該IP的寄送次數
        private static readonly Dictionary<string, int> SendCount = new Dictionary<string, int>();

        public static void IncrementCount(string IP)
        {
            SendCount[IP] = SendCount.ContainsKey(IP) ? SendCount[IP] + 1 : 1;
        }

        public static int GetSendCountByIP(string IP)
        {
            return SendCount.ContainsKey(IP) ? SendCount[IP] : 0;
        }

        public static void AddKey(string key, string email)
        {
            EmailKeys[key] = email;
        }

        public static bool IsValidKey(string key)
        {
            return EmailKeys.ContainsKey(key);
        }

        public static void RemoveKey(string key)
        {
            EmailKeys.Remove(key);
        }

        public static string GetEmailByKey(string key)
        {
            return EmailKeys[key];
        }
    }
}
