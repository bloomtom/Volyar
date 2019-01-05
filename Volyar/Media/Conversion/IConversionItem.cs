using System;
using System.Collections.Generic;
using DEnc;
using System.Collections.Immutable;

namespace Volyar.Media.Conversion
{
    public interface IExportableConversionItem
    {
        string SourcePath { get; }
        string OutputPath { get; }
        string OutputBaseFilename { get; }
        int Framerate { get; }
        DateTime CreateTime { get; }
        IEnumerable<DescribedProgress> Progress { get; }
        string ErrorText { get; }

        ImmutableHashSet<IQuality> Quality { get; }
    }

    public interface IConversionItem : IExportableConversionItem
    {
        Action<IConversionItem, DashEncodeResult> CompletionAction { get; }
        Action<Exception> ErrorAction { get; }

        new IEnumerable<DescribedProgress> Progress { get; set; }
        new string ErrorText { get; set; }

        bool Equals(object obj);
        int GetHashCode();
        string ToString();
    }
}