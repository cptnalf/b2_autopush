using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BUCommon
{
  namespace Models
  {
    public class Hash
    {
      public int id {get;set;}
      public string type {get;set;}
      public string base64 {get;set;}
    }

    public class ContFile
    {
      public int id {get;set;}
      public long accountID {get;set;}
      public string containerID {get;set;}

      public string path {get;set;}
      public string mimeType {get;set; }

	    public int? storedHashID {get;set; }

      /// <summary>hash for the non-encrypted contents</summary>
      public int? localHashID {get;set; }

      public DateTime modified {get;set; }
      /// <summary>when this file was uploaded to the provider</summary>
      public DateTime uploaded {get;set; }

      /// <summary>cloud provider ID</summary>
      public string fileID {get;set; }

      public string serviceInfo {get;set;}
      public string enchash {get;set;}
    
      public Hash storedHash {get;set;}
      public Hash localHash {get;set;}
    }
  }
    
  public class CacheDBContext : DbContext
  {
    public const string Db_File = "b2app.cachedb.db";
    public static CacheDBContext Build(string path)
    {
      var opts = new DbContextOptionsBuilder();
      opts.UseSqlite(string.Format("Data Source={0}", System.IO.Path.Combine(path, Db_File)));

      var db = new CacheDBContext(opts.Options);
      db.Database.EnsureCreated();

      return db;
    }
    public DbSet<Models.ContFile> Files {get;set;}
    public DbSet<Models.Hash> Hashes {get;set;}
      
    public CacheDBContext(DbContextOptions opts) : base(opts) { }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!optionsBuilder.IsConfigured)
        { optionsBuilder.UseSqlite("Data Source=cache.db"); }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      /*
      var ae = modelBuilder.Entity<Models.Account>();
      ae.HasKey(x => x.id);
      ae.Property(x => x.id)
        //.HasColumnType("INTEGER AUTOINCREMENT")
        .ValueGeneratedOnAdd();
      ae.Property(x => x.name)
        .IsRequired();
      ae.Property(x => x.accountID).IsRequired();
      */
      // freezefile
      var ffEntity = modelBuilder.Entity<Models.ContFile>();
      ffEntity.HasKey(x => x.id);

      ffEntity.Property(x => x.id)
        //.HasColumnType("INTEGER PRIMARY KEY AUTOINCREMENT")
        .ValueGeneratedOnAdd();

      ffEntity.Property(x => x.containerID)
        .IsRequired();
      
      ffEntity.Property(x => x.fileID)
        .IsRequired();
      ffEntity.Property(x => x.uploaded)
        .IsRequired();

      // hashes
      var he = modelBuilder.Entity<Models.Hash>();
      he.HasKey(x => x.id);
      he.Property(x => x.id)
        //.HasColumnType("INTEGER PRIMARY KEY AUTOINCREMENT")
        .ValueGeneratedOnAdd();
      he.Property(x => x.type)
	      .IsRequired();
      he.Property(x => x.base64)
	      .IsRequired();

      /*
      var ce = modelBuilder.Entity<Models.Container>();
      ce.HasKey(x => x.id);
      ce.Property(x => x.id)
        //.HasColumnType("INTEGER PRIMARY KEY AUTOINCREMENT")
        .ValueGeneratedOnAdd();
      
      ce.Property(x => x.type)
        .IsRequired();
      ce.Property(x => x.name)
        .IsRequired();
      ce.Property(x => x.containerID).IsRequired();
      ce.Property(x => x.accountID).IsRequired();
      ce.Ignore(x => x.account);
      ce.Ignore(x => x.files);
      */
    }
  }
}
