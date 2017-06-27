using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Divisas2.Models
{
    public class Preference
    {
        [PrimaryKey, AutoIncrement]
        public int PreferenceId { get; set; }
        public double SourceRate { get; set; }
        public double TargetRate { get; set; }
        public override int GetHashCode()
        {
            return PreferenceId;
        }

    }
}
