using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.IO;

namespace SchemSlicer
{
    class SchemReader
    {
        #region GetNextTagLength
        public static (int laenge, int neuePosition) GetNextTagLength(int aktuellePosition, byte[] schem)
        {            
            aktuellePosition += 2;  //springt 2 Bytes weiter da hier die Länge des Tags steht

            return (Convert.ToInt32(schem[aktuellePosition]), aktuellePosition);    //Gibt die Länge des nächsten Tags mit der neuen position zurück
        }
        #endregion

        #region GetNextTagName
        public static string GetNextTagName(int tagAnfang, int tagLaenge, byte[] schem)
        {
            string tag = null;

            //Nimmt vom Anfang des Tags an die Anzahl an Bytes und fügt sie zu einem String zusammen
            for(int i = tagAnfang; i < tagAnfang + tagLaenge; i++)
            {
                tag += Convert.ToChar(schem[i]);
            }

            return tag;
        }
        #endregion

        #region GetBlockMitID
        public static (string tagName, int id, int neuePosition) GetBlockMitID(int aktuellePosition, byte[] schem)
        {
            //holt die Länge des nächsten Tags
            var result = SchemReader.GetNextTagLength(aktuellePosition, schem);

            aktuellePosition = result.neuePosition;

            //holt den nächsten Tag
            string tag = SchemReader.GetNextTagName(aktuellePosition + 1, result.laenge, schem);

            //addiert zur anfangsposition die länge des Tags + 4 da nach den Blöcken noch 4 Bytes kommen bevor das nächste Tag beginnt
            aktuellePosition += result.laenge + 4;
                       
            //Gibt den Tag(Blockname Z.B. minecraft:clay), ID dieses Blockes in der .schem Datei und die neue Position zurück
            return (tag, GetInt32FromBytes(schem, aktuellePosition), aktuellePosition);
        }
        #endregion

        #region Bytes zu Int
        #region Int32
        //Nimmt die Position des letzten Bytes von 4 und generiert daraus ein Int32
        public static int GetInt32FromBytes(byte[] schem, int positionLetztesByte)
        {
            byte[] tmp = new byte[4];
            tmp[0] = schem[positionLetztesByte - 3];
            tmp[1] = schem[positionLetztesByte - 2];
            tmp[2] = schem[positionLetztesByte - 1];
            tmp[3] = schem[positionLetztesByte];
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);
            return BitConverter.ToInt32(tmp, 0);
        }
        #endregion

        #region Int16
        //Nimmt die Position des letzten Bytes von 2 und generiert daraus ein Int32 welches nur maximal den Wert eines Int16 haben kann
        public static int GetInt16FromBytes(byte[] schem, int positionLetztesByte)
        {
            byte[] tmp = new byte[4];
            tmp[2] = schem[positionLetztesByte - 1];
            tmp[3] = schem[positionLetztesByte];

            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);
            return BitConverter.ToInt32(tmp, 0);
        }
        #endregion
        #endregion

        #region ReadSchem
        public static (int[] bloecke, string[] palette, int length, int width, int height) ReadSchem(string path)
        {
            int width, height, length, aktuellesByte, bytesToSkip, anzahlBlockBytes = 0, anzahlEntityTags = 0, gefundeneEntityTags = 0, paletteMax, tagdatentyp;
            byte[] schem;
            List<int> bloecke;
            string[] palette;
            string tag;
            List<TagPositionen> tagPositionen;

            try
            {
                schem = File.ReadAllBytes(path);

                //lese die Anzahl der unter Tags von Palette aus und erstelle ein Array dieser größe
                paletteMax = GetInt32FromBytes(schem, 28);
                palette = new string[paletteMax];

                tagPositionen = new List<TagPositionen>();
                aktuellesByte = 0;

                for (int i = 0; i < Constanten.anzahlTags; i++)
                {
                    //holt die Länge des nächsten Tags
                    var result = GetNextTagLength(aktuellesByte, schem);

                    aktuellesByte = result.neuePosition;

                    //holt den nächsten Tag
                    tag = GetNextTagName(aktuellesByte + 1, result.laenge, schem);

                    //Fügt das Tag in die Liste mit der dazugehörigen Position des ersten Bytes
                    tagPositionen.Add(new TagPositionen(tag, aktuellesByte + 1));

                    #region Bytes To Skip
                    //holt sich zum Tag die Anzahl der Bytes die nach dem Tag übersprungen werden müssen
                    foreach (var tuple in Constanten.bytesNachTag)
                    {
                        if (tuple.tagName == tag)
                        {
                            bytesToSkip = tuple.anzahlBytes;

                            //errechnet die Anfangsposition des nächsten Tags
                            aktuellesByte += bytesToSkip + result.laenge + 1;
                            break;
                        }
                    }
                    #endregion

                    #region Verschiedene Blöcke auslesen
                    if (tag == "Palette")
                    {
                        for (int p = 0; p < paletteMax; p++)
                        {
                            //holt den Block mit seiner ID
                            var result2 = GetBlockMitID(aktuellesByte, schem);

                            //schreibt den Block an die Position seiner ID Z.B. ID=1 -> Array[1]
                            tag = result2.tagName;
                            //Schneidet details der Block ID ab welche in einem 2D Bild nicht unterschiedlich dargestellt werden können 
                            tag = tag.Split('[')[0];
                            tag = tag.Replace("_stairs", "");
                            tag = tag.Replace("_pane", "");
                            tag = tag.Replace("_slab", "");
                            tag = tag.Replace("redstone_wire", "redstone_dust_line1");
                            palette[result2.id] = tag;
                            //ändere den Zähler für das Aktuelle Byte
                            aktuellesByte = result2.neuePosition + 1;
                        }
                        //Überspringe ein weitere Byte nach dem Nächsten Block
                        aktuellesByte += 1;
                    }
                    #endregion

                    #region Anzahl Blöcke
                    if (tag == "BlockData")
                    {
                        //holt aus dem letzten Byte des Tags BlockData die Anzahl der Blöcke und über springt diese
                        anzahlBlockBytes = GetInt32FromBytes(schem, aktuellesByte - 1);
                        aktuellesByte += anzahlBlockBytes;
                    }
                    #endregion

                    #region Block Entities
                    if(tag == "BlockEntities")
                    {
                        //Anzahl der Compound Tag die zu BlockEntities gehören
                        anzahlEntityTags = GetInt32FromBytes(schem, aktuellesByte - 1);                        
                        
                        if(anzahlEntityTags != 0)
                        {
                            //Wiederholung so lange nicht alle Tag Compounds gefunden und übersprungen wurden
                            while (anzahlEntityTags != gefundeneEntityTags)
                            {
                                tagdatentyp = schem[aktuellesByte];

                                //Prüfung was für ein Datentyp das nächste Tag hat und Anschließendes überspringen des Tags 
                                switch (tagdatentyp)
                                {
                                    case 0:     //TAG_End
                                        gefundeneEntityTags++;
                                        aktuellesByte++;
                                        break;
                                    case 1:     //TAG_Byte
                                        //Überspringe 2 Bytes welche die Länge des Variablen Namens des Tags Beinhalten
                                        aktuellesByte += 2;
                                        //Überspringe die Bytes die den Variablen Namen enthalten,
                                        //+ 1 Byte der den Inhalt der Variable beinhaltet und + 1 um am Anfang des nächsten Tags zu sein
                                        aktuellesByte += GetInt16FromBytes(schem, aktuellesByte) + 2;                                        
                                        break;
                                    case 2:     //TAG_Short
                                        //Überspringe 2 Bytes welche die Länge des Variablen Namens des Tags Beinhalten
                                        aktuellesByte += 2;
                                        //Überspringe die Bytes die den Variablen Namen enthalten,
                                        //+ 2 Byte die den Inhalt der Variable beinhaltet und + 1 um am Anfang des nächsten Tags zu sein
                                        aktuellesByte += GetInt16FromBytes(schem, aktuellesByte) + 3;
                                        break;
                                    case 3:     //TAG_Int
                                        //Überspringe 2 Bytes welche die Länge des Variablen Namens des Tags Beinhalten
                                        aktuellesByte += 2;
                                        //Überspringe die Bytes die den Variablen Namen enthalten,
                                        //+ 4 Byte die den Inhalt der Variable beinhaltet und + 1 um am Anfang des nächsten Tags zu sein
                                        aktuellesByte += GetInt16FromBytes(schem, aktuellesByte) + 5;
                                        break;
                                    case 4:      //TAG_Long
                                        //Überspringe 2 Bytes welche die Länge des Variablen Namens des Tags Beinhalten
                                        aktuellesByte += 2;
                                        //Überspringe die Bytes die den Variablen Namen enthalten,
                                        //+ 8 Byte die den Inhalt der Variable beinhaltet und + 1 um am Anfang des nächsten Tags zu sein
                                        aktuellesByte += GetInt16FromBytes(schem, aktuellesByte) + 9;
                                        break;
                                    case 5:     //TAG_Float
                                        //Überspringe 2 Bytes welche die Länge des Variablen Namens des Tags Beinhalten
                                        aktuellesByte += 2;
                                        //Überspringe die Bytes die den Variablen Namen enthalten,
                                        //+ 4 Byte die den Inhalt der Variable beinhaltet und + 1 um am Anfang des nächsten Tags zu sein
                                        aktuellesByte += GetInt16FromBytes(schem, aktuellesByte) + 5;
                                        break;
                                    case 6:     //TAG_Double
                                        //War bei unseren Test nie in einer .schem Datei vorhanden in BlockEntities
                                        Console.WriteLine("Unerwartetes Double Tag: " + aktuellesByte);
                                        break;
                                    case 7:     //TAG_Byte_Array
                                        //War bei unseren Test nie in einer .schem Datei vorhanden in BlockEntities
                                        Console.WriteLine("Unerwartetes Byte-Array Tag: " + aktuellesByte);
                                        break;
                                    case 8:     //TAG_String
                                        //Überspringe 2 Bytes welche die Länge des Variablen Namens des Tags Beinhalten
                                        aktuellesByte += 2;
                                        //Überspringe die Bytes die den Variablen Namen enthalten,
                                        //+ 2 Byte welche die Länge des darauf Folgenden strings angeben
                                        aktuellesByte += GetInt16FromBytes(schem, aktuellesByte) + 2;
                                        //Überspringe die Bytes welche den String Representieren
                                        aktuellesByte += GetInt16FromBytes(schem, aktuellesByte) + 1;                                        
                                        break;
                                    case 9:     //TAG_List
                                        //Überspringe 2 Bytes welche die Länge des Variablen Namens des Tags Beinhalten
                                        aktuellesByte += 2;
                                        //Überspringe die Bytes die den Variablen Namen enthalten,
                                        //+ 4 Byte welche die Anzahl der Tags in der TAG_List enthalten
                                        aktuellesByte += GetInt16FromBytes(schem, aktuellesByte) + 5;
                                        //Ziehe die Anzahl der Tags in der Tag List von gefundeneEntityTags ab da jedes Tag in der Tag List von einem TAG_End abgeschlossen werden
                                        //und diese Tags nicht in der Gesamtzahl der Tags mit einbezogen werden
                                        gefundeneEntityTags -= GetInt32FromBytes(schem, aktuellesByte);
                                        aktuellesByte++;
                                        break;
                                    case 10:     //TAG_Compound
                                        //öffnet ein weiteres Tag Compound weshalb die gefundene Anzahl runtergezählt werden muss da nach dem Tag ein weiteres END-Tag kommt
                                        //welches in der Tag Anzahl von Block Entities nicht mit inbegriffen ist
                                        gefundeneEntityTags--;
                                        //Überspringe 2 Bytes welche die Länge des Variablen Namens des Tags Beinhalten
                                        aktuellesByte += 2;
                                        //Überspringe die Bytes die den Variablen Namen enthalten,
                                        //und + 1 um am Anfang des nächsten Tags zu sein
                                        aktuellesByte += GetInt16FromBytes(schem, aktuellesByte) + 1;
                                        break;
                                    case 11:     //TAG_Int_Array
                                        //Überspringe 2 Bytes welche die Länge des Variablen Namens des Tags Beinhalten
                                        aktuellesByte += 2;
                                        //Überspringe die Bytes die den Variablen Namen enthalten,
                                        //+ 4 Byte welche die Anzahl an Ints im Array beinhaltet
                                        aktuellesByte += GetInt16FromBytes(schem, aktuellesByte) + 4;
                                        //Überspringe die Anzahl an Ints im Array *4 da jeder Int aus 4 Bytes besteht und + 1 um am Anfang des nächsten Tags zu sein
                                        aktuellesByte += GetInt32FromBytes(schem, aktuellesByte) * 4 + 1;
                                        break;
                                    case 12:     //TAG_Long_Array
                                        //War bei unseren Test nie in einer .schem Datei vorhanden in BlockEntities
                                        Console.WriteLine("Unerwartetes Long-Array Tag: " + aktuellesByte);
                                        break;
                                }
                            }
                        }                        
                    }
                    #endregion
                }

                //hole Länge, breite und höhe aus der datei
                length = GetInt16FromBytes(schem, tagPositionen[4].tagAnfang + 7);
                width = GetInt16FromBytes(schem, tagPositionen[13].tagAnfang + 6);
                height = GetInt16FromBytes(schem, tagPositionen[9].tagAnfang + 7);

                #region Blöcke in Array schreiben
                if (anzahlBlockBytes != 0)
                {
                    //erstellt ein Array mit der Größe der Anzahl an Blöcken
                    bloecke = new List<int>();

                    //holt die Blöcke der Reihenfolge nach aus der .schem und schreibt sie in das Array
                    for (int i = 0; i < anzahlBlockBytes; i++)
                    {
                        //Wenn das erste Byte, welches die ID des Blocks angibt, 127 überschreitet wird automatisch ein zweites Byte, das darauf folgende, benutzt um die ID darzustellen
                        if(schem[tagPositionen[11].tagAnfang + 13 + i] >= 128)
                        {
                            if (schem[tagPositionen[11].tagAnfang + 13 + i + 1] >= 128)
                            {
                                //Wenn die ID aus 2 Bytes besteht muss von dem ersten Byte 128 abgezogen werden und dazu wird der Wert, aus dem (2. Byte - 128) * 128 und 3. Byte * 128 * 128, addiert
                                bloecke.Add(schem[tagPositionen[11].tagAnfang + 13 + i] - 128 + (schem[tagPositionen[11].tagAnfang + 13 + i + 1] - 128) * 128 + schem[tagPositionen[11].tagAnfang + 13 + i + 2] * 128 * 128);
                                i++;
                            }
                            else
                            {
                                //Wenn die ID aus 2 Bytes besteht muss von dem ersten Byte 128 abgezogen werden und dazu wird der Wert, aus dem 2. Byte * 128, addiert
                                bloecke.Add(schem[tagPositionen[11].tagAnfang + 13 + i] - 128 + schem[tagPositionen[11].tagAnfang + 13 + i + 1] * 128);                                
                            }
                            i++;
                        }
                        else
                        {
                            //Bei IDs bis einschließlich 127 muss nicht beachtet werden und der Wert aus dem Byte kann so benutzt werden
                            bloecke.Add(schem[tagPositionen[11].tagAnfang + 13 + i]);
                        }
                    }

                    return (bloecke.ToArray(), palette, length, width, height);
                }
                else
                {
                    return (null, palette, length, width, height);
                }
                #endregion
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return (null, null, 0, 0, 0);
            }
        }
        #endregion
    }
}
