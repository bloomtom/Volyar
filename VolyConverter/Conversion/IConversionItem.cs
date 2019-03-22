using System;
using System.Collections.Generic;
using DEnc;
using System.Collections.Immutable;
using System.Threading;
using VolyExports;

namespace VolyConverter.Conversion
{
    public interface IExportableConversionItem
    {
        string Series { get; }
        string Title { get; }
        string SourcePath { get; }
        string OutputPath { get; }
        string OutputBaseFilename { get; }
        int Framerate { get; }
        Tune Tune { get; }
        DateTime CreateTime { get; }
        IEnumerable<DescribedProgress> Progress { get; }
        string ErrorText { get; }

        ImmutableHashSet<IQuality> Quality { get; }
    }

    public interface IConversionItem : IExportableConversionItem
    {
        Action<IConversionItem, DashEncodeResult> CompletionAction { get; }
        Action<Exception> ErrorAction { get; }
        CancellationTokenSource CancellationToken { get; }

        new IEnumerable<DescribedProgress> Progress { get; set; }
        new string ErrorText { get; set; }

        bool Equals(object obj);
        int GetHashCode();
        string ToString();
    }
}