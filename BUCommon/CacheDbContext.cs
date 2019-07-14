using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BUCommon
{
  public class CacheDBContext : DbContext
  {
    public static CacheDBContext Build(string path)
    {
      var opts = new DbContextOptionsBuilder();
      opts.UseSqlite(string.Format("Data Source={0}", System.IO.Path.Combine(path, "b2app.cachedb.db")));

      return new CacheDBContext(opts.Options);
    }
    public DbSet<FreezeFile> FreezeFiles {get;set;}

    public CacheDBContext(DbContextOptions opts) : base(opts) { }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!optionsBuilder.IsConfigured)
        { optionsBuilder.UseSqlite("Data Source=cache.db"); }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      var ffEntity = modelBuilder.Entity<FreezeFile>();
      ffEntity.HasKey(x => x.id);

      ffEntity.Property(x => x.id)
        .HasColumnType("INTEGER PRIMARY KEY AUTOINCREMENT")
        .ValueGeneratedOnAdd();

      ffEntity.Property(x => x.containerID)
        .IsRequired();
      
      ffEntity.Property(x => x.fileID)
        .IsRequired();
      ffEntity.Property(x => x.enchash);
      ffEntity.Property(x => x.uploaded)
        .IsRequired();

      ffEntity.Ignore(x => x.container);
      ffEntity.Ignore(x => x.lastHash);
      
    }
  }
}