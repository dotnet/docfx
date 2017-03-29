﻿---
title: Available Templates
documentType: dashboard
contributionLink: ~/templates-and-plugins/contribute-your-template.md
templates: 
    - name: default
      description: The default template
      type: Embedded
      thumbnail: ~/templates-and-plugins/images/default.screenshot.png
      homepage: https://github.com/dotnet/docfx/tree/dev/src/docfx.website.themes/default
      repository:
        type: git
        url: "https://github.com/dotnet/docfx/tree/dev/src/docfx.website.themes/default"
    - name: statictoc
      description: The template similar to default template however with static toc
      type: Embedded
      thumbnail: ~/templates-and-plugins/images/default.green.screenshot.png
      homepage: https://github.com/dotnet/docfx/tree/dev/src/docfx.website.themes/statictoc
      repository:
        type: git
        url: "https://github.com/dotnet/docfx/tree/dev/src/docfx.website.themes/statictoc"
      usage:
        command: "-t default,statictoc"
        config: "template: [default, statictoc]"
    - name: sideway
      description: A simple template
      type: Internal
      author: DocAsCode
      version: 0.0.1
      engines:
        docfx: "^2.15"
      thumbnail: ~/templates-and-plugins/images/default.screenshot.png
      homepage: https://github.com/dotnet/docfx/tree/dev/src/docfx.website.themes/default
      repository:
        type: git
        url: "https://github.com/dotnet/docfx/tree/dev/src/docfx.website.themes/default"
      license: MIT
---

# Dashboard for Templates
Here lists all the available templates for `docfx build`. Add your own customized templates here for others to view and use!