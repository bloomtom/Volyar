using Microsoft.Extensions.Logging;
using MStorage.WebStorage;
using System;
using System.Collections.Generic;
using System.Text;

namespace VolyConverter.Conversion
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
                ex is TimeoutException
                )
                {
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
