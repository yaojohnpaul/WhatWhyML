using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IE.Models
{
    //enum NamedEntity
    //{
    //    PER,
    //    LOC,
    //    DATE,
    //    ORG
    //}

    public class Token
    {
        public static readonly String[] NamedEntityTags = {
            "PER", "LOC", "DATE", "ORG"
        };

        public static readonly String[] PartOfSpeechTags = {
            "PRC", "CDB", "PP", "PPIN", "LM", "NNC", "VBTR", "JJCC", "JJC", "PR", "VBTS", "JJD", "PRF", "PRI", "JJCN", "PRL", "PRO", "PRN", "PRQ", "JJN", "PRP", "PRS", "DT", "NNP", "JJCS", "RBC", "RBB", "RBD", "NNPA", "RBF", "RBI", "RBK", "RBM", "VBH", "RBL", "RBN", "RBQ", "RBP", "VBL", "RBR", "VBN", "RBT", "RBW", "VBS", "VBW", "VBOF", "VB", "DTPP", "RB", "DTC", "VBTF", "NN", "JJ", "PPA", "DTP", "PPD", "PROP", "PPF", "VBRF", "PPM", "PPL", "PPO", "PPR", "PPU", "PPTS", "DTCP", "CCA", "CC", "CD", "CCC", "CCB", "CCD", "PMC", "PME", "CCP", "PMP", "PPBY", "CCR", "CCT", "PMQ", "PMS", "VBAF", "PRSP", "PM"
        };

        public String Value { get; set; }

        public int Sentence { get; set; }

        public int Position { get; set; }

        public String PartOfSpeech { get; set; }

        public String NamedEntity { get; set; }

        public int Frequency { get; set; }

        public Boolean IsWho { get; set; }

        public Boolean IsWhen { get; set; }

        public Boolean IsWhere { get; set; }

        public Boolean IsWhat { get; set; }

        public Boolean IsWhy { get; set; }
        
        public Token(String value, int position)
        {
            Value = value;
            Position = position;
        }
    }
}
