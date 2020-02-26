// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System.Collections.Generic;
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
            // <Snippet_inner>
            int rowGroupsToAdd = 4;
            // </Snippet_inner>
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
        static public string contentCharpRegion = @"using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TagHelpersBuiltIn
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            #region snippet_AllowAreas
            services.AddMvc()
                    .AddRazorPagesOptions(options => options.AllowAreas = true);
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(""/Error"");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            #region snippet_UseMvc
            app.UseMvc(routes =>
            {
                // need route and attribute on controller: [Area(""Blogs"")]
                routes.MapRoute(name: ""mvcAreaRoute"",
                                template: ""{area:exists}/{controller=Home}/{action=Index}"");

                // default route for non-areas
#region inner
                routes.MapRoute(
                    name: ""default"",
#endregion
                    template: ""{controller=Home}/{action=Index}/{id?}"");
            });
            #endregion
        }
    }
}";
        static public string contentASPNet = @"@{
    ViewData[""Title""] = ""Anchor Tag Helper"";
}

<table class=""table table-hover"">
    <caption>Anchor Tag Helper attribute examples</caption>
    <thead>
        <tr>
            <th>Attribute</th>
            <th>Markup</th>
            <th>Result</th>
        </tr>
    </thead>
<!-- <snippet_BigSnippet> -->
    <tbody>
        <tr>
            <td>asp-action</td>
            <td>
                <code>
                    @Html.Raw(Html.Encode(@""<a asp-controller=""""Speaker"""" asp-action=""""Evaluations"""">Speaker Evaluations</a>""))
                </code>
            </td>
            <td>
                <!-- <snippet_AspAction> -->
                <a asp-controller=""Speaker""
                   asp-action=""Evaluations"">Speaker Evaluations</a>
                <!-- </snippet_AspAction> -->
            </td>
        </tr>
        <tr>
            <td>asp-all-route-data</td>
            <td>
                <code>
                    @Html.Raw(Html.Encode(@""<a asp-route=""""speakerevalscurrent"""" asp-all-route-data=""""parms"""">Speaker Evaluations</a>""))
                </code>
            </td>
            <td>
                <!-- <snippet_AspAllRouteData> -->
                @{
                var parms = new Dictionary<string, string>
                            {
                                { ""speakerId"", ""11"" },
                                { ""currentYear"", ""true"" }
                            };
                }

                <a asp-route=""speakerevalscurrent""
                   asp-all-route-data=""parms"">Speaker Evaluations</a>
                <!-- </snippet_AspAllRouteData> -->
            </td>
        </tr>
        <tr>
            <td rowspan=""2"">asp-area</td>
            <td>
                <code>
                    @Html.Raw(Html.Encode(@""<a asp-area=""""Blogs"""" asp-controller=""""Home"""" asp-action=""""AboutBlog"""">About Blog</a>""))
                </code>
            </td>
            <td>
                <!-- <snippet_AspArea> -->
                <a asp-area=""Blogs""
                   asp-controller=""Home""
                   asp-action=""AboutBlog"">About Blog</a>
                <!-- </snippet_AspArea> -->
            </td>
        </tr>
        <tr>
            <td>
                <code>
                    @Html.Raw(Html.Encode(@""<a asp-area=""""Sessions"""" asp-page=""""/Index"""">View Sessions</a>""))
                </code>
            </td>
            <td>
                <!-- <snippet_AspAreaRazorPages> -->
                <a asp-area=""Sessions""
                   asp-page=""/Index"">View Sessions</a>
                <!-- </snippet_AspAreaRazorPages> -->
            </td>
        </tr>
        <tr>
            <td>asp-controller</td>
            <td>
                <code>
                    @Html.Raw(Html.Encode(@""<a asp-controller=""""Speaker"""" asp-action=""""Index"""">All Speakers</a>""))
                </code>
            </td>
            <td>
                <!-- <snippet_AspController> -->
                <a asp-controller=""Speaker""
                   asp-action=""Index"">All Speakers</a>
                <!-- </snippet_AspController> -->
            </td>
        </tr>
        <tr>
            <td>asp-fragment</td>
            <td>
                <code>
                    @Html.Raw(Html.Encode(@""<a asp-controller=""""Speaker"""" asp-action=""""Evaluations"""" asp-fragment=""""SpeakerEvaluations"""">Speaker Evaluations</a>""))
                </code>
            </td>
            <td>
                <!-- <snippet_AspFragment> -->
                <a asp-controller=""Speaker""
                   asp-action=""Evaluations""
                   asp-fragment=""SpeakerEvaluations"">Speaker Evaluations</a>
                <!-- </snippet_AspFragment> -->
            </td>
        </tr>
        <tr>
            <td>asp-host</td>
            <td>
                <code>
                    @Html.Raw(Html.Encode(@""<a asp-protocol=""""https"""" asp-host=""""microsoft.com"""" asp-controller=""""Home"""" asp-action=""""About"""">About</a>""))
                </code>
            </td>
            <td>
                <!-- <snippet_AspHost> -->
                <a asp-protocol=""https""
                   asp-host=""microsoft.com""
                   asp-controller=""Home""
                   asp-action=""About"">About</a>
                <!-- </snippet_AspHost> -->
            </td>
        </tr>
        <tr>
            <td>asp-page <span class=""badge"">RP</span></td>
            <td>
                <code>
                    @Html.Raw(Html.Encode(@""<a asp-page=""""/Attendee"""">All Attendees</a>""))
                </code>
            </td>
            <td>
                <!-- <snippet_AspPage> -->
                <a asp-page=""/Attendee"">All Attendees</a>
                <!-- </snippet_AspPage> -->
            </td>
        </tr>
        <tr>
            <td>asp-page-handler <span class=""badge"">RP</span></td>
            <td>
                <code>
                    @Html.Raw(Html.Encode(@""<a asp-page=""""/Attendee"""" asp-page-handler=""""Profile"""" asp-route-attendeeid=""""12"""">Attendee Profile</a>""))
                </code>
            </td>
            <td>
                <!-- <snippet_AspPageHandler> -->
                <a asp-page=""/Attendee""
                   asp-page-handler=""Profile""
                   asp-route-attendeeid=""12"">Attendee Profile</a>
                <!-- </snippet_AspPageHandler> -->
            </td>
        </tr>
        <tr>
            <td>asp-protocol</td>
            <td>
                <code>
                    @Html.Raw(Html.Encode(@""<a asp-protocol=""""https"""" asp-controller=""""Home"""" asp-action=""""About"""">About</a>""))
                </code>
            </td>
            <td>
                <!-- <snippet_AspProtocol> -->
                <a asp-protocol=""https""
                   asp-controller=""Home""
                   asp-action=""About"">About</a>
                <!-- </snippet_AspProtocol> -->
            </td>
        </tr>
        <tr>
            <td>asp-route</td>
            <td>
                <code>
                    @Html.Raw(Html.Encode(@""<a asp-route=""""speakerevals"""">Speaker Evaluations</a>""))
                </code>
            </td>
            <td>
                <!-- <snippet_AspRoute> -->
                <a asp-route=""speakerevals"">Speaker Evaluations</a>
                <!-- </snippet_AspRoute> -->
            </td>
        </tr>
        <tr>
            <td>asp-route-<em>{value}</em></td>
            <td>
                <code>
                    @Html.Raw(Html.Encode(@""<a asp-page=""""/Attendee"""" asp-route-attendeeid=""""10"""">View Attendee</a>""))
                </code>
            </td>
            <td>
                <!-- <snippet_AspPageAspRouteId> -->
                <a asp-page=""/Attendee""
                   asp-route-attendeeid=""10"">View Attendee</a>
                <!-- </snippet_AspPageAspRouteId> -->
            </td>
        </tr>
    </tbody>
<!-- </snippet_BigSnippet> -->
    <tfoot>
        <tr>
            <td colspan=""3"">
                <span class=""badge"">RP</span> Supported in Razor Pages only
            </td>
        </tr>
    </tfoot>
</table>";
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
'</snippet2>

'<snippet3>
Imports System.Reflection

Class AppDomain2
    Public Shared Sub Main()
'<snippet_Inner>
        Console.WriteLine(""Creating new AppDomain."")
        Dim domain As AppDomain = AppDomain.CreateDomain(""MyDomain"")
'</snippet_Inner>
        Console.WriteLine(""Host domain: "" + AppDomain.CurrentDomain.FriendlyName)
        Console.WriteLine(""child domain: "" + domain.FriendlyName)
    End Sub
End Class
'</snippet3>
";
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
        static public string contentCrazy = @"//<Snippet1>
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
        static public string contentSQL = @"-- <everything>
--<students>
SELECT * FROM Students
WHERE Grade = 12
AND Major = 'Math'
--</students>

--<teachers>
SELECT * FROM Teachers
WHERE Grade = 12
AND Class = 'Math'
--</teachers>
--</everything>";
        static public string contentPython = @"#<everything>
#<first>
from flask import Flask
app = Flask(__name__)
#</first>

#<second>
@app.route(""/"")
def hello():
    return ""Hello World!""
#</second>
#</everything>
";
        static public string contentBatch = @"REM <snippet>
:Label1
	:Label2
:: Comment line 3
REM </snippet>
	:: Comment line 4
IF EXIST C:\AUTOEXEC.BAT REM AUTOEXEC.BAT exists";
        static public string contentErlang = @"-module(hello_world).
-compile(export_all).

% <snippet>
hello() ->
    io:format(""hello world~n"").
% </snippet>";
        static public string contentLisp = @";<everything>
USER(64): (member 'b '(perhaps today is a good day to die)) ; test fails
NIL
;<inner>
USER(65): (member 'a '(perhaps today is a good day to die)) ; returns non-NIL
'(a good day to die)
; </inner>
;</everything>";


        [Theory]
        [InlineData(@":::code source=""source.cs"" range=""9"" language=""csharp"":::", @"<pre>
<code class=""lang-csharp"">namespace TableSnippets
</code></pre>
")]
        [InlineData(@":::code source=""source.cs"" range=""11 - 33, 40-44"" highlight=""6-7"" language=""azurecli"" interactive=""try-dotnet"":::", @"<pre>
<code class=""lang-azurecli"" data-interactive=""azurecli"" data-interactive-mode=""try-dotnet"" highlight-lines=""6-7"">/// &lt;summary&gt;
/// Interaction logic for Window1.xaml
/// &lt;/summary&gt;

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
// &lt;/Snippet_Table_Columns_Add&gt;

// Insert a new first column.
// &lt;Snippet_Table_Columns_Insert&gt;
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
           TableCell cellx = new TableCell(parx);
           // &lt;/Snippet_TableCell_Const1&gt;
       }
   }
}
</code></pre>
")]
        [InlineData(@":::code source=""source.cs"" id=""Snippet_Table_RowGroups_Add"" language=""azurecli"" interactive=""try-dotnet"":::", @"<pre>
<code class=""lang-azurecli"" data-interactive=""azurecli"" data-interactive-mode=""try-dotnet"">Table tbl = new Table();
int rowGroupsToAdd = 4;
for (int x = 0; x &lt; rowGroupsToAdd; x++)
    tbl.RowGroups.Add(new TableRowGroup());
</code></pre>
")]
        [InlineData(@":::code source=""source.vb"" id=""snippet2"" interactive=""try-dotnet"":::", @"<pre>
<code class=""lang-vb"" data-interactive=""vb"" data-interactive-mode=""try-dotnet"">Imports System.Reflection

Class AppDomain1
    Public Shared Sub Main()
        Console.WriteLine(&quot;Creating new AppDomain.&quot;)
        Dim domain As AppDomain = AppDomain.CreateDomain(&quot;MyDomain&quot;)

        Console.WriteLine(&quot;Host domain: &quot; + AppDomain.CurrentDomain.FriendlyName)
        Console.WriteLine(&quot;child domain: &quot; + domain.FriendlyName)
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
            root.SetupInformation.ApplicationBase &amp; &quot;MyAppSubfolder\&quot;

        Dim domain As AppDomain = AppDomain.CreateDomain(&quot;MyDomain&quot;, Nothing, setup)

        Console.WriteLine(&quot;Application base of {0}:&quot; &amp; vbCrLf &amp; vbTab &amp; &quot;{1}&quot;, _
            root.FriendlyName, root.SetupInformation.ApplicationBase)
        Console.WriteLine(&quot;Application base of {0}:&quot; &amp; vbCrLf &amp; vbTab &amp; &quot;{1}&quot;, _
            domain.FriendlyName, domain.SetupInformation.ApplicationBase)

        AppDomain.Unload(domain)
    End Sub
End Class

&#39; This example produces output similar to the following:
&#39;
&#39;Application base of MyApp.exe:
&#39;        C:\Program Files\MyApp\
&#39;Application base of MyDomain:
&#39;        C:\Program Files\MyApp\MyAppSubfolder\
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
        domaininfo-&gt;ApplicationBase = &quot;f:\\work\\development\\latest&quot;;

        // Create the application domain.
        AppDomain^ domain = AppDomain::CreateDomain(&quot;MyDomain&quot;, nullptr, domaininfo);

        // Write application domain information to the console.
        Console::WriteLine(&quot;Host domain: &quot; + AppDomain::CurrentDomain-&gt;FriendlyName);
        Console::WriteLine(&quot;child domain: &quot; + domain-&gt;FriendlyName);
        Console::WriteLine(&quot;Application base is: &quot; + domain-&gt;SetupInformation-&gt;ApplicationBase);

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
        domaininfo-&gt;ApplicationBase = &quot;f:\\work\\development\\latest&quot;;

        // Create the application domain.
        AppDomain^ domain = AppDomain::CreateDomain(&quot;MyDomain&quot;, nullptr, domaininfo);

        // Write application domain information to the console.
        Console::WriteLine(&quot;Host domain: &quot; + AppDomain::CurrentDomain-&gt;FriendlyName);
        Console::WriteLine(&quot;child domain: &quot; + domain-&gt;FriendlyName);
        Console::WriteLine(&quot;Application base is: &quot; + domain-&gt;SetupInformation-&gt;ApplicationBase);

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
    setup-&gt;ApplicationBase = 
        root-&gt;SetupInformation-&gt;ApplicationBase + &quot;MyAppSubfolder\\&quot;;

    AppDomain^ domain = AppDomain::CreateDomain(&quot;MyDomain&quot;, nullptr, setup);

    Console::WriteLine(&quot;Application base of {0}:\r\n\t{1}&quot;, 
        root-&gt;FriendlyName, root-&gt;SetupInformation-&gt;ApplicationBase);
    Console::WriteLine(&quot;Application base of {0}:\r\n\t{1}&quot;, 
        domain-&gt;FriendlyName, domain-&gt;SetupInformation-&gt;ApplicationBase);

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
        [InlineData(@":::code source=""source2.cs"" id=""snippet_UseMvc"":::
", @"<pre>
<code class=""lang-csharp"">app.UseMvc(routes =&gt;
{
    // need route and attribute on controller: [Area(&quot;Blogs&quot;)]
    routes.MapRoute(name: &quot;mvcAreaRoute&quot;,
                    template: &quot;{area:exists}/{controller=Home}/{action=Index}&quot;);

    // default route for non-areas
    routes.MapRoute(
        name: &quot;default&quot;,
        template: &quot;{controller=Home}/{action=Index}/{id?}&quot;);
});
</code></pre>")]
        [InlineData(@":::code source=""source2.cs"" id=""snippet_AllowAreas"":::
", @"<pre>
<code class=""lang-csharp"">services.AddMvc()
        .AddRazorPagesOptions(options =&gt; options.AllowAreas = true);
</code></pre>
")]
        [InlineData(@":::code source=""source.vb"" id=""snippet3"":::
", @"<pre>
<code class=""lang-vb"">Imports System.Reflection

Class AppDomain2
    Public Shared Sub Main()
        Console.WriteLine(&quot;Creating new AppDomain.&quot;)
        Dim domain As AppDomain = AppDomain.CreateDomain(&quot;MyDomain&quot;)
        Console.WriteLine(&quot;Host domain: &quot; + AppDomain.CurrentDomain.FriendlyName)
        Console.WriteLine(&quot;child domain: &quot; + domain.FriendlyName)
    End Sub
End Class
</code></pre>
")]
        [InlineData(@":::code source=""asp.cshtml"" id=""snippet_BigSnippet"":::
", @"<pre>
<code class=""lang-cshtml"">&lt;tbody&gt;
   &lt;tr&gt;
       &lt;td&gt;asp-action&lt;/td&gt;
       &lt;td&gt;
           &lt;code&gt;
               @Html.Raw(Html.Encode(@&quot;&lt;a asp-controller=&quot;&quot;Speaker&quot;&quot; asp-action=&quot;&quot;Evaluations&quot;&quot;&gt;Speaker Evaluations&lt;/a&gt;&quot;))
           &lt;/code&gt;
       &lt;/td&gt;
       &lt;td&gt;
           &lt;a asp-controller=&quot;Speaker&quot;
              asp-action=&quot;Evaluations&quot;&gt;Speaker Evaluations&lt;/a&gt;
       &lt;/td&gt;
   &lt;/tr&gt;
   &lt;tr&gt;
       &lt;td&gt;asp-all-route-data&lt;/td&gt;
       &lt;td&gt;
           &lt;code&gt;
               @Html.Raw(Html.Encode(@&quot;&lt;a asp-route=&quot;&quot;speakerevalscurrent&quot;&quot; asp-all-route-data=&quot;&quot;parms&quot;&quot;&gt;Speaker Evaluations&lt;/a&gt;&quot;))
           &lt;/code&gt;
       &lt;/td&gt;
       &lt;td&gt;
           @{
           var parms = new Dictionary&lt;string, string&gt;
                       {
                           { &quot;speakerId&quot;, &quot;11&quot; },
                           { &quot;currentYear&quot;, &quot;true&quot; }
                       };
           }

           &lt;a asp-route=&quot;speakerevalscurrent&quot;
              asp-all-route-data=&quot;parms&quot;&gt;Speaker Evaluations&lt;/a&gt;
       &lt;/td&gt;
   &lt;/tr&gt;
   &lt;tr&gt;
       &lt;td rowspan=&quot;2&quot;&gt;asp-area&lt;/td&gt;
       &lt;td&gt;
           &lt;code&gt;
               @Html.Raw(Html.Encode(@&quot;&lt;a asp-area=&quot;&quot;Blogs&quot;&quot; asp-controller=&quot;&quot;Home&quot;&quot; asp-action=&quot;&quot;AboutBlog&quot;&quot;&gt;About Blog&lt;/a&gt;&quot;))
           &lt;/code&gt;
       &lt;/td&gt;
       &lt;td&gt;
           &lt;a asp-area=&quot;Blogs&quot;
              asp-controller=&quot;Home&quot;
              asp-action=&quot;AboutBlog&quot;&gt;About Blog&lt;/a&gt;
       &lt;/td&gt;
   &lt;/tr&gt;
   &lt;tr&gt;
       &lt;td&gt;
           &lt;code&gt;
               @Html.Raw(Html.Encode(@&quot;&lt;a asp-area=&quot;&quot;Sessions&quot;&quot; asp-page=&quot;&quot;/Index&quot;&quot;&gt;View Sessions&lt;/a&gt;&quot;))
           &lt;/code&gt;
       &lt;/td&gt;
       &lt;td&gt;
           &lt;a asp-area=&quot;Sessions&quot;
              asp-page=&quot;/Index&quot;&gt;View Sessions&lt;/a&gt;
       &lt;/td&gt;
   &lt;/tr&gt;
   &lt;tr&gt;
       &lt;td&gt;asp-controller&lt;/td&gt;
       &lt;td&gt;
           &lt;code&gt;
               @Html.Raw(Html.Encode(@&quot;&lt;a asp-controller=&quot;&quot;Speaker&quot;&quot; asp-action=&quot;&quot;Index&quot;&quot;&gt;All Speakers&lt;/a&gt;&quot;))
           &lt;/code&gt;
       &lt;/td&gt;
       &lt;td&gt;
           &lt;a asp-controller=&quot;Speaker&quot;
              asp-action=&quot;Index&quot;&gt;All Speakers&lt;/a&gt;
       &lt;/td&gt;
   &lt;/tr&gt;
   &lt;tr&gt;
       &lt;td&gt;asp-fragment&lt;/td&gt;
       &lt;td&gt;
           &lt;code&gt;
               @Html.Raw(Html.Encode(@&quot;&lt;a asp-controller=&quot;&quot;Speaker&quot;&quot; asp-action=&quot;&quot;Evaluations&quot;&quot; asp-fragment=&quot;&quot;SpeakerEvaluations&quot;&quot;&gt;Speaker Evaluations&lt;/a&gt;&quot;))
           &lt;/code&gt;
       &lt;/td&gt;
       &lt;td&gt;
           &lt;a asp-controller=&quot;Speaker&quot;
              asp-action=&quot;Evaluations&quot;
              asp-fragment=&quot;SpeakerEvaluations&quot;&gt;Speaker Evaluations&lt;/a&gt;
       &lt;/td&gt;
   &lt;/tr&gt;
   &lt;tr&gt;
       &lt;td&gt;asp-host&lt;/td&gt;
       &lt;td&gt;
           &lt;code&gt;
               @Html.Raw(Html.Encode(@&quot;&lt;a asp-protocol=&quot;&quot;https&quot;&quot; asp-host=&quot;&quot;microsoft.com&quot;&quot; asp-controller=&quot;&quot;Home&quot;&quot; asp-action=&quot;&quot;About&quot;&quot;&gt;About&lt;/a&gt;&quot;))
           &lt;/code&gt;
       &lt;/td&gt;
       &lt;td&gt;
           &lt;a asp-protocol=&quot;https&quot;
              asp-host=&quot;microsoft.com&quot;
              asp-controller=&quot;Home&quot;
              asp-action=&quot;About&quot;&gt;About&lt;/a&gt;
       &lt;/td&gt;
   &lt;/tr&gt;
   &lt;tr&gt;
       &lt;td&gt;asp-page &lt;span class=&quot;badge&quot;&gt;RP&lt;/span&gt;&lt;/td&gt;
       &lt;td&gt;
           &lt;code&gt;
               @Html.Raw(Html.Encode(@&quot;&lt;a asp-page=&quot;&quot;/Attendee&quot;&quot;&gt;All Attendees&lt;/a&gt;&quot;))
           &lt;/code&gt;
       &lt;/td&gt;
       &lt;td&gt;
           &lt;a asp-page=&quot;/Attendee&quot;&gt;All Attendees&lt;/a&gt;
       &lt;/td&gt;
   &lt;/tr&gt;
   &lt;tr&gt;
       &lt;td&gt;asp-page-handler &lt;span class=&quot;badge&quot;&gt;RP&lt;/span&gt;&lt;/td&gt;
       &lt;td&gt;
           &lt;code&gt;
               @Html.Raw(Html.Encode(@&quot;&lt;a asp-page=&quot;&quot;/Attendee&quot;&quot; asp-page-handler=&quot;&quot;Profile&quot;&quot; asp-route-attendeeid=&quot;&quot;12&quot;&quot;&gt;Attendee Profile&lt;/a&gt;&quot;))
           &lt;/code&gt;
       &lt;/td&gt;
       &lt;td&gt;
           &lt;a asp-page=&quot;/Attendee&quot;
              asp-page-handler=&quot;Profile&quot;
              asp-route-attendeeid=&quot;12&quot;&gt;Attendee Profile&lt;/a&gt;
       &lt;/td&gt;
   &lt;/tr&gt;
   &lt;tr&gt;
       &lt;td&gt;asp-protocol&lt;/td&gt;
       &lt;td&gt;
           &lt;code&gt;
               @Html.Raw(Html.Encode(@&quot;&lt;a asp-protocol=&quot;&quot;https&quot;&quot; asp-controller=&quot;&quot;Home&quot;&quot; asp-action=&quot;&quot;About&quot;&quot;&gt;About&lt;/a&gt;&quot;))
           &lt;/code&gt;
       &lt;/td&gt;
       &lt;td&gt;
           &lt;a asp-protocol=&quot;https&quot;
              asp-controller=&quot;Home&quot;
              asp-action=&quot;About&quot;&gt;About&lt;/a&gt;
       &lt;/td&gt;
   &lt;/tr&gt;
   &lt;tr&gt;
       &lt;td&gt;asp-route&lt;/td&gt;
       &lt;td&gt;
           &lt;code&gt;
               @Html.Raw(Html.Encode(@&quot;&lt;a asp-route=&quot;&quot;speakerevals&quot;&quot;&gt;Speaker Evaluations&lt;/a&gt;&quot;))
           &lt;/code&gt;
       &lt;/td&gt;
       &lt;td&gt;
           &lt;a asp-route=&quot;speakerevals&quot;&gt;Speaker Evaluations&lt;/a&gt;
       &lt;/td&gt;
   &lt;/tr&gt;
   &lt;tr&gt;
       &lt;td&gt;asp-route-&lt;em&gt;{value}&lt;/em&gt;&lt;/td&gt;
       &lt;td&gt;
           &lt;code&gt;
               @Html.Raw(Html.Encode(@&quot;&lt;a asp-page=&quot;&quot;/Attendee&quot;&quot; asp-route-attendeeid=&quot;&quot;10&quot;&quot;&gt;View Attendee&lt;/a&gt;&quot;))
           &lt;/code&gt;
       &lt;/td&gt;
       &lt;td&gt;
           &lt;a asp-page=&quot;/Attendee&quot;
              asp-route-attendeeid=&quot;10&quot;&gt;View Attendee&lt;/a&gt;
       &lt;/td&gt;
   &lt;/tr&gt;
&lt;/tbody&gt;
</code></pre>
")]
        [InlineData(@":::code source=""source.sql"" id=""teachers"":::
", @"<pre>
<code class=""lang-sql"">SELECT * FROM Teachers
WHERE Grade = 12
AND Class = &#39;Math&#39;
</code></pre>
")]
        [InlineData(@":::code source=""source.sql"" id=""everything"":::
", @"<pre>
<code class=""lang-sql"">SELECT * FROM Students
WHERE Grade = 12
AND Major = &#39;Math&#39;

SELECT * FROM Teachers
WHERE Grade = 12
AND Class = &#39;Math&#39;
</code></pre>
")]
        [InlineData(@":::code source=""source.py"" id=""everything"":::
", @"<pre>
<code class=""lang-python"">from flask import Flask
app = Flask(__name__)

@app.route(&quot;/&quot;)
def hello():
    return &quot;Hello World!&quot;
</code></pre>
")]
        [InlineData(@":::code source=""source.bat"" id=""snippet"":::
", @"<pre>
<code class=""lang-batchfile"">:Label1
:Label2
:: Comment line 3
</code></pre>
")]
        [InlineData(@":::code source=""source.erl"" id=""snippet"":::
", @"<pre>
<code class=""lang-erlang"">hello() -&gt;
    io:format(&quot;hello world~n&quot;).
</code></pre>
")]
        [InlineData(@":::code source=""source.lsp"" id=""everything"":::
", @"<pre>
<code class=""lang-lisp"">USER(64): (member &#39;b &#39;(perhaps today is a good day to die)) ; test fails
NIL
USER(65): (member &#39;a &#39;(perhaps today is a good day to die)) ; returns non-NIL
&#39;(a good day to die)
</code></pre>
")]
        public void CodeTestBlockGeneral(string source, string expected)
        {
            var filename = string.Empty;
            var content = string.Empty;
            // arrange
            if (source.Contains("source.cs"))
            {
                filename = "source.cs";
                content = contentCSharp;
            }
            else if (source.Contains("source2.cs"))
            {
                filename = "source2.cs";
                content = contentCharpRegion;
            }
            else if (source.Contains("asp.cshtml"))
            {
                filename = "asp.cshtml";
                content = contentASPNet;
            }
            else if (source.Contains("source.vb"))
            {
                filename = "source.vb";
                content = contentVB;
            }
            else if (source.Contains("source.cpp"))
            {
                filename = "source.cpp";
                content = contentCPP;
            }
            else if (source.Contains("source.sql"))
            {
                filename = "source.sql";
                content = contentSQL;
            }
            else if (source.Contains("source.py"))
            {
                filename = "source.py";
                content = contentPython;
            }
            else if (source.Contains("source.bat"))
            {
                filename = "source.bat";
                content = contentBatch;
            }
            else if (source.Contains("source.erl"))
            {
                filename = "source.erl";
                content = contentErlang;
            }
            else if (source.Contains("source.lsp"))
            {
                filename = "source.lsp";
                content = contentLisp;
            }

            // act

            // assert
            TestUtility.VerifyMarkup(source, expected, files: new Dictionary<string, string>
            {
                { filename, content }
            });
            
        }

        [Theory]
        [InlineData(@":::code source=""source.cs"" range=""205-250"" language=""azurecli"" interactive=""try-dotnet"":::")]
        [InlineData(@":::code source=""source.cs"" badattribute=""ham"" range=""1-5"" language=""azurecli"" interactive=""try-dotnet"":::")]
        [InlineData(@":::code source=""source.cs"" id=""id"" range=""1-5"" language=""azurecli"" interactive=""try-dotnet"":::")]
        [InlineData(@":::code range=""1-5"" language=""azurecli"" interactive=""try-dotnet"":::")]
        [InlineData(@":::code source=""source.cs"" range=""abc-def"" language=""azurecli"" interactive=""try-dotnet"":::")]
        [InlineData(@":::code source=""source.crazy"" range=""1-3"" interactive=""try-dotnet"":::")]
        public void CodeTestBlockGeneralCSharp_Error(string source)
        {
            // arrange
            var filename = string.Empty;
            var content = string.Empty;
            if (source.Contains("source.cs"))
            {
                filename = "source.cs";
                content = contentCSharp;
            }
            else if (source.Contains("source.vb"))
            {
                filename = "source.vb";
                content = contentVB;
            }
            else if (source.Contains("source.cpp"))
            {
                filename = "source.cpp";
                content = contentCPP;
            }
            else if (source.Contains("source.crazy"))
            {
                filename = "source.crazy";
                content = contentCrazy;
            }

            // act

            // assert
            TestUtility.VerifyMarkup(source, null, errors:new string[] { "invalid-code" }, files: new Dictionary<string, string>
            {
                { filename, content }
            });

        }

    }
}
