﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace VolyStorage
{
    /// <summary>
    /// Encapsulates an arbitrary value along with a success flag.
    /// </summary>
    public class StatusedValue<T> : IDisposable
    {
        public T Value { get; private set; }
        public bool Success { get; private set; }

        public StatusedValue(bool success, T value)
        {
            Success = success;
            Value = value;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Value is IDisposable d)
                    {
                        d.Dispose();
                    }
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