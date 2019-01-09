using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace VolyDatabase
{
    public class MediaDatabase
    {
        public DbContextOptions<VolyContext> Database { get; set; }
    }
}
