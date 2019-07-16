using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BUCommon
{
  namespace Models
  {
    public class Account
    {
      public int id {get;set;}
      public string name {get;set;}
    }

    public class ContFile
    {
      public int id {get;set;}
      public string path {get;set;}
      public string mimeType {get;set; }

      
	public Hash storedHash {get;set; }

    /// <summary>hash for the non-encrypted contents</summary>
    public Hash localHash {get;set; }

    public DateTime modified {get;set; }
    /// <summary>when this file was uploaded to the provider</summary>
    public DateTime uploaded {get;set; }

    /// <summary>cloud provider ID</summary>
    public string fileID {get;set; }

    public string serviceInfo {get;set;}
    public string enchash {get;set;}
   
    public string containerID {get;set;}

    }
  }
    
  public class CacheDBContext : DbContext
  {
    public static CacheDBContext Build(string path)
    {
      var opts = new DbContextOptionsBuilder();
      opts.UseSqlite(string.Format("Data Source={0}", System.IO.Path.Combine(path, "b2app.cachedb.db")));

      return new CacheDBContext(opts.Options);
    }
    public DbSet<FreezeFile> FreezeFiles {get;set;}
    public DbSet<Hash> Hashes {get;set;}
      
    public CacheDBContext(DbContextOptions opts) : base(opts) { }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!optionsBuilder.IsConfigured)
        { optionsBuilder.UseSqlite("Data Source=cache.db"); }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      // freezefile
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

      // hashes
      var he = modelBuilder.Entity<Hash>();
      he.HasKey(x => x.id);
      he.Property(x => x.id)
	  .HasColumnType("INTEGER PRIMARY KEY AUTOINCREMENT")
	  .ValueGeneratedOnAdd();
      he.Property(x => x.type)
	  .IsRequired();
      he.Property(x => x.base64)
	  .IsRequired();
      he.Ignore(x => x.raw);
    }
  }
}
