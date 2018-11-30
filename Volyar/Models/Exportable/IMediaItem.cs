using System;

namespace Volyar.Models.Exportable
{
    public interface IMediaItem
    {
        DateTimeOffset CreateDate { get; set; }
        TimeSpan Duration { get; set; }
        string IndexHash { get; set; }
        string IndexName { get; set; }
        string LibraryName { get; set; }
        int MediaId { get; set; }
        string Name { get; set; }
        string SeriesName { get; set; }
        string SourceHash { get; set; }
        DateTimeOffset SourceModified { get; set; }
        string SourcePath { get; set; }
    }
}