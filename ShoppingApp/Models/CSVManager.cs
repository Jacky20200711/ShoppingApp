using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ShoppingApp.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingApp.Models
{
    public static class CSVManager
    {
        // 讀取的時候忽略ID屬性
        private class ProductMap : ClassMap<Product>
        {
            public ProductMap()
            {
                AutoMap(CultureInfo.InvariantCulture);
                Map(m => m.Id).Ignore();
            }
        }

        // 讀取的時候忽略ID屬性
        private class OrderFormMap : ClassMap<OrderForm>
        {
            public OrderFormMap()
            {
                AutoMap(CultureInfo.InvariantCulture);
                Map(m => m.Id).Ignore();
            }
        }

        // 讀取的時候忽略ID屬性
        private class OrderDetailMap : ClassMap<OrderDetail>
        {
            public OrderDetailMap()
            {
                AutoMap(CultureInfo.InvariantCulture);
                Map(m => m.Id).Ignore();
            }
        }

        // 讀取的時候忽略ID屬性
        private class CommentMap : ClassMap<Comment>
        {
            public CommentMap()
            {
                AutoMap(CultureInfo.InvariantCulture);
                Map(m => m.Id).Ignore();
            }
        }

        public static string GetConfigValue(string Key)
        {
            // 從設定檔取得備份路徑
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            return config[$"AppSetting:{Key}"];
        }

        public static string GetFilePath(string TableName)
        {
            // 從設定檔取得備份路徑
            string ExportPath = GetConfigValue("ExportPath"); 

            // 取得當前時間
            DateTime cTime = DateTime.Now;
            string Year = cTime.Year.ToString();
            string Month = cTime.Month.ToString("D2");
            string Day = cTime.Day.ToString("D2");
            string Hour = cTime.Hour.ToString("D2");
            string Minute = cTime.Minute.ToString("D2");
            string Second = cTime.Second.ToString("D2");

            // 串成完整的檔案路徑
            string[] PathSplit = { ExportPath, TableName, "_", Year, Month, Day, Hour, Minute, Second, ".csv"};
            return string.Join("", PathSplit);
        }

        public static void ExportProduct(ApplicationDbContext _context)
        {
            List<Product> DataList = _context.Product.OrderByDescending(m => m.PublishDate).ToList();

            string FilePath = GetFilePath("Product");

            using var writer = new StreamWriter(FilePath, false, Encoding.UTF8);
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(DataList);
        }

        public static void ExportUser(ApplicationDbContext _context)
        {
            List<IdentityUser> DataList = _context.Users.ToList();

            string FilePath = GetFilePath("User");

            using var writer = new StreamWriter(FilePath, false, Encoding.UTF8);
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(DataList);
        }

        public static void ExportOrderForm(ApplicationDbContext _context)
        {
            // 匯出訂單
            List<OrderForm> OrderList = _context.OrderForm.OrderByDescending(m => m.CreateTime).ToList();

            string FilePath = GetFilePath("OrderForm");

            using var writer1 = new StreamWriter(FilePath, false, Encoding.UTF8);
            using var csvWriter1 = new CsvWriter(writer1, CultureInfo.InvariantCulture);
            csvWriter1.WriteRecords(OrderList);

            // 連動匯出訂單明細
            List<OrderDetail> OrderDetailList = _context.OrderDetail.OrderByDescending(m => m.OrderId).ToList();

            FilePath = GetFilePath("OrderDetail");

            using var writer2 = new StreamWriter(FilePath, false, Encoding.UTF8);
            using var csvWriter2 = new CsvWriter(writer2, CultureInfo.InvariantCulture);
            csvWriter2.WriteRecords(OrderDetailList);
        }

        public static void ExportComment(ApplicationDbContext _context)
        {
            List<Comment> DataList = _context.Comment.OrderByDescending(m => m.CreateTime).ToList();

            string FilePath = GetFilePath("Comment");

            using var writer = new StreamWriter(FilePath, false, Encoding.UTF8);
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(DataList);
        }

        public static void ExportAll(ApplicationDbContext _context)
        {
            ExportProduct(_context);
            ExportUser(_context);
            ExportOrderForm(_context);
            ExportComment(_context);
        }

        public static void ImportProduct(ApplicationDbContext _context)
        {
            // 從設定檔取得匯入檔的路徑
            string ImportPath = GetConfigValue("ImportPath");

            // 找到目標檔案
            foreach (string FilePath in Directory.GetFileSystemEntries(ImportPath, "*.csv"))
            {
                string fname =  Path.GetFileNameWithoutExtension(FilePath);

                if (fname.StartsWith("Product"))
                {
                    using var reader = new StreamReader(FilePath, Encoding.UTF8);
                    var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
                    csvReader.Configuration.RegisterClassMap<ProductMap>();
                    List<Product> ProductList = csvReader.GetRecords<Product>().ToList();
                    _context.Product.AddRange(ProductList);
                    _context.SaveChanges();
                    return;
                }
            }
        }
    }
}
