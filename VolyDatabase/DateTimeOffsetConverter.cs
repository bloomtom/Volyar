using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace VolyDatabase
{

    public class DateTimeOffsetToTicksConverter(ConverterMappingHints mappingHints = null) : ValueConverter<DateTimeOffset, long>(
            v => v.UtcDateTime.Ticks,
            v => new DateTimeOffset(v, new TimeSpan(0, 0, 0)),
            mappingHints)
    {
        public static ValueConverterInfo DefaultInfo { get; }
            = new(typeof(DateTimeOffset), typeof(long), i => new DateTimeOffsetToTicksConverter(i.MappingHints));
    }
}
