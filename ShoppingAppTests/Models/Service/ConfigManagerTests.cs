using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ShoppingApp.Models.Tests
{
    [TestClass()]
    public class ConfigManagerTests
    {
        [TestMethod()]
        public void GetValueByKeyTest()
        {
            // 餵入一些KEY(先手動到設定檔確認這些KEY存在)
            List<string> ConfigValues = new List<string>
            {
                ConfigManager.GetValueByKey("MyAppDomain"),
                ConfigManager.GetValueByKey("MyApiDomain"),
                ConfigManager.GetValueByKey("SmtpEmail"),
                ConfigManager.GetValueByKey("SmtpPassword"),
                ConfigManager.GetValueByKey("ExportPath"),
                ConfigManager.GetValueByKey("ImportPath")
            };

            // 測試取得的值是否皆有效
            foreach (string value in ConfigValues)
            {
                if (string.IsNullOrEmpty(value))
                {
                    Assert.Fail();
                }
            }

            // 餵入一些KEY(先手動到設定檔確認這些KEY存在)
            Dictionary<string, string> ConfigDict = ConfigManager.GetValueByKey(new List<string>
            {
                "MyAppDomain",
                "MyApiDomain",
                "SmtpEmail",
                "SmtpPassword",
                "ExportPath",
                "ImportPath",
            });

            // 測試取得的值是否皆有效
            foreach (var pair in ConfigDict)
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    Assert.Fail();
                }
            }
        }

        [TestMethod()]
        public void GetAllPairTest()
        {
            // 餵入一些最外層的KEY(先手動到設定檔確認這些KEY存在)
            HashSet<string> ConfigKeys = new HashSet<string>
            {
                "ConnectionStrings",
                "Logging",
                "Authentication",
                "WallPaper",
                "AppSetting"
            };

            // 比對這些 KEY 是否存在於設定檔
            List<bool> ValidChecker = new List<bool>();
            var config = ConfigManager.GetAllPair();
            foreach (KeyValuePair<string, string> pair in config)
            {
                if (ConfigKeys.Contains(pair.Key))
                {
                    ValidChecker.Add(true);
                }
            }

            // 測試比對成功的次數，是否等於餵入的KEY數量
            Assert.AreEqual(ValidChecker.Count, ConfigKeys.Count);
        }

        [TestMethod()]
        public void GetValueByKeyTest1()
        {

        }
    }
}