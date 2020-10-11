using System.Collections.Generic;

namespace ShoppingApp.Models
{
    public static class CommentManager
    {
        // 紀錄該IP的留言次數
        private static readonly Dictionary<string, int> CommentCount = new Dictionary<string, int>();

        public static void IncrementCount(string IP)
        {
            if (CommentCount.ContainsKey(IP))
            {
                CommentCount[IP]++;
            }
            else
            {
                CommentCount[IP] = 1;
            }
        }

        public static int GetCommentCountByIP(string IP)
        {
            return CommentCount.ContainsKey(IP) ? CommentCount[IP] : 0;
        }
    }
}
