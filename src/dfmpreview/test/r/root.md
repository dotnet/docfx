
---
uid: root.md
title: A
---
### Yaml header

-------

## Cross reference (**cannot demo**)
[bing](BuildTest/program.cs)

## file include
[!include[linkAndRefRoot](b/linkAndRefRoot.md)]
[!include[refc](a/refc.md ""This is root"")]
[!include[refc_using_cache](a/refc.md)]
[!include[empty](empty.md)]
[!include[external](http://microsoft.com/a.md)]

------


## code Snippets Sample

[!code-csharp[Main](Program.cs)]


------

## Section define
> [!div class="tabbedCodeSnippets" data-resources="OutlookServices.Calendar"]
> ```cs
>       some cs code
> ```


-------
## Note
> [!NOTE]
> this is note1 , this is note2 , this is note3, this is note4 , this is note5 , this is note6 , this is note7 , this is note8

> this is a nother note

> [!WARNING]
> this is a warnig1,this is a warnig2,this is a warnig3,this is a warnig4,this is a warnig5,this is a warnig6

> this is another warning

--------
## Code block test
```cs
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
```
