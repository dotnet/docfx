// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownAzureRewritersTest.Tests
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.AzureMarkdownRewriters;

    using Xunit;

    public class AzureMarkdownRewritersTest
    {
        #region Azure marked

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_Simple()
        {
            var source = @"Hello world";
            var expected = @"Hello world

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_NormalBlockquote()
        {
            var source = @"> Hello world
this is new line originally  
> This is a new line";
            var expected = @"> Hello world
> this is new line originally  
> This is a new line

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_NormalBlockquoteNest()
        {
            var source = @"> Hello world
this is new line originally  
> > This is a second nested first line
> > This is a second nested seconde line
> > > This is a third nested first line
> > This is a second nested second line
This is no-nested line
";
            var expected = @"> Hello world
> this is new line originally  
> 
> > This is a second nested first line
> > This is a second nested seconde line
> > 
> > > This is a third nested first line
> > > This is a second nested second line
> > > This is no-nested line

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureNoteBlockquote()
        {
            var source = @"> [AZURE.NOTE]
This is azure note
> [AZURE.WARNING]
This is azure warning";
            var expected = @"> [!NOTE]
> This is azure note
> 
> [!WARNING]
> This is azure warning

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureNoteShouldParseFollowedText()
        {
            var source = @"> [AZURE.NOTE]
> This is a link [hello text]      (hello.md)
> This is a style text _yes_";
            var expected = @"> [!NOTE]
> This is a link [hello text](hello.md)
> This is a style text *yes*

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureNoteWithExtraWhiteSpaces()
        {
            var source = @"> [AZURE.NOTE]
>       This is azure note text
>       Not code text
>       We should ignore the extra white spaces at the beginning";
            var expected = @"> [!NOTE]
> This is azure note text
> Not code text
> We should ignore the extra white spaces at the beginning

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureNoteWithExtraWhiteSpacesNoLt()
        {
            var source = @"> [AZURE.NOTE]
      This information applies to the Azure AD B2C consumer identity service preview.  For information on Azure AD for employees and organizations, 
         please refer to the [Azure Active Directory Developer Guide](active-directory-developers-guide.md).";
            var expected = @"> [!NOTE]
> This information applies to the Azure AD B2C consumer identity service preview.  For information on Azure AD for employees and organizations, 
> please refer to the [Azure Active Directory Developer Guide](active-directory-developers-guide.md).

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureNoteBlockquoteTextFollowed()
        {
            var source = @"> [AZURE.NOTE]
This is azure note
> [AZURE.WARNING] This is azure warning
> [AZURE.IMPORTANT] This is azure important
> [AZURE.TIP]
This is azure TIP";
            var expected = @"> [!NOTE]
> This is azure note
> 
> [!WARNING]
> This is azure warning
> 
> [!IMPORTANT]
> This is azure important
> 
> [!TIP]
> This is azure TIP

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureNoteBlockquoteNest()
        {
            var source = @"> [AZURE.NOTE]
This is azure note
> > [AZURE.WARNING]
This is azure warning
> > [AZURE.IMPORTANT]
> > This is azure Important
> > > [AZURE.TIP]
This is TIP
> > [AZURE.CAUTION]
> This is CAUTION";
            var expected = @"> [!NOTE]
> This is azure note
> 
> > [!WARNING]
> > This is azure warning
> > 
> > [!IMPORTANT]
> > This is azure Important
> > 
> > > [!TIP]
> > > This is TIP
> > > 
> > > [!CAUTION]
> > > This is CAUTION

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact(Skip = "Disable as Include logic change")]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureInclude()
        {
            var source = @"This is azure include [AZURE.INCLUDE [include-short-name](../includes/include-file-name.md)] inline.

This is azure include block.

[AZURE.INCLUDE [include-short-name](../includes/include-file-name.md ""option title"")]";
            var expected = @"This is azure include [!INCLUDE [include-short-name](../includes/include-file-name.md)] inline.

This is azure include block.

[!INCLUDE [include-short-name](../includes/include-file-name.md ""option title"")]
";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureInclude_New()
        {
            // Prepare data
            var root = @"This is azure include [AZURE.INCLUDE [ref1 text](ref1.md)] inline.

This is azure include block.

[AZURE.INCLUDE [ref2 text](ref2.md)]";
            var ref1 = @"ref1 content";
            var ref2 = @"ref2 content: [text](./this/fake.md)";
            File.WriteAllText("root.md", root);
            File.WriteAllText("ref1.md", ref1);
            File.WriteAllText("ref2.md", ref2);

            // Expected result
            var expected = @"This is azure include ref1 content inline.

This is azure include block.

ref2 content: [text](./this/fake.md)

";

            var result = AzureMarked.Markup(root, "root.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureIncludeWithAzureNote()
        {
            // Prepare data
            var root = @"This is azure include [AZURE.INCLUDE [ref1 text](ref1.md)] inline.

This is azure include block.

[AZURE.INCLUDE [ref2 text](ref2.md)]";
            var ref1 = @"ref1 content";
            var ref2 = @"> [AZURE.NOTE]
This is azure note
> [AZURE.WARNING]
This is azure warning";
            File.WriteAllText("root.md", root);
            File.WriteAllText("ref1.md", ref1);
            File.WriteAllText("ref2.md", ref2);

            // Expected result
            var expected = @"This is azure include ref1 content inline.

This is azure include block.

> [!NOTE]
> This is azure note
> 
> [!WARNING]
> This is azure warning

";

            var result = AzureMarked.Markup(root, "root.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        [Trait("Bug 551844", "Rewriter issue")]
        public void TestAzureMarkdownRewriters_ListRewriterBug()
        {
            var source = @"5. Open a command prompt with administrative permissions, and run the following command:

            gpupdate /force
6. Restart the computer.
";
            var expected = @"1. Open a command prompt with administrative permissions, and run the following command:
   
            gpupdate /force
2. Restart the computer.

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_ListWithInlineToken()
        {
            var source = @"Hello world
* list _system_
  this should be same line with the above one
  
  this should be another line
* list item2
- list item3
- list item4

---

1. nolist item1
2. nolist item2";
            var expected = @"Hello world

* list *system*
  this should be same line with the above one
  
  this should be another line
* list item2
* list item3
* list item4

- - -
1. nolist item1
2. nolist item2

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_Heading()
        {
            var source = @"#h1 title
h1 text
##h2 title-1
h2-1 text
###h3 title
h3 text
##h2 title-2
h2-2 text";
            var expected = @"# h1 title
h1 text

## h2 title-1
h2-1 text

### h3 title
h3 text

## h2 title-2
h2-2 text

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_HeadingWithInlineToken()
        {
            var source = @"#h1 title _system_
h1 text
##h2 title-1
h2-1 text
###h3 title ** system **
h3 text
##h2 title-2
h2-2 text";
            var expected = @"# h1 title *system*
h1 text

## h2 title-1
h2-1 text

### h3 title ** system **
h3 text

## h2 title-2
h2-2 text

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_HrInContent()
        {
            var source = @"This is an H1
========
This is an H2
-------------
hr1
- - -
hr2
* * *
hr3
***
hr4
*****
this is an h2
---------------------------------------
hr5

---------------------------------------
world.";
            var expected = @"# This is an H1
## This is an H2
hr1

- - -
hr2

- - -
hr3

- - -
hr4

- - -
## this is an h2
hr5

- - -
world.

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_HtmlTagWithSimpleContent()
        {
            var source = @"# This is an H1
<div>
This is text inside html tag
</div>";
            var expected = @"# This is an H1
<div>
This is text inside html tag
</div>";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_HtmlTagWithNotAffectiveBlockTokenContent()
        {
            var source = @"# This is an H1
<div>
- list item1
- list item2
- list item3
</div>";
            var expected = @"# This is an H1
<div>
- list item1
- list item2
- list item3
</div>";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_HtmlTagWithAffectiveInlineTokenContent()
        {
            var source = @"# This is an H1
<div>
_system_
</div>";
            var expected = @"# This is an H1
<div>
*system*
</div>";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_SimpleStrongEmDel()
        {
            var source = @"# Test Simple **Strong** *Em* ~~Del~~
This is __Strong__
This is _Em_
This is ~~Del~~";
            var expected = @"# Test Simple **Strong** *Em* ~~Del~~
This is **Strong**
This is *Em*
This is ~~Del~~

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_ComplexStrongEmDel()
        {
            var source = @"# Test Complex String Em Del
__Strong Text__
<div>
_Em Text_
<div>
- ~~Del Text~~
- Simple text";
            var expected = @"# Test Complex String Em Del
**Strong Text**
<div>
*Em Text*
<div>

* ~~Del Text~~
* Simple text

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_ComplexStrongEmDel2()
        {
            var source = @"# Test Complex String Em Del
__Strong Text__
<div>
_Em Text_
</div>
- ~~Del Text~~
- Simple text";
            var expected = @"# Test Complex String Em Del
**Strong Text**
<div>
*Em Text*
</div>

* ~~Del Text~~
* Simple text

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_Table()
        {
            var source = @"# Test table
| header-1 | header-2 | header-3 |
|:-------- |:--------:| --------:|
| *1-1* | __1-2__ | ~~1-3~~ |
| 2-1:| 2-2 | 2-3 |

header-1 | header-2 | header-3
-------- |--------:|:--------
*1-1* | __1-2__ | ~~1-3~~
2-1: | 2-2 | 2-3";
            var expected = @"# Test table
| header-1 | header-2 | header-3 |
|:--- |:---:| ---:|
| *1-1* |**1-2** |~~1-3~~ |
| 2-1: |2-2 |2-3 |

| header-1 | header-2 | header-3 |
| --- | ---:|:--- |
| *1-1* |**1-2** |~~1-3~~ |
| 2-1: |2-2 |2-3 |

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AppendMissingMarkdownLinkExtension()
        {
            var source = @"#Test Markdownlink extension
this is a missing extension link [text](missing_extension) file ref
this is a normal link [text](missing_extension.md) file ref
this is a missing extension link with bookmark [text](missing_extension#bookmark) file ref
this is a normal link with bookmark [text](missing_extension.md#bookmark) file ref
this is a missing extension link with / at the end [text](missing_extension/) file ref
this is a missing extension link with / and bookmark at the end [text](missing_extension/#bookmark) file ref
this is a normal link with / and extension at the end [text](normal.md/) file ref
this is a normal link with /, extension and bookmark at the end [text](normal.md/#bookmark) file ref
this is http link [text](http://www.google.com ""Google"") ref
this is http escape link [text](http://www.google.com'dd#bookmark ""Google's homepage"") ref
this is absolute link [text](c:/this/is/markdown ""Local File"") file ref";
            var expected = @"# Test Markdownlink extension
this is a missing extension link [text](missing_extension.md) file ref
this is a normal link [text](missing_extension.md) file ref
this is a missing extension link with bookmark [text](missing_extension.md#bookmark) file ref
this is a normal link with bookmark [text](missing_extension.md#bookmark) file ref
this is a missing extension link with / at the end [text](missing_extension.md) file ref
this is a missing extension link with / and bookmark at the end [text](missing_extension.md#bookmark) file ref
this is a normal link with / and extension at the end [text](normal.md) file ref
this is a normal link with /, extension and bookmark at the end [text](normal.md#bookmark) file ref
this is http link [text](http://www.google.com ""Google"") ref
this is http escape link [text](http://www.google.com\'dd#bookmark ""Google's homepage"") ref
this is absolute link [text](c:/this/is/markdown ""Local File"") file ref

";

            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_FixResourceFileNotInCurrentDocset()
        {
            // Prepare data
            // Create a docset1 markdown file. Ref docset2's markdown, image and resource
            var docset1Md = @"[Ref a md content in another docset](../docset2/docset2Md.md)
[Ref a non md resource in another docset](../docset2/docset2Resource.html)
![Ref a image content in another docset](../docset2/docset2Image.png)
![Ref a image content not in another docset](../docset2/docset2FakeImage.png)
![Ref a abs path image content in another docset](c:\\docset2\fullNameImage.img)
![Ref a abs http image content in another docset](https://google/images/fullNameImage.img)";
            var docset1Dir = Directory.CreateDirectory("docset1");
            var docset1MdFilePath = Path.Combine(docset1Dir.FullName, "docset1Md.md");
            File.WriteAllText(docset1MdFilePath, docset1Md);

            // Create docset2's files, one markdown, one resource, one image
            var docset2Dir = Directory.CreateDirectory("docset2");
            var docset2Md = @"docset2 content";
            var docset2MdFileName = "docset2Md.md";
            File.WriteAllText(Path.Combine(docset2Dir.FullName, docset2MdFileName), docset2Md);
            var docset2Resource = @"<p>docset2 resource</p>";
            var docset2ResourceFileName = "docset2Resource.html";
            var docset2ResourceFilePath = Path.Combine(docset2Dir.FullName, docset2ResourceFileName);
            File.WriteAllText(docset2ResourceFilePath, docset2Resource);
            var docset2Image = @"image";
            var docset2ImageFileName = "docset2Image.png";
            var docset2ImageFilePath = Path.Combine(docset2Dir.FullName, docset2ImageFileName);
            File.WriteAllText(docset2ImageFilePath, docset2Image);

            // Expected result
            var expected = @"[Ref a md content in another docset](../docset2/docset2Md.md)
[Ref a non md resource in another docset](ex_resource/docset2Resource.html)
![Ref a image content in another docset](ex_resource/docset2Image.png)
![Ref a image content not in another docset](../docset2/docset2FakeImage.png)
![Ref a abs path image content in another docset](c:\\docset2\\fullNameImage.img)
![Ref a abs http image content in another docset](https://google/images/fullNameImage.img)

";

            Dictionary<string, AzureFileInfo> azureResourceFileInfoMapping = new Dictionary<string, AzureFileInfo>
            {
                [docset2ResourceFileName] = new AzureFileInfo { FileName = docset2ResourceFileName, FilePath = docset2ResourceFilePath },
                [docset2ImageFileName] = new AzureFileInfo { FileName = docset2ImageFileName, FilePath = docset2ImageFilePath }
            };


            var result = AzureMarked.Markup(docset1Md, docset1MdFilePath, null, null, azureResourceFileInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
            var externalResourceFolderName = "ex_resource";
            Assert.True(!File.Exists(Path.Combine(docset1Dir.FullName, externalResourceFolderName, docset2MdFileName)));
            Assert.True(File.Exists(Path.Combine(docset1Dir.FullName, externalResourceFolderName, docset2ResourceFileName)));
            Assert.True(File.Exists(Path.Combine(docset1Dir.FullName, externalResourceFolderName, docset2ImageFileName)));
        }

        [Fact(Skip = "Disable as Include logic change")]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_TransformMultiAzureInclude()
        {
            var source = @"[AZURE.INCLUDE [azure-probe-intro-include](../../includes/application-gateway-create-probe-intro-include.md)].

[AZURE.INCLUDE [azure-arm-classic-important-include](../../includes/learn-about-deployment-models-classic-include.md)] [Resource Manager model](application-gateway-create-probe-ps.md).


[AZURE.INCLUDE [azure-ps-prerequisites-include.md](../../includes/azure-ps-prerequisites-include.md)]
";
            var expected = @"[!INCLUDE [azure-probe-intro-include](../../includes/application-gateway-create-probe-intro-include.md)].

[!INCLUDE [azure-arm-classic-important-include](../../includes/learn-about-deployment-models-classic-include.md)] [Resource Manager model](application-gateway-create-probe-ps.md).

[!INCLUDE [azure-ps-prerequisites-include.md](../../includes/azure-ps-prerequisites-include.md)]
";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AutoLink()
        {
            var source = @" See [http://www.openldap.org/doc/admin24/overlays.html#Access%20Logging](http://www.openldap.org/doc/admin24/overlays.html#Access%20Logging)";
            var expected = @" See [http://www.openldap.org/doc/admin24/overlays.html#Access%20Logging](http://www.openldap.org/doc/admin24/overlays.html#Access%20Logging)

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_LinkRefWithBracket()
        {
            var source = @"[User-Defined Date/Time Formats (Format Function)](http://msdn2.microsoft.com/library/73ctwf33\(VS.90\).aspx)";
            var expected = @"[User-Defined Date/Time Formats (Format Function)](http://msdn2.microsoft.com/library/73ctwf33\(VS.90\).aspx)

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_LinkRefWithBackslash()
        {
            var source = @"[User-Defined Date/Time Formats (Format Function)](https://github\.com/Azure-Samples/active-directory-java-webapp\-openidconnect\\/archive/complete\.zip)";
            var expected = @"[User-Defined Date/Time Formats (Format Function)](https://github.com/Azure-Samples/active-directory-java-webapp-openidconnect\\/archive/complete.zip)

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureSingleSelector()
        {
            var source = @"> [AZURE.SELECTOR]
- [Universal Windows](../articles/notification-hubs-windows-store-dotnet-get-started/)
- [Windows Phone](../articles/notification-hubs-windows-phone-get-started/)
- [iOS](../articles/notification-hubs-ios-get-started/)
- [Android](../articles/notification-hubs-android-get-started/)
- [Kindle](../articles/notification-hubs-kindle-get-started/)
- [Baidu](../articles/notification-hubs-baidu-get-started/)
- [Xamarin.iOS](../articles/partner-xamarin-notification-hubs-ios-get-started/)
- [Xamarin.Android](../articles/partner-xamarin-notification-hubs-android-get-started/)";
            var expected = @"> [!div class=""op_single_selector""]
> * [Universal Windows](../articles/notification-hubs-windows-store-dotnet-get-started.md)
> * [Windows Phone](../articles/notification-hubs-windows-phone-get-started.md)
> * [iOS](../articles/notification-hubs-ios-get-started.md)
> * [Android](../articles/notification-hubs-android-get-started.md)
> * [Kindle](../articles/notification-hubs-kindle-get-started.md)
> * [Baidu](../articles/notification-hubs-baidu-get-started.md)
> * [Xamarin.iOS](../articles/partner-xamarin-notification-hubs-ios-get-started.md)
> * [Xamarin.Android](../articles/partner-xamarin-notification-hubs-android-get-started.md)

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureMultiSelectors()
        {
            var source = @"> [AZURE.SELECTOR-LIST (Platform | Backend )]
- [(iOS | .NET)](./mobile-services-dotnet-backend-ios-get-started-push.md)
- [(iOS | JavaScript)](./mobile-services-javascript-backend-ios-get-started-push.md)
- [(Windows universal C# | .NET)](./mobile-services-dotnet-backend-windows-universal-dotnet-get-started-push.md)
- [(Windows universal C# | Javascript)](./mobile-services-javascript-backend-windows-universal-dotnet-get-started-push.md)
- [(Windows Phone | .NET)](./mobile-services-dotnet-backend-windows-phone-get-started-push.md)
- [(Windows Phone | Javascript)](./mobile-services-javascript-backend-windows-phone-get-started-push.md)
- [(Android | .NET)](./mobile-services-dotnet-backend-android-get-started-push.md)
- [(Android | Javascript)](./mobile-services-javascript-backend-android-get-started-push.md)
- [(Xamarin iOS | Javascript)](./partner-xamarin-mobile-services-ios-get-started-push.md)
- [(Xamarin Android | Javascript)](./partner-xamarin-mobile-services-android-get-started-push.md)";
            var expected = @"> [!div class=""op_multi_selector"" title1=""Platform"" title2=""Backend""]
> * [(iOS | .NET)](./mobile-services-dotnet-backend-ios-get-started-push.md)
> * [(iOS | JavaScript)](./mobile-services-javascript-backend-ios-get-started-push.md)
> * [(Windows universal C# | .NET)](./mobile-services-dotnet-backend-windows-universal-dotnet-get-started-push.md)
> * [(Windows universal C# | Javascript)](./mobile-services-javascript-backend-windows-universal-dotnet-get-started-push.md)
> * [(Windows Phone | .NET)](./mobile-services-dotnet-backend-windows-phone-get-started-push.md)
> * [(Windows Phone | Javascript)](./mobile-services-javascript-backend-windows-phone-get-started-push.md)
> * [(Android | .NET)](./mobile-services-dotnet-backend-android-get-started-push.md)
> * [(Android | Javascript)](./mobile-services-javascript-backend-android-get-started-push.md)
> * [(Xamarin iOS | Javascript)](./partner-xamarin-mobile-services-ios-get-started-push.md)
> * [(Xamarin Android | Javascript)](./partner-xamarin-mobile-services-android-get-started-push.md)

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }


        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_MetadataTransform()
        {
            var source = @"---
foo: ""bar""
baz: 
  - ""qux""
  - ""quxx""
corge: null
grault: 1
garply: true
waldo: ""false""
fred: ""undefined""
emptyArray: []
emptyObject: {}
emptyString: """"
---";
            var expected = @"---
foo: ""bar""
baz: 
  - ""qux""
  - ""quxx""
corge: null
grault: 1
garply: true
waldo: ""false""
fred: ""undefined""
emptyArray: []
emptyObject: {}
emptyString: """"
---
";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureProperties()
        {
            var source = @"<properties
   pageTitle=""Azure Container Service Introduction | Microsoft Azure""
   description=""Azure Container Service (ACS) provides a way to simplify the creation, configuration, and management of a cluster of virtual machines that are preconfigured to run containerized applications.""
   services=""virtual-machines""
   documentationCenter=""""
   authors=""rgardler; fenxu""
   manager=""nepeters""
   editor=""""
   tags=""acs, azure-container-service""
   keywords=""Docker, Containers, Micro-services, Mesos, Azure""/>

<tags
   ms.service=""virtual-machines""
   ms.devlang=""na""
   ms.topic=""home-page""
   ms.tgt_pltfrm=""na""
   ms.workload=""na""
   ms.date=""12/02/2015""
   ms.author=""rogardle""/>

# Azure Container Service Introduction
";
            var expected = @"---
title: Azure Container Service Introduction | Microsoft Azure
description: Azure Container Service (ACS) provides a way to simplify the creation, configuration, and management of a cluster of virtual machines that are preconfigured to run containerized applications.
services: virtual-machines
documentationcenter: ''
author: rgardler
manager: nepeters
editor: ''
tags: acs, azure-container-service
keywords: Docker, Containers, Micro-services, Mesos, Azure
ms.assetid: https://azure.microsoft.com/en-us/documentation/articles/azure_file

ms.service: virtual-machines
ms.devlang: na
ms.topic: home-page
ms.tgt_pltfrm: na
ms.workload: na
ms.date: 12/02/2015
ms.author: rogardle

---
# Azure Container Service Introduction
";
            var result = AzureMarked.Markup(source, "azure_file.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureUniqueNameMarkdownRelativeLinkInSameDocset()
        {
            var azureMarkdownFileInfoMapping =
                new Dictionary<string, AzureFileInfo>{
                    {
                        "unique.md",
                        new AzureFileInfo
                        {
                            FileName = "unique.md",
                            FilePath = @"c:\root\parent\folder2\subfolder1\unique.md",
                            NeedTransformToAzureExternalLink = false,
                            UriPrefix = "https://azure.microsoft.com/en-us/documentation/articles"
                        }
                    }
                };
            var sourceFilePath = @"c:\root\parent\folder2\subfolder1\source.md";
            var source = @"[azure file link](unique.md)";
            var expected = @"[azure file link](unique.md)

";

            var result = AzureMarked.Markup(source, sourceFilePath, azureMarkdownFileInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        [Trait("Bug 617364", "link with query condition")]
        public void TestAzureMarkdownRewriters_AbsoluteLinkWithQueryCondition()
        {
            var source = @"[Microsoft Azure Active Directory Samples and Documentation](https://github.com/Azure-Samples?page=3&query=active-directory)";
            var expected = @"[Microsoft Azure Active Directory Samples and Documentation](https://github.com/Azure-Samples?page=3&query=active-directory)

";

            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureUniqueNameMarkdownRelativeLinkInsideDocsetWithOnlyBookmark()
        {
            var azureMarkdownFileInfoMapping =
                new Dictionary<string, AzureFileInfo>{
                    {
                        "unique.md",
                        new AzureFileInfo
                        {
                            FileName = "unique.md",
                            FilePath = @"c:\root\parent\folder1\subfolder1\unique.md",
                            NeedTransformToAzureExternalLink = false,
                            UriPrefix = "https://azure.microsoft.com/en-us/documentation/articles"
                        }
                    }
                };
            var sourceFilePath = @"c:\root\parent\folder2\subfolder1\source.md";
            var source = @"[azure file link](#bookmark_test)";
            var expected = @"[azure file link](#bookmark_test)

";

            var result = AzureMarked.Markup(source, sourceFilePath, azureMarkdownFileInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureUniqueNameMarkdownRelativeLinkInSameDocsetWithBookmark()
        {
            var azureMarkdownFileInfoMapping =
                new Dictionary<string, AzureFileInfo>{
                    {
                        "unique.md",
                        new AzureFileInfo
                        {
                            FileName = "unique.md",
                            FilePath = @"c:\root\parent\folder2\subfolder1\unique.md",
                            NeedTransformToAzureExternalLink = false,
                            UriPrefix = "https://docsmsftstage.azurewebsites.net/parent"
                        }
                    }
                };
            var sourceFilePath = @"c:\root\parent\folder2\subfolder1\source.md";
            var source = @"[azure file link](unique.md#bookmark_test)";
            var expected = @"[azure file link](unique.md#bookmark_test)

";

            var result = AzureMarked.Markup(source, sourceFilePath, azureMarkdownFileInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureUniqueNameMarkdownRelativeLinkInDifferentDocsetWithBookmark()
        {
            var azureMarkdownFileInfoMapping =
                new Dictionary<string, AzureFileInfo>{
                    {
                        "unique.md",
                        new AzureFileInfo
                        {
                            FileName = "unique.md",
                            FilePath = @"c:\root\parent\folder1\subfolder1\unique.md",
                            NeedTransformToAzureExternalLink = false,
                            UriPrefix = "https://docsmsftstage.azurewebsites.net/parent"
                        }
                    }
                };
            var sourceFilePath = @"c:\root\parent\folder2\subfolder1\source.md";
            var source = @"[azure file link](unique.md#bookmark_test)";
            var expected = @"[azure file link](https://docsmsftstage.azurewebsites.net/parent/unique#bookmark_test)

";

            var result = AzureMarked.Markup(source, sourceFilePath, azureMarkdownFileInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureUniqueNameMarkdownRelativeOutsideDocset()
        {
            var azureMarkdownFileInfoMapping =
                new Dictionary<string, AzureFileInfo>{
                    {
                        "unique.md",
                        new AzureFileInfo
                        {
                            FileName = "unique.md",
                            FilePath = @"c:\root\parent\folder1\subfolder1\unique.md",
                            NeedTransformToAzureExternalLink = true,
                            UriPrefix = "https://azure.microsoft.com/en-us/documentation/articles"
                        }
                    }
                };
            var sourceFilePath = @"c:\root\parent\folder2\subfolder1\source.md";
            var source = @"[azure file link](unique.md)";
            var expected = @"[azure file link](https://azure.microsoft.com/en-us/documentation/articles/unique)

";

            var result = AzureMarked.Markup(source, sourceFilePath, azureMarkdownFileInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureUniqueNameMarkdownRelativeLinkOutsideDocsetWithBookmark()
        {
            var azureMarkdownFileInfoMapping =
                new Dictionary<string, AzureFileInfo>{
                    {
                        "unique.md",
                        new AzureFileInfo
                        {
                            FileName = "unique.md",
                            FilePath = @"c:\root\parent\folder1\subfolder1\unique.md",
                            NeedTransformToAzureExternalLink = true,
                            UriPrefix = "https://azure.microsoft.com/en-us/documentation/articles"
                        }
                    }
                };
            var sourceFilePath = @"c:\root\parent\folder2\subfolder1\source.md";
            var source = @"[azure file link](unique.md#bookmark_test)";
            var expected = @"[azure file link](https://azure.microsoft.com/en-us/documentation/articles/unique#bookmark_test)

";

            var result = AzureMarked.Markup(source, sourceFilePath, azureMarkdownFileInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureVideoLink()
        {
            var azureVideoInfoMapping =
                new Dictionary<string, AzureVideoInfo>{
                    {
                        "azure-ad--introduction-to-dynamic-memberships-for-groups",
                        new AzureVideoInfo
                        {
                            Id = "azure-ad--introduction-to-dynamic-memberships-for-groups",
                            Link = "https://channel9.msdn.com/Series/Azure-Active-Directory-Videos-Demos/Azure-AD--Introduction-to-Dynamic-Memberships-for-Groups/player/"
                        }
                    }
                };

            var source = @"> [AZURE.VIDEO azure-ad--introduction-to-dynamic-memberships-for-groups]";
            var expected = @"<iframe width=""640"" height=""360"" src=""https://channel9.msdn.com/Series/Azure-Active-Directory-Videos-Demos/Azure-AD--Introduction-to-Dynamic-Memberships-for-Groups/player/"" frameborder=""0"" allowfullscreen=""true""></iframe>

";

            var result = AzureMarked.Markup(source, "sourceFile.md", null, azureVideoInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_NormalBlockquoteWith()
        {
            var source = @"> [Just a test for blockquote]";
            var expected = @"> [Just a test for blockquote]

";

            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_NestedList()
        {
            var source = @"* Option 1: **Unregister a Windows 8.1 domain joined device using PC Settings**
  1. On the Windows 8.1 device, navigate to **PC Settings** > **Network** > **Workplace**
  2. Select **Leave**.
This process must be repeated for each domain user that has signed into the machine and has been automatically workplace joined.

* Option 2: Unregister a Windows 8.1 domain joined device using a script
  	1. Open a command prompt on the Windows 8.1 machine and execute the following command:
   ` %SystemRoot%\System32\AutoWorkplace.exe leave`
   
This command must be run in the context of each domain user that has signed into the machine.";

            var expected = @"* Option 1: **Unregister a Windows 8.1 domain joined device using PC Settings**
  
  1. On the Windows 8.1 device, navigate to **PC Settings** > **Network** > **Workplace**
  2. Select **Leave**.
     This process must be repeated for each domain user that has signed into the machine and has been automatically workplace joined.
* Option 2: Unregister a Windows 8.1 domain joined device using a script
  
  1. Open a command prompt on the Windows 8.1 machine and execute the following command:
     ` %SystemRoot%\System32\AutoWorkplace.exe leave`

This command must be run in the context of each domain user that has signed into the machine.

";

            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_NestedList_Bug551102()
        {
            var source = @"1. Click the check mark icon to run the report.
2. If applicable, click **Download** to download the report to a compressed file in comma-separated values (CSV) format for offline viewing or archiving purposes.
   - Up to 75,000 events will be included in the downloaded file.
   - For more data, check out the [Azure AD Reporting API](active-directory-reporting-api-getting-started.md).";
            var expected = @"1. Click the check mark icon to run the report.
2. If applicable, click **Download** to download the report to a compressed file in comma-separated values (CSV) format for offline viewing or archiving purposes.
   * Up to 75,000 events will be included in the downloaded file.
   * For more data, check out the [Azure AD Reporting API](active-directory-reporting-api-getting-started.md).

";

            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        #endregion

        #region Azure migration marked

        [Fact]
        [Trait("Related", "AzureMarkdownMigrationRewriters")]
        public void TestAzureMarkdownMigrationRewriters_AzureVideoLink()
        {
            var azureVideoInfoMapping =
                new Dictionary<string, AzureVideoInfo>{
                    {
                        "azure-ad--introduction-to-dynamic-memberships-for-groups",
                        new AzureVideoInfo
                        {
                            Id = "azure-ad--introduction-to-dynamic-memberships-for-groups",
                            Link = "https://channel9.msdn.com/Series/Azure-Active-Directory-Videos-Demos/Azure-AD--Introduction-to-Dynamic-Memberships-for-Groups/player/"
                        }
                    }
                };

            var source = @"> [AZURE.VIDEO azure-ad--introduction-to-dynamic-memberships-for-groups]";
            var expected = @"> [!VIDEO https://channel9.msdn.com/Series/Azure-Active-Directory-Videos-Demos/Azure-AD--Introduction-to-Dynamic-Memberships-for-Groups/player/]

";

            var result = AzureMigrationMarked.Markup(source, "sourceFile.md", azureVideoInfoMapping: azureVideoInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownMigrationRewriters")]
        public void TestAzureMarkdownMigrationRewriters_AzureVideoLinkNoMapping()
        {
            var azureVideoInfoMapping =
                new Dictionary<string, AzureVideoInfo>{
                    {
                        "fake-azure-ad--introduction-to-dynamic-memberships-for-groups",
                        new AzureVideoInfo
                        {
                            Id = "fake-azure-ad--introduction-to-dynamic-memberships-for-groups",
                            Link = "https://channel9.msdn.com/Series/Azure-Active-Directory-Videos-Demos/Azure-AD--Introduction-to-Dynamic-Memberships-for-Groups/player/"
                        }
                    }
                };

            var source = @"> [AZURE.VIDEO azure-ad--introduction-to-dynamic-memberships-for-groups]";
            var expected = @"> [!VIDEO azure-ad--introduction-to-dynamic-memberships-for-groups]

";

            var result = AzureMigrationMarked.Markup(source, "sourceFile.md", azureVideoInfoMapping: azureVideoInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownMigrationRewriters_AzureProperties_SiteIdentifierAtTheEnd()
        {
            var source = @"<properties
   pageTitle=""Azure Container Service Introduction | Microsoft  Azure ""
   description=""Azure Container Service (ACS) provides a way to simplify the creation, configuration, and management of a cluster of virtual machines that are preconfigured to run containerized applications.""
   services=""virtual-machines""
   documentationCenter=""""
   authors=""rgardler; fenxu""
   manager=""nepeters""
   editor=""""
   tags=""acs, azure-container-service""
   keywords=""Docker, Containers, Micro-services, Mesos, Azure""/>

<tags
   ms.service=""virtual-machines""
   ms.devlang=""na""
   ms.topic=""home-page""
   ms.tgt_pltfrm=""na""
   ms.workload=""na""
   ms.date=""12/02/2015""
   ms.author=""rogardle""/>

# Azure Container Service Introduction
";
            var expected = @"---
title: Azure Container Service Introduction | Microsoft Docs
description: Azure Container Service (ACS) provides a way to simplify the creation, configuration, and management of a cluster of virtual machines that are preconfigured to run containerized applications.
services: virtual-machines
documentationcenter: ''
author: rgardler
manager: nepeters
editor: ''
tags: acs, azure-container-service
keywords: Docker, Containers, Micro-services, Mesos, Azure

ms.service: virtual-machines
ms.devlang: na
ms.topic: home-page
ms.tgt_pltfrm: na
ms.workload: na
ms.date: 12/02/2015
ms.author: rogardle

---
# Azure Container Service Introduction
";
            var result = AzureMigrationMarked.Markup(source, "azure_file.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }


        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownMigrationRewriters_AzureProperties_SiteIdentifierInTheMiddle()
        {
            var source = @"<properties
   pageTitle=""Azure Container Service Introduction |   Microsoft Azure  | Microsoft Azure Storage ""
   description=""Azure Container Service (ACS) provides a way to simplify the creation, configuration, and management of a cluster of virtual machines that are preconfigured to run containerized applications.""
   services=""virtual-machines""
   documentationCenter=""""
   authors=""rgardler; fenxu""
   manager=""nepeters""
   editor=""""
   tags=""acs, azure-container-service""
   keywords=""Docker, Containers, Micro-services, Mesos, Azure""/>

<tags
   ms.service=""virtual-machines""
   ms.devlang=""na""
   ms.topic=""home-page""
   ms.tgt_pltfrm=""na""
   ms.workload=""na""
   ms.date=""12/02/2015""
   ms.author=""rogardle""/>

# Azure Container Service Introduction
";
            var expected = @"---
title: Azure Container Service Introduction | Microsoft Docs
description: Azure Container Service (ACS) provides a way to simplify the creation, configuration, and management of a cluster of virtual machines that are preconfigured to run containerized applications.
services: virtual-machines
documentationcenter: ''
author: rgardler
manager: nepeters
editor: ''
tags: acs, azure-container-service
keywords: Docker, Containers, Micro-services, Mesos, Azure

ms.service: virtual-machines
ms.devlang: na
ms.topic: home-page
ms.tgt_pltfrm: na
ms.workload: na
ms.date: 12/02/2015
ms.author: rogardle

---
# Azure Container Service Introduction
";
            var result = AzureMigrationMarked.Markup(source, "azure_file.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownMigrationRewriters_AzureProperties_NoSiteIdentifier()
        {
            var source = @"<properties
   pageTitle=""Azure Container Service Introduction | Microsoft Azure Storage ""
   description=""Azure Container Service (ACS) provides a way to simplify the creation, configuration, and management of a cluster of virtual machines that are preconfigured to run containerized applications.""
   services=""virtual-machines""
   documentationCenter=""""
   authors=""rgardler; fenxu""
   manager=""nepeters""
   editor=""""
   tags=""acs, azure-container-service""
   keywords=""Docker, Containers, Micro-services, Mesos, Azure""/>

<tags
   ms.service=""virtual-machines""
   ms.devlang=""na""
   ms.topic=""home-page""
   ms.tgt_pltfrm=""na""
   ms.workload=""na""
   ms.date=""12/02/2015""
   ms.author=""rogardle""/>

# Azure Container Service Introduction
";
            var expected = @"---
title: Azure Container Service Introduction | Microsoft Docs
description: Azure Container Service (ACS) provides a way to simplify the creation, configuration, and management of a cluster of virtual machines that are preconfigured to run containerized applications.
services: virtual-machines
documentationcenter: ''
author: rgardler
manager: nepeters
editor: ''
tags: acs, azure-container-service
keywords: Docker, Containers, Micro-services, Mesos, Azure

ms.service: virtual-machines
ms.devlang: na
ms.topic: home-page
ms.tgt_pltfrm: na
ms.workload: na
ms.date: 12/02/2015
ms.author: rogardle

---
# Azure Container Service Introduction
";
            var result = AzureMigrationMarked.Markup(source, "azure_file.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        #endregion
    }
}
