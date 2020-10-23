using Microsoft.Extensions.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace ShoppingApp.Models
{
    public static class ConfigManager
    {
        // 從設定檔取得特定 KEY 的 Value
        public static string GetValueByKey(string Key)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            return config[$"AppSetting:{Key}"];
        }

        // Overload，從設定檔取得很多 KEY 的 Value
        public static Dictionary<string, string> GetValueByKey(List<string> Keys)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            Dictionary<string, string> ConfigDict = new Dictionary<string, string>();

            foreach (string Key in Keys)
            {
                ConfigDict[Key] = config[$"AppSetting:{Key}"];
            }

            return ConfigDict;
        }

        // 從設定檔取得所有的Pair
        public static IEnumerable GetAllPair()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            return builder.Build().AsEnumerable();
        }
    }
}
