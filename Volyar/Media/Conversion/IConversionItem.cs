using System;
using System.Collections.Generic;
using DEnc;
using System.Collections.Immutable;

namespace Volyar.Media.Conversion
{
    public interface IExportableConversionItem
    {
        string SourcePath { get; }
        string DestinationDirectory { get; }
        string OutputBaseFilename { get; }
        int Framerate { get; }
        DateTime CreateTime { get; }
        float Progress { get; }

        ImmutableHashSet<IQuality> Quality { get; }
    }

    public interface IConversionItem : IExportableConversionItem
    {
        Action<DashEncodeResult> CompletionAction { get; }
        Action<Exception> ErrorAction { get; }

        new float Progress { get; set; }

        bool Equals(object obj);
        int GetHashCode();
        string ToString();
    }
}