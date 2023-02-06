using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildFromCSharpSourceCode;

public class CSharp
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");
    }

    /// <summary>
    /// This method should do something...
    /// </summary>
    /// <include file='../docs.xml' path='docs/members[@name="MyTests"]/Test/*'/>
    public void XmlCommentIncludeTag()
    {

    }
}
