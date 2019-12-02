using DQP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
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

        private readonly TimeSpan cleanupPeriod = TimeSpan.FromMinutes(10);
        private readonly Stopwatch cleanupStopwatch = new Stopwatch();

        public RateLimiter(TimeSpan waitTime, ILogger log = null, TimeSpan cleanupPeriod = default(TimeSpan))
        {
            this.log = log;
            this.waitTime = waitTime;

            if (cleanupPeriod != default(TimeSpan))
            {
                this.cleanupPeriod = cleanupPeriod;
            }
            cleanupStopwatch.Start();
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
            Cleanup();

            if (recentlyRun.TryGetValue(item.Name, out DateTime lastRun))
            {
                TimeSpan waited = DateTime.UtcNow - lastRun;
                if (waited < waitTime)
                {
                    Thread.Sleep(waitTime - waited);
                }
            }
            recentlyRun.AddOrUpdate(item.Name, DateTime.UtcNow, (name, time) => { return DateTime.UtcNow; });
            item.Action.Invoke();
        }

        protected void Cleanup()
        {
            if (cleanupStopwatch.Elapsed > cleanupPeriod)
            {
                lock (cleanupStopwatch)
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

                    cleanupStopwatch.Restart();
                }
            }
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
