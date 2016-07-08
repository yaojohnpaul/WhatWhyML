using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IE.Models
{
    public class Candidate : Token
    {
        public int Length { get; set; }

        public Candidate(String value, int pos, int length) : base(value, pos)
        {
            this.Length = length;
        }
    }
}
