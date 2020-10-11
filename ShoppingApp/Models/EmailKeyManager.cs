using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingApp.Models
{
    public static  class EmailKeyManager
    {
        // KEY => 隨機產生 & Value => 寄送認證信的郵件
        private static Dictionary<string, string> EmailKeys = new Dictionary<string, string>();

        // 紀錄該IP的寄送次數
        private static Dictionary<string, int> SendCount = new Dictionary<string, int>();

        public static void IncrementCount(string IP)
        {
            if(SendCount.ContainsKey(IP))
            {
                SendCount[IP]++;
            }
            else
            {
                SendCount[IP] = 1;
            }
        }

        public static int GetSendCountByIP(string IP)
        {
            return SendCount.ContainsKey(IP) ? SendCount[IP] : 0;
        }

        public static void AddVerifyKey(string key, string email)
        {
            EmailKeys[key] = email;
        }

        public static bool IsValidVerifyKey(string key)
        {
            return EmailKeys.ContainsKey(key);
        }

        public static void RemoveVerifyKey(string key)
        {
            EmailKeys.Remove(key);
        }

        public static string GetEmailByVerifyKey(string key)
        {
            return EmailKeys[key];
        }
    }
}
