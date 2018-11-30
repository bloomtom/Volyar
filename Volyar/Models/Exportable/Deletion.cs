using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Volyar.Models.Exportable
{
    public class Deletion
    {
        public string Table { get; set; }
        public int Key { get; set; }

        public Deletion(string table, int key)
        {
            Table = table;
            Key = key;
        }
    }
}