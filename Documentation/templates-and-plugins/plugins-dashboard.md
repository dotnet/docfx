﻿---
title: Available plugins and templates
documentType: dashboard
contributionLink: ~/templates-and-plugins/contribute-your-template.md
templates: 
    - name: memberpage
      description: It splits the *YAML* data model into member level. Currently it supports ManagedReference document type. With this template enabled, the class page contains lists of method overloads, fields, events and so on, while every method overload, field or event displays in a separated page.
      type: Internal
      thumbnail: ~/templates-and-plugins/images/memberpage.default.screenshot.png
      homepage: https://www.nuget.org/packages/memberpage/
      repository:
        type: git
        url: "https://github.com/dotnet/docfx/tree/master/plugins/Microsoft.DocAsCode.Build.MemberLevelManagedReference"
      usage:
        init: "nuget install memberpage -OutputDirectory <output>"
        command: "-t default,<output>/memberpage.<version>/content"
        config: 'template: ["statictoc", "<output>/memberpage.<version>/content"]'
    - name: rest.tagpage
      description: It splits the *REST* model into tag level model. With this plugin enabled, operations with the same tag are grouped into one page. If the operation is in multiple tags, it would be included in first tag level page.
      type: Internal
      thumbnail: ~/templates-and-plugins/images/rest.tagpage.default.screenshot.png
      homepage: https://www.nuget.org/packages/rest.tagpage/
      repository:
        type: git
        url: "https://github.com/dotnet/docfx/tree/master/plugins/Microsoft.DocAsCode.Build.TagLevelRestApi"
      usage:
        init: "nuget install rest.tagpage -OutputDirectory <output>"
        command: "-t default,<output>/rest.tagpage.<version>/content"
        config: 'template: ["default", "<output>/rest.tagpage.<version>/content"]'
    - name: rest.operationpage
      description: It splits the *REST* model into operation level model. If it's enabled together with `rest.tagpage`, the *REST* model will split to tag level first, then split to operation level.
      type: Internal
      thumbnail: ~/templates-and-plugins/images/rest.operationpage.default.screenshot.png
      homepage: https://www.nuget.org/packages/rest.operationpage/
      repository:
        type: git
        url: "https://github.com/dotnet/docfx/tree/master/plugins/Microsoft.DocAsCode.Build.OperationLevelRestApi"
      usage:
        init: "nuget install rest.operationpage -OutputDirectory <output>"
        command: "-t default,<output>/rest.operationpage.<version>/content"
        config: 'template: ["default", "<output>/rest.operationpage.<version>/content"]'
---

# Dashboard for Plugins
The plugins listed here mainly focus on the functionality of the generated website, it often contains a *plugins* folder containing assemblies for advanced processing of the data model. It usually also contains corresponding renderer files to further transform the processed data model to web pages. You can follow [How to Create Custom Document Processors](../tutorial/howto_build_your_own_type_of_documentation_with_custom_plug-in.md) and [How to Create Custom PostProcessor](../tutorial/howto_add_a_customized_post_processor.md) to create your own plugins. Add your own customized templates and plugins here for others to view and use!
