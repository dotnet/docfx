// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmProcess
{
    using System;
    using System.IO;
    using System.Text;

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
                    int num_of_row = Convert.ToInt32(num_str);          //a simple protocal
                    StringBuilder str = new StringBuilder();

                    //get the path;
                    string path = Console.ReadLine();                   //path -> basedir
                    string filename = Console.ReadLine();               //filename -> the relative path of the current file 
                    for (int i = 0; i < num_of_row; i++)
                    {
                        str.AppendLine(Console.ReadLine());
                    }
                    DfmServiceProvider myserviceprovider = new DfmServiceProvider();
                    MarkdownServiceParameters para = new MarkdownServiceParameters();
                    para.BasePath = path;
                    IMarkdownService myservice = myserviceprovider.CreateMarkdownService(para);
                    var result = myservice.Markup(str.ToString(), filename).Html;

                    Console.Write(result + '\a');
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(" error");
                Console.WriteLine(e.ToString());
                return;
            }
        }
    }
}
