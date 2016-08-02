using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Dfm_test
{
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                while (true)
                {
                    string num_str = Console.ReadLine();
                    //Console.WriteLine(tmp + '\n');
                    int num_of_row = System.Convert.ToInt32(num_str);
                    string str = "";
                    //get the path;
                    string path = Console.ReadLine();
                    string filename = Console.ReadLine();
                    for (int i = 0; i < num_of_row; i++)
                    {
                        //tmp = Console.ReadLine();
                        //Console.WriteLine(tmp + '\n');
                        str += Console.ReadLine();
                        str += "\r\n";
                    }

                    DfmServiceProvider myserviceprovider = new DfmServiceProvider();
                    MarkdownServiceParameters para = new MarkdownServiceParameters();
                    para.BasePath = path;
                    IMarkdownService myservice = myserviceprovider.CreateMarkdownService(para);
                    var result = myservice.Markup(str, filename).Html;


                    //string mdout = Microsoft.DocAsCode.Dfm.DocfxFlavoredMarked.Markup(str,"E:\\Test\\test2.md");
                    Console.Write(result + '\a');
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
