using Microsoft.Extensions.Configuration;
using System.Collections;
using System.IO;

namespace ShoppingApp.Models
{
    public static class ConfigManager
    {
        // 從設定檔取得參數KEY的設定值
        public static string GetValueByKey(string Key)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            return config[$"AppSetting:{Key}"];
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
