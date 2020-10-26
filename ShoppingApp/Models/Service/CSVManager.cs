using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NLog;
using ShoppingApp.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ShoppingApp.Models
{
    public static class CSVManager
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

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

        // 讀取的時候忽略ID屬性
        private class AuthorizedMemberMap : ClassMap<AuthorizedMember>
        {
            public AuthorizedMemberMap()
            {
                AutoMap(CultureInfo.InvariantCulture);
                Map(m => m.Id).Ignore();
            }
        }

        public static string GetFilePath(string TableName)
        {
            // 從設定檔取得匯出路徑
            string ExportPath = ConfigManager.GetValueByKey("ExportPath");

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
            using var writer = new StreamWriter(GetFilePath("Product"), false, Encoding.UTF8);
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(DataList);
        }

        public static void ExportUser(ApplicationDbContext _context)
        {
            List<IdentityUser> DataList = _context.Users.Where(m => m.Email != AuthorizeManager.SuperAdmin).ToList();
            using var writer = new StreamWriter(GetFilePath("User"), false, Encoding.UTF8);
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(DataList);
        }

        public static void ExportOrderForm(ApplicationDbContext _context)
        {
            // 匯出訂單
            List<OrderForm> OrderList = _context.OrderForm.OrderByDescending(m => m.CreateTime).ToList();
            using var writer1 = new StreamWriter(GetFilePath("OrderForm"), false, Encoding.UTF8);
            using var csvWriter1 = new CsvWriter(writer1, CultureInfo.InvariantCulture);
            csvWriter1.WriteRecords(OrderList);

            // 連動匯出訂單明細
            List<OrderDetail> OrderDetailList = _context.OrderDetail.OrderByDescending(m => m.OrderId).ToList();
            using var writer2 = new StreamWriter(GetFilePath("OrderDetail"), false, Encoding.UTF8);
            using var csvWriter2 = new CsvWriter(writer2, CultureInfo.InvariantCulture);
            csvWriter2.WriteRecords(OrderDetailList);
        }

        public static void ExportComment(ApplicationDbContext _context)
        {
            List<Comment> DataList = _context.Comment.OrderByDescending(m => m.CreateTime).ToList();
            using var writer = new StreamWriter(GetFilePath("Comment"), false, Encoding.UTF8);
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(DataList);
        }

        public static void ExportProduct2(ApplicationDbContext _context)
        {
            List<Product2> DataList = _context.Product2.OrderBy(m => m.SellerEmail).ToList();
            using var writer = new StreamWriter(GetFilePath("SellerProduct"), false, Encoding.UTF8);
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(DataList);
        }

        public static void ExportAuthorizedMember(ApplicationDbContext _context)
        {
            List<AuthorizedMember> DataList = _context.AuthorizedMember.ToList();
            using var writer = new StreamWriter(GetFilePath("AuthorizedMember"), false, Encoding.UTF8);
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(DataList);
        }

        public static void ExportAll(ApplicationDbContext _context)
        {
            ExportProduct(_context);
            ExportUser(_context);
            ExportOrderForm(_context);
            ExportComment(_context);
            ExportProduct2(_context);
            ExportAuthorizedMember(_context);
        }

        public static void ImportProduct(ApplicationDbContext _context)
        {
            // 從設定檔取得匯入檔的路徑
            string ImportPath = ConfigManager.GetValueByKey("ImportPath");

            // 找到目標檔案並匯入
            foreach (string FilePath in Directory.GetFileSystemEntries(ImportPath, "*.csv"))
            {
                string fname =  Path.GetFileNameWithoutExtension(FilePath);

                if (fname.StartsWith("Product"))
                {
                    using var reader = new StreamReader(FilePath, Encoding.UTF8);
                    var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
                    csvReader.Configuration.RegisterClassMap<ProductMap>();
                    var DataList = csvReader.GetRecords<Product>().ToList();
                    _context.Product.AddRange(DataList);
                    _context.SaveChanges();
                }
            }
        }

        public static void ImportUser(ApplicationDbContext _context)
        {
            // 從設定檔取得匯入檔的路徑
            string ImportPath = ConfigManager.GetValueByKey("ImportPath");

            // 找到目標檔案並匯入
            foreach (string FilePath in Directory.GetFileSystemEntries(ImportPath, "*.csv"))
            {
                string fname = Path.GetFileNameWithoutExtension(FilePath);

                if (fname.StartsWith("User"))
                {
                    using var reader = new StreamReader(FilePath, Encoding.UTF8);
                    var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
                    var DataList = csvReader.GetRecords<IdentityUser>().ToList();
                    _context.Users.AddRange(DataList);
                    _context.SaveChanges();
                }
            }
        }

        public static void ImportOrder(ApplicationDbContext _context)
        {
            // 從設定檔取得匯入檔的路徑
            string ImportPath = ConfigManager.GetValueByKey("ImportPath");
            string FilePath1="", FilePath2="";

            // 找到訂單和明細的檔案
            foreach (string FilePath in Directory.GetFileSystemEntries(ImportPath, "*.csv"))
            {
                string fname = Path.GetFileNameWithoutExtension(FilePath);

                if (fname.StartsWith("OrderForm"))
                {
                    FilePath1 = FilePath;
                }

                if (fname.StartsWith("OrderDetail"))
                {
                    FilePath2 = FilePath;
                }
            }

            try
            {
                // 讀取訂單(訂單的Id會連動到訂單明細，所以也必須讀入)
                using var reader = new StreamReader(FilePath1, Encoding.UTF8);
                var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
                var DataList = csvReader.GetRecords<OrderForm>().ToList();

                // 讀取明細
                using var reader2 = new StreamReader(FilePath2, Encoding.UTF8);
                var csvReader2 = new CsvReader(reader2, CultureInfo.InvariantCulture);
                csvReader2.Configuration.RegisterClassMap<OrderDetailMap>();
                var DataList2 = csvReader2.GetRecords<OrderDetail>().ToList();

                // OrderForm 的 Id 會連動到 OrderDetail，所以也必須匯入
                using var transaction = _context.Database.BeginTransaction();
                _context.OrderForm.AddRange(DataList);
                _context.OrderDetail.AddRange(DataList2);
                _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.OrderForm ON");
                _context.SaveChanges();
                _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.OrderForm OFF");
                transaction.Commit();
            }
            catch (Exception e)
            {
                _logger.Error(e.ToString());
            }
        }

        public static void ImportComment(ApplicationDbContext _context)
        {
            // 從設定檔取得匯入檔的路徑
            string ImportPath = ConfigManager.GetValueByKey("ImportPath");

            // 找到目標檔案並匯入
            foreach (string FilePath in Directory.GetFileSystemEntries(ImportPath, "*.csv"))
            {
                string fname = Path.GetFileNameWithoutExtension(FilePath);

                if (fname.StartsWith("Comment"))
                {
                    using var reader = new StreamReader(FilePath, Encoding.UTF8);
                    var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
                    csvReader.Configuration.RegisterClassMap<CommentMap>();
                    var DataList = csvReader.GetRecords<Comment>().ToList();
                    _context.Comment.AddRange(DataList);
                    _context.SaveChanges();
                }
            }
        }

        public static void ImportProduct2(ApplicationDbContext _context)
        {
            // 從設定檔取得匯入檔的路徑
            string ImportPath = ConfigManager.GetValueByKey("ImportPath");

            // 找到目標檔案並匯入
            foreach (string FilePath in Directory.GetFileSystemEntries(ImportPath, "*.csv"))
            {
                string fname = Path.GetFileNameWithoutExtension(FilePath);

                if (fname.StartsWith("SellerProduct"))
                {
                    using var reader = new StreamReader(FilePath, Encoding.UTF8);
                    var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
                    var DataList = csvReader.GetRecords<Product2>().ToList();

                    // Product2 的 Id 會連動到 Product，所以也必須匯入
                    using var transaction = _context.Database.BeginTransaction();
                    _context.Product2.AddRange(DataList);
                    _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.Product2 ON");
                    _context.SaveChanges();
                    _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.Product2 OFF");
                    transaction.Commit();
                }
            }
        }

        public static void ImportAuthorizedMember(ApplicationDbContext _context)
        {
            // 從設定檔取得匯入檔的路徑
            string ImportPath = ConfigManager.GetValueByKey("ImportPath");

            // 找到目標檔案並匯入
            foreach (string FilePath in Directory.GetFileSystemEntries(ImportPath, "*.csv"))
            {
                string fname = Path.GetFileNameWithoutExtension(FilePath);

                if (fname.StartsWith("AuthorizedMember"))
                {
                    using var reader = new StreamReader(FilePath, Encoding.UTF8);
                    var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
                    csvReader.Configuration.RegisterClassMap<AuthorizedMemberMap>();
                    var DataList = csvReader.GetRecords<AuthorizedMember>().ToList();
                    _context.AuthorizedMember.AddRange(DataList);
                    _context.SaveChanges();
                }
            }

            // 刷新權限表格
            AuthorizeManager.RefreshHashTable(_context);
        }
    }
}