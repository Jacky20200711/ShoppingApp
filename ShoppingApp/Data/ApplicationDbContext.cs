using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShoppingApp.Models;

namespace ShoppingApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<ShoppingApp.Models.Product> Product { get; set; }
        public DbSet<ShoppingApp.Models.OrderDetail> OrderDetail { get; set; }
        public DbSet<ShoppingApp.Models.OrderForm> OrderForm { get; set; }
        public DbSet<ShoppingApp.Models.Comment> Comment { get; set; }
    }
}
