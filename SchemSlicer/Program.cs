using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SchemSlicer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start reading .schem file: {0}", args[0]);
            var result = SchemReader.ReadSchem(args[0]);

            if(result.bloecke != null)
            {
                Console.WriteLine("Finished reading .schem file: {0}\n", args[0]);

                Console.WriteLine("Start Layer Generation.");
                CreateLayer.CreatePNGPerLayer(result.bloecke, result.palette, result.length, result.width, result.height);
                

                Console.WriteLine("Press <Enter> to end the programm");
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Error while Reading .schem file. Check your file and restart.\n");
                Console.WriteLine("Press <Enter> to end the programm");
                Console.ReadLine();
            }
        }
    }
}
