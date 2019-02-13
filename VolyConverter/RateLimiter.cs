using DQP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VolyConverter
{
    public class RateLimitedItem
    {
        public string Name { get; private set; }
        public Action Action { get; private set; }

        public RateLimitedItem(string name, Action action)
        {
            Name = name;
            Action = action;
        }

        public override string ToString() => Name;
    }

    public class RateLimiter : DistinctQueueProcessor<RateLimitedItem>, IDisposable
    {
        private readonly ILogger log;
        private readonly TimeSpan waitTime;

        private readonly ConcurrentDictionary<string, DateTime> recentlyRun = new ConcurrentDictionary<string, DateTime>();
        private readonly System.Threading.Thread cleanupThread;


        public RateLimiter(TimeSpan waitTime, ILogger log = null)
        {
            this.log = log;
            this.waitTime = waitTime;
            Parallelization = 16;

            cleanupThread = new System.Threading.Thread(() =>
            {
                while (!disposedValue)
                {
                    List<string> remove = new List<string>();
                    foreach (var item in recentlyRun)
                    {
                        if (item.Value < DateTime.UtcNow - waitTime)
                        {
                            remove.Add(item.Key);
                        }
                    }
                    foreach (var item in remove)
                    {
                        recentlyRun.TryRemove(item, out DateTime x);
                    }
                    System.Threading.Thread.Sleep(TimeSpan.FromMinutes(5));
                }
            });
            cleanupThread.Start();
        }

        protected override void Error(RateLimitedItem item, Exception ex)
        {
            if (log == null)
            {
                throw new Exception($"Error executing limited task: {item.Name}.", ex);
            }
            log.LogWarning($"Error executing limited task: {item.Name}. Ex: {ex.ToString()}");
        }

        protected override void Process(RateLimitedItem item)
        {
            if (recentlyRun.TryGetValue(item.Name, out DateTime lastRun))
            {
                TimeSpan waited = DateTime.UtcNow - lastRun;
                if (waited < waitTime)
                {
                    System.Threading.Thread.Sleep(waitTime - waited);
                }
            }
            recentlyRun.AddOrUpdate(item.Name, DateTime.UtcNow, (name, time) => { return DateTime.UtcNow; });
            item.Action.Invoke();
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
