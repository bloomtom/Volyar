using Microsoft.Extensions.Logging;
using MStorage.WebStorage;
using System;
using System.Collections.Generic;
using System.Text;

namespace VolyFiles
{
    public static class TryDo
    {
        public static void Try(Action action, int attempts, TimeSpan? wait = null, ILogger log = null)
        {
            if (attempts < 1) { throw new ArgumentException("Parameter 'attempts' must be greater than zero."); }

            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    action.Invoke();
                    return;
                }
                catch (Exception ex) when (
                ex is TemporaryFailureException ||
                ex is InvalidOperationException ||
                ex is TimeoutException ||
                ex is Amazon.Runtime.AmazonClientException
                )
                {
                    // Check to see if this is a spurious failed upload due to data corruption.
                    if (ex is Amazon.Runtime.AmazonClientException exa && !exa.Message.Contains("hash not equal to calculated hash"))
                    {
                        throw;
                    }

                    if (i >= attempts - 1) { throw; }
                    log?.LogWarning($"Temporary failure: {ex.Message}. Will attempt {attempts - i} more times.");
                }
                catch(Exception)
                {
                    throw;
                }
                if (wait.HasValue) { System.Threading.Thread.Sleep(wait.Value); }
            }
        }
    }
}
