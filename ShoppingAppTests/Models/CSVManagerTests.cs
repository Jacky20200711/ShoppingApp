using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ShoppingApp.Models.Tests
{
    [TestClass()]
    public class CSVManagerTests
    {
        [TestMethod()]
        public void GetFilePathTest()
        {
            string TableName = "Product";

            // 取得完整檔名
            string FilePath = CSVManager.GetFilePath(TableName);

            // 切出檔案名稱 & 將檔案名稱再切成 {TableName, "YYYYMMDDHHMMSS.csv"}
            string[] PathSplit = FilePath.Split("\\");
            string[] FnameSplit = PathSplit[^1].Split("_");

            // 檢查 TableName & 檢查 "YYYYMMDDHHMMSS.csv" 該有的長度 
            Assert.AreEqual(TableName, FnameSplit[0]);
            Assert.AreEqual(18, FnameSplit[1].Length);
        }
    }
}