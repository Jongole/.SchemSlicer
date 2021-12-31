using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SchemSlicer
{
    class CreateLayer
    {
        #region load Images
        private static List<Image> loadImagesFromPalette(string[] palette)
        {
            List<Image> texturen = new List<Image>();
            string blocktmp = "";
            int zuLadendeTexturen = palette.Length, geladeneTexturen = 0;
            double prozent;
            try
            {
                Image debugTexture = Image.FromFile(@".\block\debug.png");

                Console.WriteLine("\nStart loading Textures.");
                foreach (string block in palette)
                {
                    blocktmp = block.Replace("minecraft:", "");
                    try
                    {
                        texturen.Add(Image.FromFile(@".\block\" + blocktmp + ".png"));
                        geladeneTexturen++;
                    }
                    catch (Exception)
                    {
                        texturen.Add(debugTexture);
                        geladeneTexturen++;
                    }

                    //Console.SetCursorPosition(1, 0);
                    prozent = geladeneTexturen * 100.0 / zuLadendeTexturen;
                    Console.Write("\r{0:F}% of needed Textures loaded.", prozent);                    
                }
                Console.WriteLine("\nAll needed Textures loaded\n");
                return texturen;
            }
            catch(Exception)
            {
                Console.WriteLine("debug.png Textur nicht gefunden.");
                return null;
            }            
        }       
        #endregion

        #region Create Layer PNG
        public static void CreatePNGPerLayer(int[]bloecke, string[]palette, int length, int width, int height)
        {
            List<Image> texturen = loadImagesFromPalette(palette);

            if(texturen != null)
            {
                int blockStelle = 0;

                if (!Directory.Exists(@".\Layer Output\"))
                {
                    Directory.CreateDirectory(@".\Layer Output\");
                }

                //Länge und Breite mit 16 multiplizieren, für die Größe der Bitmap da eine Textur 16x16 pixel ist
                length *= 16;
                width *= 16;

                using (Bitmap b = new Bitmap(width, length))
                {
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.Clear(Color.Transparent); //BackGround

                        Font drawFont = new Font("Arial", 10);
                        StringFormat drawFormat = new StringFormat();
                        drawFormat.FormatFlags = StringFormatFlags.DirectionVertical;

                        #region Images
                        //For Schleife für Y Koordinate
                        for (int ycord = 0; ycord < height; ycord++)
                        {
                            Console.WriteLine("Layer: " + ycord);
                            //For Schleife für Z Koordinate
                            for (int zcord = 0; zcord < length; zcord += 16)
                            {
                                //For Schleife für X Koordinate
                                for (int xcord = 0; xcord < width; xcord += 16)
                                {
                                    g.DrawImage(texturen[bloecke[blockStelle]], xcord, zcord);

                                    blockStelle++;
                                }
                            }
                            #region Grid 
                            Pen blackPen = new Pen(Color.Black, 1);

                            for (int y = 0; y < (length); ++y)
                            {
                                g.DrawLine(blackPen, 0, y * 16, (width / 16) * 16, y * 16);
                            }

                            for (int x = 0; x < (width); ++x)
                            {
                                g.DrawLine(blackPen, x * 16, 0, x * 16, (length / 16) * 16);
                            }
                            #endregion

                            b.Save(@".\Layer Output\" + ycord + ".png", ImageFormat.Png);

                            g.Clear(Color.Transparent);
                        }

                        #endregion
                    }

                }
                Console.WriteLine("\nLayer Generation finished.");
            }
            else
            {
                Console.WriteLine("\nCould not find textures.");
            }
        }
        #endregion
    }
}
