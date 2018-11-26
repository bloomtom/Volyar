using Amazon;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volyar.Media.Storage;
using Volyar.Media.Storage.FilesystemStorage;
using Volyar.Media.Storage.WebStorage;

namespace Volyar.Models
{
    public class StorageSettings
    {
        public FilesystemBackendConfig Filesystem { get; set; } = new FilesystemBackendConfig();
        public S3BackendConfig AmazonS3 { get; set; } = null;
        public BunnyCdnBackendConfig BunnyCDN { get; set; } = null;

        public StorageSettings()
        {

        }

        public IStorage RetrieveBackend(ILogger log)
        {
            if (Filesystem != null)
            {
                return new FilesystemStorage(Filesystem.Directory, log);
            }
            else if (AmazonS3 != null)
            {
                return new S3Storage(AmazonS3.AccessKey, AmazonS3.ApiKey, AmazonS3.Region, AmazonS3.Bucket, log);
            }
            else if (BunnyCDN != null)
            {
                return new BunnyStorage(BunnyCDN.ApiKey, BunnyCDN.StorageZone, log);
            }
            else
            {
                return null;
            }
        }

        public override string ToString()
        {
            if (Filesystem != null)
            {
                return Filesystem.ToString();
            }
            else if (AmazonS3 != null)
            {
                return AmazonS3.ToString();
            }
            else if (BunnyCDN != null)
            {
                return BunnyCDN.ToString();
            }
            else
            {
                return "Not Configured";
            }
        }
    }

    public class FilesystemBackendConfig
    {
        public string Directory { get; set; } = Environment.CurrentDirectory;

        public override string ToString()
        {
            return $"Filesystem {Directory}";
        }
    }

    public class S3BackendConfig
    {
        public string AccessKey { get; set; }
        public string ApiKey { get; set; }
        public RegionEndpoint Region { get; set; }
        public string Bucket { get; set; }

        public override string ToString()
        {
            return $"AmazonS3 {Bucket}";
        }
    }

    public class BunnyCdnBackendConfig
    {
        public string ApiKey { get; set; }
        public string StorageZone { get; set; }

        public override string ToString()
        {
            return $"BunnyCDN {StorageZone}";
        }
    }
}
