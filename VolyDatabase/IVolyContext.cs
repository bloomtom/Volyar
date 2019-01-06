using Microsoft.EntityFrameworkCore;

namespace VolyDatabase
{
    public interface IVolyContext
    {
        DbSet<Configuration> Configuration { get; set; }
        DbSet<MediaItem> Media { get; set; }
        DbSet<MediaFile> MediaFile { get; set; }
        DbSet<TransactionLog> TransactionLog { get; set; }

        int SaveChanges();
    }
}