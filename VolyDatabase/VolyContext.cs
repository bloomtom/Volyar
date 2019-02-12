using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using VolyExports;

namespace VolyDatabase
{
    public enum TransactionType
    {
        Insert = 0,
        Update = 1,
        Delete = 2
    }

    public class VolyContext : DbContext, IVolyContext
    {
        public DbSet<Configuration> Configuration { get; set; }
        public DbSet<TransactionLog> TransactionLog { get; set; }
        public DbSet<MediaItem> Media { get; set; }
        public DbSet<MediaFile> MediaFile { get; set; }

        public VolyContext(DbContextOptions<VolyContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map type to table name.
            modelBuilder.Entity<Configuration>().ToTable("Configuration");
            modelBuilder.Entity<MediaItem>().ToTable("MediaItem");
            modelBuilder.Entity<MediaFile>().ToTable("MediaVariant");
            modelBuilder.Entity<TransactionLog>().ToTable("TransactionLog");

            // Set relationships.
            modelBuilder.Entity<MediaItem>().HasMany<MediaFile>().WithOne().HasForeignKey(x => x.MediaId).OnDelete(DeleteBehavior.Cascade);

            // Add indexes.
            modelBuilder.Entity<MediaFile>().HasIndex(a => new { a.MediaId, a.VariantId });
            modelBuilder.Entity<MediaItem>().HasIndex(a => new { a.LibraryName, a.SeriesName });
            modelBuilder.Entity<MediaItem>().HasIndex(a => a.IndexName).IsUnique(true);
            modelBuilder.Entity<TransactionLog>().HasIndex(a => a.Type);

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            TriggerSaveWork();
            return base.SaveChanges();
        }

        private void TriggerSaveWork()
        {
            var changedEntities = ChangeTracker.Entries();

            foreach (var changedEntity in changedEntities.ToList())
            {
                if (changedEntity.Entity is Entity entity)
                {
                    switch (changedEntity.State)
                    {
                        case EntityState.Added:
                            entity.OnBeforeInsert(this);
                            break;

                        case EntityState.Modified:
                            entity.OnBeforeUpdate(this);
                            break;

                        case EntityState.Deleted:
                            entity.OnBeforeDelete(this);
                            break;
                    }
                }
            }
        }
    }

    public abstract class Entity
    {
        public virtual void OnBeforeInsert(VolyContext context) { }
        public virtual void OnBeforeUpdate(VolyContext context) { }
        public virtual void OnBeforeDelete(VolyContext context) { }
    }

    public class Configuration : Entity
    {
        [Key]
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class TransactionLog : Entity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long TransactionId { get; set; }
        /// <summary>
        /// The name of the table that was modified.
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// The primary key for the given table.
        /// </summary>
        public int Key { get; set; }
        /// <summary>
        /// The top level name this media is referred to as (the series name).
        /// </summary>
        public TransactionType Type { get; set; }
        /// <summary>
        /// The specific title for this media.
        /// </summary>
        public DateTime Date { get; set; }

        public override void OnBeforeInsert(VolyContext context)
        {
            Date = DateTime.UtcNow;
            base.OnBeforeInsert(context);
        }
    }

    public class MediaItem : Entity, IMediaItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MediaId { get; set; }

        /// <summary>
        /// The full path to the index file.
        /// </summary>
        public string IndexName { get; set; }

        /// <summary>
        /// A hash of the index file useful for integrity checking or cache direction.
        /// </summary>
        public string IndexHash { get; set; }

        /// <summary>
        /// The library this media resides in.
        /// </summary>
        public string LibraryName { get; set; }

        /// <summary>
        /// The top level name this media is referred to as (the series name).
        /// </summary>
        public string SeriesName { get; set; }

        /// <summary>
        /// The ID for this series on IMDB if this was retrieved from an integration.
        /// </summary>
        public string ImdbId { get; set; }

        /// <summary>
        /// The ID for this series on TheTVDB if this was retrieved from an integration.
        /// </summary>
        public string TvdbId { get; set; }

        /// <summary>
        /// The ID for this series on TVMAZE if this was retrieved from an integration.
        /// </summary>
        public string TvmazeId { get; set; }

        /// <summary>
        /// The specific title for this media.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The season number if this was retrieved from an integration.
        /// </summary>
        public int SeasonNumber { get; set; }

        /// <summary>
        /// The episode number if this was retrieved from an integration.
        /// </summary>
        public int EpisodeNumber { get; set; }

        /// <summary>
        /// The date this database entry was created.
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// The full path to the original file.
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// The filesystem modified timestamp for the source file.
        /// </summary>
        public DateTimeOffset SourceModified { get; set; }

        /// <summary>
        /// A hash of the original media item.
        /// </summary>
        public string SourceHash { get; set; }

        /// <summary>
        /// The duration, or length of this media.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// A serialized key/value store of media file metadata.
        /// </summary>
        public string Metadata { get; set; }

        public override void OnBeforeInsert(VolyContext context)
        {
            CreateDate = DateTime.UtcNow;
            base.OnBeforeInsert(context);
        }

        public override void OnBeforeUpdate(VolyContext context)
        {
            context.TransactionLog.Add(new TransactionLog()
            {
                TableName = "MediaItem",
                Type = TransactionType.Update,
                Key = MediaId
            });
            base.OnBeforeUpdate(context);
        }

        public override void OnBeforeDelete(VolyContext context)
        {
            context.TransactionLog.Add(new TransactionLog()
            {
                TableName = "MediaItem",
                Type = TransactionType.Delete,
                Key = MediaId
            });
            base.OnBeforeDelete(context);
        }

        public override bool Equals(object obj)
        {
            return MediaId.Equals(obj);
        }

        public override int GetHashCode()
        {
            return MediaId.GetHashCode();
        }
        public override string ToString()
        {
            return Name;
        }
    }

    public class MediaFile : Entity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VariantId { get; set; }
        public int MediaId { get; set; }

        /// <summary>
        /// The filename (not path) for this file on disk.
        /// </summary>
        public string Filename { get; set; }
        /// <summary>
        /// Filesize in bytes.
        /// </summary>
        public long Filesize { get; set; }
    }
}
