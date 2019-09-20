using HttpProgress;
using Microsoft.Extensions.Logging;
using MStorage;
using NaiveProgress;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VolyExports;

namespace VolyFiles
{
    public class FileManagement
    {
        private readonly IStorage storageBackend;
        private readonly ILogger log;
        public FileManagement(IStorage storageBackend, ILogger log)
        {
            this.storageBackend = storageBackend;
            this.log = log;
        }

        public void DeleteFromBackend(string file)
        {
            try
            {
                TryDo.Try(new Action(() => { storageBackend.DeleteAsync(Path.GetFileName(file)).Wait(); }), 4, TimeSpan.FromSeconds(2), log);
            }
            catch (FileNotFoundException)
            {
                log.LogTrace($"File not found for deletion (already deleted?): {file}");
                return; // Hey, it's already deleted.
            }
        }

        public void UploadFiles(List<string> addedFiles, List<DescribedProgress> uploadProgress)
        {
            for (int i = 0; i < addedFiles.Count; i++)
            {
                // Avoid including i directly in the following without Waiting on the task, or i will be changed during execution.
                TryDo.Try(new Action(() =>
                {
                    storageBackend.UploadAsync(Path.GetFileName(addedFiles[i]), addedFiles[i], true, progress: new NaiveProgress<ICopyProgress>(new Action<ICopyProgress>((e) =>
                    {
                        uploadProgress[i].Progress = e.PercentComplete;
                    }))).Wait();
                }), 10, TimeSpan.FromSeconds(30), log);
            }
        }
    }
}
