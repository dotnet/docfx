// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using MarkdigEngine;

    using Microsoft.DocAsCode.Plugins;
    using Xunit;

    public class YamlHeaderTest
    {
        private static MarkupResult SimpleMarkup(string source)
        {
            var parameter = new MarkdownServiceParameters
            {
                BasePath = "."
            };
            var service = new MarkdigMarkdownService(parameter);
            return service.Markup(source, "Topic.md");
        }

        [Fact(Skip = "Invalid YamlHeader")]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfm_InvalidYamlHeader_YamlUtilityThrowException()
        {
            var source = @"---
- Jon Schlinkert
- Brian Woodward

---";
            var expected = @"<hr />
<ul>
<li>Jon Schlinkert</li>
<li>Brian Woodward</li>
</ul>
<hr />
";
            var marked = SimpleMarkup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }


        [Fact(Skip = "Invalid YamlHeader")]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmYamlHeader_YamlUtilityReturnNull()
        {
            var source = @"---

### /Unconfigure

---";
            var expected = @"<hr />
<h3 id=""unconfigure"">/Unconfigure</h3>
<hr />
";
            var marked = SimpleMarkup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }
        
        [Fact]
        public void TestDfmYamlHeader_General()
        {
            //arange
            var content = @"---
title: ""如何使用 Visual C++ 工具集报告问题 | Microsoft Docs""
ms.custom: 
ms.date: 11/04/2016
ms.reviewer: 
ms.suite: 
ms.technology:
- cpp
ms.tgt_pltfrm: 
ms.topic: article
dev_langs:
- C++
ms.assetid: ec24a49c-411d-47ce-aa4b-8398b6d3e8f6
caps.latest.revision: 8
author: corob-msft
ms.author: corob
manager: ghogen
translation.priority.mt:
- cs-cz
- pl-pl
- pt-br
- tr-tr
translationtype: Human Translation
ms.sourcegitcommit: 5c6fbfc8699d7d66c40b0458972d8b6ef0dcc705
ms.openlocfilehash: 2ea129ac94cb1ddc7486ba69280dc0390896e088
---";
            // act
            var marked = TestUtility.MarkupWithoutSourceInfo(content, "Topic.md");

            // assert
            var expected = @"<yamlheader start=""1"" end=""26"">title: &quot;如何使用 Visual C++ 工具集报告问题 | Microsoft Docs&quot;
ms.custom: 
ms.date: 11/04/2016
ms.reviewer: 
ms.suite: 
ms.technology:
- cpp
ms.tgt_pltfrm: 
ms.topic: article
dev_langs:
- C++
ms.assetid: ec24a49c-411d-47ce-aa4b-8398b6d3e8f6
caps.latest.revision: 8
author: corob-msft
ms.author: corob
manager: ghogen
translation.priority.mt:
- cs-cz
- pl-pl
- pt-br
- tr-tr
translationtype: Human Translation
ms.sourcegitcommit: 5c6fbfc8699d7d66c40b0458972d8b6ef0dcc705
ms.openlocfilehash: 2ea129ac94cb1ddc7486ba69280dc0390896e088</yamlheader>";
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }
    }
}
