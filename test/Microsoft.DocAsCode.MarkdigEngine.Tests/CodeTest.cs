// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Markdig.Syntax;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdigEngine.Extensions;
    using Microsoft.DocAsCode.Plugins;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Xunit;

    public class CodeTest
    {
        static public string LoggerPhase = "Code";
        static public string contentCSharp = @"using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TableSnippets
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class Window1 : Window
    {

        public Window1()
        {
            InitializeComponent();

            TableRowGroupsProperty();
        }

        void WindowLoaded(Object sender, RoutedEventArgs args)
        {
            TableColumnsProperty();
            TableRowGroupsProperty();
            TableRowGroupRows();
            TableCellConst();
        }

        void TableColumnsProperty()
        {

            // <Snippet_Table_Columns_Add>
            Table tbl = new Table();
            int columnsToAdd = 4;
            for (int x = 0; x < columnsToAdd; x++)
                tbl.Columns.Add(new TableColumn());
            // </Snippet_Table_Columns_Add>

            // Insert a new first column.
            // <Snippet_Table_Columns_Insert>
            tbl.Columns.Insert(0, new TableColumn());
            // </Snippet_Table_Columns_Insert>

            // Manipulating columns.
#region Snippet_Table_Columns_Manip
            tbl.Columns[0].Width = new GridLength(20);
            tbl.Columns[1].Background = Brushes.AliceBlue;
            tbl.Columns[2].Width = new GridLength(20);
            tbl.Columns[3].Background = Brushes.AliceBlue;
#endregion 

            // Get a count of columns hosted by the table.
            // <Snippet_Table_Columns_Count>
            int columns = tbl.Columns.Count;
            // </Snippet_Table_Columns_Count>

            // Remove a particular column by reference (the 4th).
            // <Snippet_Table_Columns_DelRef>
            tbl.Columns.Remove(tbl.Columns[3]);
            // </Snippet_Table_Columns_DelRef>

            // Remove a particular column by index (the 3rd).
            // <Snippet_Table_Columns_DelIndex>
            tbl.Columns.RemoveAt(2);
            // </Snippet_Table_Columns_DelIndex>

            // Remove all columns from the table's columns collection.
            // <Snippet_Table_Columns_Clear>
            tbl.Columns.Clear();
            // </Snippet_Table_Columns_Clear>
        }

        void TableRowGroupsProperty()
        {
            // Add rowgroups...
            // <Snippet_Table_RowGroups_Add>
            Table tbl = new Table();
            int rowGroupsToAdd = 4;
            for (int x = 0; x < rowGroupsToAdd; x++)
                tbl.RowGroups.Add(new TableRowGroup());
            // </Snippet_Table_RowGroups_Add>

            // Insert rowgroup...
            // <Snippet_Table_RowGroups_Insert>
            tbl.RowGroups.Insert(0, new TableRowGroup());
            // </Snippet_Table_RowGroups_Insert>

            // Adding rows to a rowgroup...
            {
                // <Snippet_Table_RowGroups_AddRows>
                int rowsToAdd = 10;
                for (int x = 0; x < rowsToAdd; x++)
                    tbl.RowGroups[0].Rows.Add(new TableRow());
                // </Snippet_Table_RowGroups_AddRows>
            }

            // Manipulating rows (through rowgroups)...

            // <Snippet_Table_RowGroups_ManipRows>
            // Alias the working TableRowGroup for ease in referencing.
            TableRowGroup trg = tbl.RowGroups[0];
            trg.Rows[0].Background = Brushes.CornflowerBlue;
            trg.Rows[1].FontSize = 24;
            trg.Rows[2].ToolTip = ""This row's tooltip"";
            // </Snippet_Table_RowGroups_ManipRows>

            // Adding cells to a row...
            {
                // <Snippet_Table_RowGroups_AddCells>
                int cellsToAdd = 10;
                for (int x = 0; x < cellsToAdd; x++)
                    tbl.RowGroups[0].Rows[0].Cells.Add(new TableCell(new Paragraph(new Run(""Cell "" + (x + 1)))));
                // </Snippet_Table_RowGroups_AddCells>
            }

            // Manipulating cells (through rowgroups)...

            // <Snippet_Table_RowGroups_ManipCells>
            // Alias the working for for ease in referencing.
            TableRow row = tbl.RowGroups[0].Rows[0];
            row.Cells[0].Background = Brushes.PapayaWhip;
            row.Cells[1].FontStyle = FontStyles.Italic;
            // This call clears all of the content from this cell.
            row.Cells[2].Blocks.Clear();
            // </Snippet_Table_RowGroups_ManipCells>

            // Count rowgroups...
            // <Snippet_Table_RowGroups_Count>
            int rowGroups = tbl.RowGroups.Count;
            // </Snippet_Table_RowGroups_Count>

            // Remove rowgroup by ref...
            // <Snippet_Table_RowGroups_DelRef>
            tbl.RowGroups.Remove(tbl.RowGroups[0]);
            // </Snippet_Table_RowGroups_DelRef>

            // Remove rowgroup by index...
            // <Snippet_Table_RowGroups_DelIndex>
            tbl.RowGroups.RemoveAt(0);
            // </Snippet_Table_RowGroups_DelIndex>

            // Remove all rowgroups...
            // <Snippet_Table_RowGroups_Clear>
            tbl.RowGroups.Clear();
            // </Snippet_Table_RowGroups_Clear>
        }

        void TableRowGroupRows()
        {
            // <Snippet_TableRowGroup_Rows>
            Table tbl = new Table();
            TableRowGroup trg = new TableRowGroup();

            tbl.RowGroups.Add(trg);

            // Add rows to a TableRowGroup collection.
            int rowsToAdd = 4; 
            for (int x = 0; x < rowsToAdd; x++)
                trg.Rows.Add(new TableRow());

            // Insert a new first row (at the zero-index position).
            trg.Rows.Insert(0, new TableRow());

            // Manipulate rows...

            // Set the background on the first row.
            trg.Rows[0].Background = Brushes.CornflowerBlue;
            // Set the font size on the second row.
            trg.Rows[1].FontSize = 24;
            // Set a tooltip for the third row.
            trg.Rows[2].ToolTip = ""This row's tooltip"";

            // Adding cells to a row...
            {
                int cellsToAdd = 10;
                for (int x = 0; x < cellsToAdd; x++)
                    trg.Rows[0].Cells.Add(new TableCell(new Paragraph(new Run(""Cell "" + (x + 1)))));
            }

            // Count rows.
            int rows = trg.Rows.Count;

            // Remove 1st row by reference.
            trg.Rows.Remove(trg.Rows[0]);

            // Remove all rows...
            trg.Rows.Clear();
            // </Snippet_TableRowGroup_Rows>        
        }

        void TableCellConst()
        {
            // <Snippet_TableCell_Const1>
            // A child Block element for the new TableCell element.
            Paragraph parx = new Paragraph(new Run(""A bit of text content...""));

            // After this line executes, the new element ""cellx""
            // contains the specified Block element, ""parx"".
            TableCell cellx = new TableCell(parx);
            // </Snippet_TableCell_Const1>
        }
    }
}";
        static public string contentVB = @"''<Snippet1>
Class ADSetupInformation

    Shared Sub Main()

        Dim root As AppDomain = AppDomain.CurrentDomain

        Dim setup As New AppDomainSetup()
        setup.ApplicationBase = _
            root.SetupInformation.ApplicationBase & ""MyAppSubfolder\""

        Dim domain As AppDomain = AppDomain.CreateDomain(""MyDomain"", Nothing, setup)

        Console.WriteLine(""Application base of {0}:"" & vbCrLf & vbTab & ""{1}"", _
            root.FriendlyName, root.SetupInformation.ApplicationBase)
        Console.WriteLine(""Application base of {0}:"" & vbCrLf & vbTab & ""{1}"", _
            domain.FriendlyName, domain.SetupInformation.ApplicationBase)

        AppDomain.Unload(domain)
    End Sub
End Class

' This example produces output similar to the following:
'
'Application base of MyApp.exe:
'        C:\Program Files\MyApp\
'Application base of MyDomain:
'        C:\Program Files\MyApp\MyAppSubfolder\
'</Snippet1>

<snippet2>
Imports System.Reflection

Class AppDomain1
    Public Shared Sub Main()
        Console.WriteLine(""Creating new AppDomain."")
        Dim domain As AppDomain = AppDomain.CreateDomain(""MyDomain"")

        Console.WriteLine(""Host domain: "" + AppDomain.CurrentDomain.FriendlyName)
        Console.WriteLine(""child domain: "" + domain.FriendlyName)
    End Sub
End Class
'</snippet2>";
        static public string contentCPP = @"//<Snippet1>
using namespace System;

int main()
{
    AppDomain^ root = AppDomain::CurrentDomain;

    AppDomainSetup^ setup = gcnew AppDomainSetup();
    setup->ApplicationBase = 
        root->SetupInformation->ApplicationBase + ""MyAppSubfolder\\"";

    AppDomain^ domain = AppDomain::CreateDomain(""MyDomain"", nullptr, setup);

    Console::WriteLine(""Application base of {0}:\r\n\t{1}"", 
        root->FriendlyName, root->SetupInformation->ApplicationBase);
    Console::WriteLine(""Application base of {0}:\r\n\t{1}"", 
        domain->FriendlyName, domain->SetupInformation->ApplicationBase);

    AppDomain::Unload(domain);
}

/* This example produces output similar to the following:
Application base of MyApp.exe:
        C:\Program Files\MyApp\
Application base of MyDomain:
        C:\Program Files\MyApp\MyAppSubfolder\
 */
//</Snippet1>


// <snippet2>
using namespace System;
using namespace System::Reflection;

ref class AppDomain4
{
public:
    static void Main()
    {
        // Create application domain setup information.
        AppDomainSetup^ domaininfo = gcnew AppDomainSetup();
        domaininfo->ApplicationBase = ""f:\\work\\development\\latest"";

        // Create the application domain.
        AppDomain^ domain = AppDomain::CreateDomain(""MyDomain"", nullptr, domaininfo);

        // Write application domain information to the console.
        Console::WriteLine(""Host domain: "" + AppDomain::CurrentDomain->FriendlyName);
        Console::WriteLine(""child domain: "" + domain->FriendlyName);
        Console::WriteLine(""Application base is: "" + domain->SetupInformation->ApplicationBase);

        // Unload the application domain.
        AppDomain::Unload(domain);
    }
};

int main()
{
    AppDomain4::Main();
}
// </snippet2>";


        private static MarkupResult SimpleMarkup(string source)
        {
            return TestUtility.MarkupWithoutSourceInfo(source, "Topic.md");
        }

        [Theory]
        [InlineData(@":::code source=""source.cs"" range=""11-33, 40-44"" highlight=""6-7"" language=""azurecli"" interactive=""try-dotnet"":::", @"<pre>
<code class=""lang-azurecli"" data-interactive=""azurecli"" data-interactive-mode=""try-dotnet"" highlight-lines=""6-7"">/// <summary>
/// Interaction logic for Window1.xaml
/// </summary>

public partial class Window1 : Window
{

    public Window1()
    {
        InitializeComponent();

        TableRowGroupsProperty();
    }

    void WindowLoaded(Object sender, RoutedEventArgs args)
    {
        TableColumnsProperty();
        TableRowGroupsProperty();
        TableRowGroupRows();
        TableCellConst();
    }

    void TableColumnsProperty()
    ...
   tbl.Columns.Add(new TableColumn());
// </Snippet_Table_Columns_Add>

// Insert a new first column.
// <Snippet_Table_Columns_Insert>
</code></pre>
")]
        [InlineData(@":::code source=""source.cs"" range=""1-2"" language=""azurecli"" interactive=""try-dotnet"":::", @"<pre>
<code class=""lang-azurecli"" data-interactive=""azurecli"" data-interactive-mode=""try-dotnet"">using System;
using System.Windows;
</code></pre>
")]
        [InlineData(@":::code source=""source.cs"" range=""1-2"" interactive=""try-dotnet"":::", @"<pre>
<code class=""lang-csharp"" data-interactive=""csharp"" data-interactive-mode=""try-dotnet"">using System;
using System.Windows;
</code></pre>
")]
        [InlineData(@":::code source=""source.cs"" range=""1-2,205-"" highlight=""6-7"" language=""azurecli"" interactive=""try-dotnet"":::", @"<pre>
<code class=""lang-azurecli"" data-interactive=""azurecli"" data-interactive-mode=""try-dotnet"" highlight-lines=""6-7"">using System;
using System.Windows;
    ...
       }
   }
}
</code></pre>
")]
        [InlineData(@":::code source=""source.cs"" id=""Snippet_Table_RowGroups_Add"" language=""azurecli"" interactive=""try-dotnet"":::", @"<pre>
<code class=""lang-azurecli"" data-interactive=""azurecli"" data-interactive-mode=""try-dotnet"">Table tbl = new Table();
int rowGroupsToAdd = 4;
for (int x = 0; x < rowGroupsToAdd; x++)
    tbl.RowGroups.Add(new TableRowGroup());
</code></pre>
")]
        [InlineData(@":::code source=""source.vb"" id=""snippet2"" interactive=""try-dotnet"":::", @"<pre>
<code class=""lang-vb"" data-interactive=""vb"" data-interactive-mode=""try-dotnet"">Imports System.Reflection

Class AppDomain1
    Public Shared Sub Main()
        Console.WriteLine(""Creating new AppDomain."")
        Dim domain As AppDomain = AppDomain.CreateDomain(""MyDomain"")

        Console.WriteLine(""Host domain: "" + AppDomain.CurrentDomain.FriendlyName)
        Console.WriteLine(""child domain: "" + domain.FriendlyName)
    End Sub
End Class
</code></pre>
")]
        [InlineData(@":::code source=""source.vb"" id=""snippet1"" interactive=""try-dotnet"":::", @"<pre>
<code class=""lang-vb"" data-interactive=""vb"" data-interactive-mode=""try-dotnet"">Class ADSetupInformation

    Shared Sub Main()

        Dim root As AppDomain = AppDomain.CurrentDomain

        Dim setup As New AppDomainSetup()
        setup.ApplicationBase = _
            root.SetupInformation.ApplicationBase & ""MyAppSubfolder\""

        Dim domain As AppDomain = AppDomain.CreateDomain(""MyDomain"", Nothing, setup)

        Console.WriteLine(""Application base of {0}:"" & vbCrLf & vbTab & ""{1}"", _
            root.FriendlyName, root.SetupInformation.ApplicationBase)
        Console.WriteLine(""Application base of {0}:"" & vbCrLf & vbTab & ""{1}"", _
            domain.FriendlyName, domain.SetupInformation.ApplicationBase)

        AppDomain.Unload(domain)
    End Sub
End Class

' This example produces output similar to the following:
'
'Application base of MyApp.exe:
'        C:\Program Files\MyApp\
'Application base of MyDomain:
'        C:\Program Files\MyApp\MyAppSubfolder\
</code></pre>
")]
        [InlineData(@":::code source=""source.cpp"" id=""snippet2"":::", @"<pre>
<code class=""lang-cpp"">using namespace System;
using namespace System::Reflection;

ref class AppDomain4
{
public:
    static void Main()
    {
        // Create application domain setup information.
        AppDomainSetup^ domaininfo = gcnew AppDomainSetup();
        domaininfo->ApplicationBase = ""f:\\work\\development\\latest"";

        // Create the application domain.
        AppDomain^ domain = AppDomain::CreateDomain(""MyDomain"", nullptr, domaininfo);

        // Write application domain information to the console.
        Console::WriteLine(""Host domain: "" + AppDomain::CurrentDomain->FriendlyName);
        Console::WriteLine(""child domain: "" + domain->FriendlyName);
        Console::WriteLine(""Application base is: "" + domain->SetupInformation->ApplicationBase);

        // Unload the application domain.
        AppDomain::Unload(domain);
    }
};

int main()
{
    AppDomain4::Main();
}
</code></pre>
")]
        [InlineData(@":::code source=""source.cpp"" id=""snippet2"":::

hi
:::code source=""source.cpp"" id=""snippet1"":::
", @"<pre>
<code class=""lang-cpp"">using namespace System;
using namespace System::Reflection;

ref class AppDomain4
{
public:
    static void Main()
    {
        // Create application domain setup information.
        AppDomainSetup^ domaininfo = gcnew AppDomainSetup();
        domaininfo->ApplicationBase = ""f:\\work\\development\\latest"";

        // Create the application domain.
        AppDomain^ domain = AppDomain::CreateDomain(""MyDomain"", nullptr, domaininfo);

        // Write application domain information to the console.
        Console::WriteLine(""Host domain: "" + AppDomain::CurrentDomain->FriendlyName);
        Console::WriteLine(""child domain: "" + domain->FriendlyName);
        Console::WriteLine(""Application base is: "" + domain->SetupInformation->ApplicationBase);

        // Unload the application domain.
        AppDomain::Unload(domain);
    }
};

int main()
{
    AppDomain4::Main();
}
</code></pre>
<p>hi</p>
<pre>
<code class=""lang-cpp"">using namespace System;

int main()
{
    AppDomain^ root = AppDomain::CurrentDomain;

    AppDomainSetup^ setup = gcnew AppDomainSetup();
    setup->ApplicationBase = 
        root->SetupInformation->ApplicationBase + ""MyAppSubfolder\\"";

    AppDomain^ domain = AppDomain::CreateDomain(""MyDomain"", nullptr, setup);

    Console::WriteLine(""Application base of {0}:\r\n\t{1}"", 
        root->FriendlyName, root->SetupInformation->ApplicationBase);
    Console::WriteLine(""Application base of {0}:\r\n\t{1}"", 
        domain->FriendlyName, domain->SetupInformation->ApplicationBase);

    AppDomain::Unload(domain);
}

/* This example produces output similar to the following:
Application base of MyApp.exe:
        C:\Program Files\MyApp\
Application base of MyDomain:
        C:\Program Files\MyApp\MyAppSubfolder\
 */
</code></pre>
")]
        public void CodeTestBlockGeneral(string source, string expected)
        {
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            // arrange
            if (source.Contains("source.cs"))
            {
                File.WriteAllText("source.cs", contentCSharp.Replace("\r\n", "\n"));
            }
            else if (source.Contains("source.vb"))
            {
                File.WriteAllText("source.vb", contentVB.Replace("\r\n", "\n"));
            }
            else if (source.Contains("source.cpp"))
            {
                File.WriteAllText("source.cpp", contentCPP.Replace("\r\n", "\n"));
            }

            MarkupResult marked;
            // act
            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                marked = SimpleMarkup(source);
            }
            Logger.UnregisterListener(listener);

            // assert
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
            
        }

        [Theory]
        [InlineData(@":::code source=""source.cs"" range=""205-250"" language=""azurecli"" interactive=""try-dotnet"":::", @"Invalid code on line 0. "":::code source=""source.cs"" range=""205-250"" language=""azurecli"" interactive=""try-dotnet"":::"" is invalid. Your range is greater than the number of lines in the document.")]
        [InlineData(@":::code source=""source.cs"" badattribute=""ham"" range=""1-5"" language=""azurecli"" interactive=""try-dotnet"":::", @"Invalid code on line 0. "":::code source=""source.cs"" badattribute=""ham"" range=""1-5"" language=""azurecli"" interactive=""try-dotnet"":::"" is invalid. Unexpected attribute ""badattribute"".")]
        [InlineData(@":::code source=""source.cs"" id=""id"" range=""1-5"" language=""azurecli"" interactive=""try-dotnet"":::", @"Invalid code on line 0. "":::code source=""source.cs"" id=""id"" range=""1-5"" language=""azurecli"" interactive=""try-dotnet"":::"" is invalid. You must set only either Range or Id, but not both.")]
        [InlineData(@":::code range=""1-5"" language=""azurecli"" interactive=""try-dotnet"":::", @"Invalid code on line 0. "":::code range=""1-5"" language=""azurecli"" interactive=""try-dotnet"":::"" is invalid. source is a required attribute. Please ensure you have specified a source attribute")]
        public void CodeTestBlockGeneralCSharp_Error(string source, string expected)
        {
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            // arrange
            if (source.Contains("source.cs"))
            {
                File.WriteAllText("source.cs", contentCSharp.Replace("\r\n", "\n"));
            }
            else if (source.Contains("source.vb"))
            {
                File.WriteAllText("source.vb", contentVB.Replace("\r\n", "\n"));
            }

            MarkupResult marked;
            // act
            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                marked = SimpleMarkup(source);
            }
            Logger.UnregisterListener(listener);

            // assert
            Assert.Equal(expected, listener.Items.FirstOrDefault().Message);

        }

    }
}
