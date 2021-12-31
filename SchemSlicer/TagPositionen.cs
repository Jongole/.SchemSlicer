using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemSlicer
{
    class TagPositionen
    {
        public string tagName;  //Name des Tags
        public int tagAnfang;   //Wie vieltes Byte in der .schem Datei der Tag anfängt

        public TagPositionen(string name, int anfang)
        {
            tagName = name;
            tagAnfang = anfang;
        }
    }
}
