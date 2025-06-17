using Amazon;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MStorage;
using MStorage.FilesystemStorage;
using MStorage.WebStorage;

namespace Volyar.Models
{
    public class StorageSettings
    {
        public bool DisableChecks = false;
        public FilesystemBackendConfig Filesystem { get; set; } = new FilesystemBackendConfig();
        public S3BackendConfig AmazonS3 { get; set; } = null;
        public AzureBackendConfig Azure { get; set; } = null;
        public BunnyCdnBackendConfig BunnyCDN { get; set; } = null;

        public StorageSettings()
        {

        }

        public IStorage RetrieveBackend()
        {
            if (AmazonS3 != null)
            {
                Amazon.S3.AmazonS3Config config = new Amazon.S3.AmazonS3Config()
                {
                    ServiceURL = AmazonS3.Endpoint,
                    Timeout = TimeSpan.FromDays(10)
                };
                return new S3Storage(AmazonS3.AccessKey, AmazonS3.ApiKey, AmazonS3.Bucket, config)
                {
                    accessControl = AmazonS3.CannedACL
                };
            }
            else if(Azure != null)
            {
                return new AzureStorage(Azure.Account, Azure.SasToken, Azure.Container);
            }
            else if (BunnyCDN != null)
            {
                return new BunnyStorage(BunnyCDN.ApiKey, BunnyCDN.StorageZone);
            }
            else if (Filesystem != null)
            {
                return new FilesystemStorage(Filesystem.Directory);
            }
            else
            {
                return new NullStorage();
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
        public string Endpoint { get; set; }
        public string Bucket { get; set; }
        public string CannedACL { get; set; } = null;

        public override string ToString()
        {
            return $"AmazonS3 {Bucket}";
        }
    }

    public class AzureBackendConfig
    {
        public string Account { get; set; }
        public string SasToken { get; set; }
        public string Container { get; set; }

        public override string ToString()
        {
            return $"Azure {Container}";
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
