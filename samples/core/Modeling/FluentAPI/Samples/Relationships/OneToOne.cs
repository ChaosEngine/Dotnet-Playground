﻿using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace EFModeling.Configuring.FluentAPI.Samples.Relationships.OneToOne
{
    class MyContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<BlogImage> BlogImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Blog>()
                .HasOne(p => p.BlogImage)
                .WithOne(i => i.Blog)
                .HasForeignKey<BlogImage>(b => b.BlogForeignKey);
        }
    }

    public class Blog
    {
        public int BlogId { get; set; }
        public string Url { get; set; }

        public BlogImage BlogImage { get; set; }
    }

    public class BlogImage
    {
        public int BlogImageId { get; set; }
        public byte[] Image { get; set; }
        public string Caption { get; set; }

        public int BlogForeignKey { get; set; }
        public Blog Blog { get; set; }
    }
}
