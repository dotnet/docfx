using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Dfm_test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                while (true)
                {
                    string tmp = Console.ReadLine();
                    //Console.WriteLine(tmp + '\n');
                    int num_of_row = System.Convert.ToInt32(tmp);
                    string str = "";
                    for (int i = 0; i < num_of_row; i++)
                    {
                        tmp = Console.ReadLine();
                        //Console.WriteLine(tmp + '\n');
                        str += tmp;
                        str += "\r\n";
                    }            
                    string mdout = Microsoft.DocAsCode.Dfm.DocfxFlavoredMarked.Markup(str);
                    Console.Write(mdout + '\a');
                }

            }
            catch (IOException e)
            {
                Console.WriteLine(" error");
                Console.WriteLine(e.ToString());
                Console.ReadKey();
                return;
            }


        }
    }
}
