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
> 
> 

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
> > > 
> > > 
> > 
> > 
> 
> 

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
> 
> 

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
> 
> 

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
> > > 
> > > 
> > 
> > 
> 
> 

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
this is http link [text](http://www.google.com ""Google"") ref
this is http escape link [text](http://www.google.com'dd#bookmark ""Google's homepage"") ref
this is absolute link [text](c:/this/is/markdown ""Local File"") file ref";
            var expected = @"# Test Markdownlink extension
this is a missing extension link [text](missing_extension.md) file ref
this is a normal link [text](missing_extension.md) file ref
this is a missing extension link with bookmark [text](missing_extension.md#bookmark) file ref
this is a normal link with bookmark [text](missing_extension.md#bookmark) file ref
this is http link [text](http://www.google.com ""Google"") ref
this is http escape link [text](http://www.google.com'dd#bookmark ""Google's homepage"") ref
this is absolute link [text](c:/this/is/markdown ""Local File"") file ref

";

            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
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

        [Fact(Skip = "Not Completed")]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AutoLink()
        {
            var source = @" See [http://www.openldap.org/doc/admin24/overlays.html#Access Logging](http://www.openldap.org/doc/admin24/overlays.html#Access Logging)";
            var expected = @" See [http://www.openldap.org/doc/admin24/overlays.html#Access Logging](http://www.openldap.org/doc/admin24/overlays.html#Access Logging)

";
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact(Skip = "Not Completed")]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_LinkRefWithBracket()
        {
            var source = @"[User-Defined Date/Time Formats (Format Function)](http://msdn2.microsoft.com/library/73ctwf33\(VS.90\).aspx)";
            var expected = @" See [http://www.openldap.org/doc/admin24/overlays.html#Access Logging](http://www.openldap.org/doc/admin24/overlays.html#Access Logging)

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
> * [Universal Windows](../articles/notification-hubs-windows-store-dotnet-get-started/.md)
> * [Windows Phone](../articles/notification-hubs-windows-phone-get-started/.md)
> * [iOS](../articles/notification-hubs-ios-get-started/.md)
> * [Android](../articles/notification-hubs-android-get-started/.md)
> * [Kindle](../articles/notification-hubs-kindle-get-started/.md)
> * [Baidu](../articles/notification-hubs-baidu-get-started/.md)
> * [Xamarin.iOS](../articles/partner-xamarin-notification-hubs-ios-get-started/.md)
> * [Xamarin.Android](../articles/partner-xamarin-notification-hubs-android-get-started/.md)
> 
> 

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
> 
> 

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
   authors=""rgardler""
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
documentationcenter: 
authors: rgardler
manager: nepeters
editor: 
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
            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureUniqueNameMarkdownRelativeLinkInsideDocset()
        {
            var azureFileInfoMapping =
                new Dictionary<string, AzureFileInfo>{
                    {
                        "unique.md",
                        new AzureFileInfo
                        {
                            FileName = "unique.md",
                            FilePath = @"c:\root\parent\folder1\subfolder1\unique.md",
                            NeedTransformToExternalLink = false,
                            UriPrefix = "https://azure.microsoft.com/en-us/documentation/articles"
                        }
                    }
                };
            var sourceFilePath = @"c:\root\parent\folder2\subfolder1\source.md";
            var source = @"[azure file link](unique.md)";
            var expected = @"[azure file link](../../folder1/subfolder1/unique.md)

";

            var result = AzureMarked.Markup(source, sourceFilePath, azureFileInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureUniqueNameMarkdownRelativeLinkInsideDocsetWithOnlyBookmark()
        {
            var azureFileInfoMapping =
                new Dictionary<string, AzureFileInfo>{
                    {
                        "unique.md",
                        new AzureFileInfo
                        {
                            FileName = "unique.md",
                            FilePath = @"c:\root\parent\folder1\subfolder1\unique.md",
                            NeedTransformToExternalLink = false,
                            UriPrefix = "https://azure.microsoft.com/en-us/documentation/articles"
                        }
                    }
                };
            var sourceFilePath = @"c:\root\parent\folder2\subfolder1\source.md";
            var source = @"[azure file link](#bookmark_test)";
            var expected = @"[azure file link](#bookmark_test)

";

            var result = AzureMarked.Markup(source, sourceFilePath, azureFileInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureUniqueNameMarkdownRelativeLinkInsideDocsetWithBookmark()
        {
            var azureFileInfoMapping =
                new Dictionary<string, AzureFileInfo>{
                    {
                        "unique.md",
                        new AzureFileInfo
                        {
                            FileName = "unique.md",
                            FilePath = @"c:\root\parent\folder1\subfolder1\unique.md",
                            NeedTransformToExternalLink = false,
                            UriPrefix = "https://azure.microsoft.com/en-us/documentation/articles"
                        }
                    }
                };
            var sourceFilePath = @"c:\root\parent\folder2\subfolder1\source.md";
            var source = @"[azure file link](unique.md#bookmark_test)";
            var expected = @"[azure file link](../../folder1/subfolder1/unique.md#bookmark_test)

";

            var result = AzureMarked.Markup(source, sourceFilePath, azureFileInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureUniqueNameMarkdownRelativeOutsideDocset()
        {
            var azureFileInfoMapping =
                new Dictionary<string, AzureFileInfo>{
                    {
                        "unique.md",
                        new AzureFileInfo
                        {
                            FileName = "unique.md",
                            FilePath = @"c:\root\parent\folder1\subfolder1\unique.md",
                            NeedTransformToExternalLink = true,
                            UriPrefix = "https://azure.microsoft.com/en-us/documentation/articles"
                        }
                    }
                };
            var sourceFilePath = @"c:\root\parent\folder2\subfolder1\source.md";
            var source = @"[azure file link](unique.md)";
            var expected = @"[azure file link](https://azure.microsoft.com/en-us/documentation/articles/unique)

";

            var result = AzureMarked.Markup(source, sourceFilePath, azureFileInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_AzureUniqueNameMarkdownRelativeLinkOutsideDocsetWithBookmark()
        {
            var azureFileInfoMapping =
                new Dictionary<string, AzureFileInfo>{
                    {
                        "unique.md",
                        new AzureFileInfo
                        {
                            FileName = "unique.md",
                            FilePath = @"c:\root\parent\folder1\subfolder1\unique.md",
                            NeedTransformToExternalLink = true,
                            UriPrefix = "https://azure.microsoft.com/en-us/documentation/articles"
                        }
                    }
                };
            var sourceFilePath = @"c:\root\parent\folder2\subfolder1\source.md";
            var source = @"[azure file link](unique.md#bookmark_test)";
            var expected = @"[azure file link](https://azure.microsoft.com/en-us/documentation/articles/unique#bookmark_test)

";

            var result = AzureMarked.Markup(source, sourceFilePath, azureFileInfoMapping);
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
            var expected = @"<iframe width=""0"" height=""0"" src=""https://channel9.msdn.com/Series/Azure-Active-Directory-Videos-Demos/Azure-AD--Introduction-to-Dynamic-Memberships-for-Groups/player/"" frameborder=""0"" allowfullscreen=""true""></iframe>

";

            var result = AzureMarked.Markup(source, null, null, azureVideoInfoMapping);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "AzureMarkdownRewriters")]
        public void TestAzureMarkdownRewriters_NormalBlockquoteWith()
        {
            var source = @"> [Just a test for blockquote]";
            var expected = @"> [Just a test for blockquote]
> 
> 

";

            var result = AzureMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }
    }
}
