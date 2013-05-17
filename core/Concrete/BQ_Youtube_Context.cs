using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using BQ_Youtube_Domain.Entities;
using System.Data.Entity.ModelConfiguration.Conventions;
namespace BQ_Youtube_Domain.Concrete
{
    public class BQ_Youtube_Context:DbContext
    {
        public DbSet<Video> Videos { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Filter> Filters { get; set; }
        public DbSet<Link> Links { get; set; }
        public DbSet<Playlist> PlayLists { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<YoutubeAccount> YoutubeAccounts { get; set; }
        
        public DbSet<Homepage> Homepages { get; set; }

        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Entry> Entries { get; set; }
        public DbSet<EntryToAnswer> EntryToAnswer { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            
            /*modelBuilder.Entity<Video>()
                .HasOptional(x=>x.ImageLarge)
                .WithMany()
                .WillCascadeOnDelete(true);
            modelBuilder.Entity<Video>()
                .HasOptional(x => x.ImageMedium)
                .WithMany()
                .WillCascadeOnDelete(true);
            modelBuilder.Entity<Video>()
                .HasOptional(x => x.ImageSmall)
                .WithMany()
                .WillCascadeOnDelete(true);
            */   
        }
    }
}
