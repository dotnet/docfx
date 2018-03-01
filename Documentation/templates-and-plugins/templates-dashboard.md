﻿---
title: Available templates and themes
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
      description: The template similar to default template however with static toc. With static toc, the generated web pages can be previewed from local file system.
      type: Embedded
      thumbnail: ~/templates-and-plugins/images/default.green.screenshot.png
      homepage: https://github.com/dotnet/docfx/tree/dev/src/docfx.website.themes/statictoc
      repository:
        type: git
        url: "https://github.com/dotnet/docfx/tree/dev/src/docfx.website.themes/statictoc"
      usage:
        command: "-t statictoc"
        config: '"template": "statictoc"'
    - name: mathew
      description: A simple template
      type: External
      author: MathewSachin
      version: 1.0.0
      engines:
        docfx: ">=2.17.4"
      thumbnail: ~/templates-and-plugins/images/mathew.screenshot.png
      homepage: https://github.com/MathewSachin/docfx-tmpl
      repository:
        type: git
        url: "https://github.com/MathewSachin/docfx-tmpl.git"
      license: MIT
      usage:
        init: "git clone https://github.com/MathewSachin/docfx-tmpl.git mathew"
        command: "-t default,mathew/src"
        config: '"template":["default","mathew/src"]'
    - name: docfx-lightbox-plugin (Featherlight)
      description: A template which adds a lightbox to each image, using the jquery plugin Featherlight.
      type: External
      author: roel4ez
      version: 1.0.0
      engines:
        docfx: ">=2.20.0"
      thumbnail: ~/templates-and-plugins/images/roel4ez-feather.screenshot.png
      homepage: https://github.com/roel4ez/docfx-lightbox-plugin
      repository:
        type: git
        url: "https://github.com/roel4ez/docfx-lightbox-plugin.git"
      license: MIT
      usage:
        init: "git clone https://github.com/roel4ez/docfx-lightbox-plugin.git docfx-lightbox-plugin"
        command: "-t default,docfx-lightbox-plugin/templates/lightbox-featherlight"
        config: '"template":["default","docfx-lightbox-plugin/templates/lightbox-featherlight"]'
    - name: docfx-lightbox-plugin (Bootstrap Modal)
      description: A template which adds a lightbox to each image, using the Modal window from Bootstrap.
      type: External
      author: roel4ez
      version: 1.0.0
      engines:
        docfx: ">=2.20.0"
      thumbnail: ~/templates-and-plugins/images/roel4ez-modal.screenshot.png
      homepage: https://github.com/roel4ez/docfx-lightbox-plugin
      repository:
        type: git
        url: "https://github.com/roel4ez/docfx-lightbox-plugin.git"
      license: MIT
      usage:
        init: "git clone https://github.com/roel4ez/docfx-lightbox-plugin.git docfx-lightbox-plugin"
        command: "-t default,docfx-lightbox-plugin/templates/bootstrap-modal"
        config: '"template":["default","docfx-lightbox-plugin/templates/bootstrap-modal"]'
---

# Dashboard for Templates
The templates listed here mainly focus on the layout and themes of the generated website. Add your own customized templates here for others to view and use!
