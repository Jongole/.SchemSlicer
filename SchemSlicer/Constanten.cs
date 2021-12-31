using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemSlicer
{
    class Constanten
    {
        #region bytesNachTag
        //Gibt an wie viele Bytes nach dem Ende des Tag Wortes übersprungen werden müssen
        public static List<(string tagName, int anzahlBytes)> bytesNachTag = new List<(string tagName, int anzahlBytes)>
        {
            ("Schematic", 0),
            ("PaletteMax", 4),
            ("Palette", 0),
            ("Length", 2),
            ("Width", 2),
            ("Height", 2),
            ("Version", 4),
            ("Metadata", 0),
            ("WEOffsetX", 4),
            ("WEOffsetY", 4),
            ("WEOffsetZ", 5),
            ("DataVersion", 4),
            ("BlockData", 4),
            ("BlockEntities", 5),
            ("Offset", 16)
        };
        #endregion

        #region Tagtypen
        //Gibt die Anzahl der Bytes nach einem Tag an welche nicht Berücksichtigt werden müssen um Block Entities zu überspringen
        public static List<(int tagTypID, int bytesToSkip)> tagTypen = new List<(int tagTypID, int bytesToSkip)>
        {
            (1, 1),
            (2, 2),
            (3, 4),
            (4, 8),
            (5, 4),
            (6, 8)
        };
        #endregion

        //Anzahl an Tags die immer vorhanden sind
        public const int anzahlTags = 15;
    }
}
