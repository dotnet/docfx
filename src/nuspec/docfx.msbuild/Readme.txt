DocFx is an API documentation generator for .NET, currently support C# and VB, as similar to JSDoc or Sphnix. It has the ability to extract triple slash comments out from your source code. What's more, it has syntax to link additional files to API to add additional remarks. DocFx will scan your source code and your additional conceptual files and generate a complete HTML documentation website for you. The website is currently written in AngularJS, but DocFx provides the flexibility for you to customize the website through specifying templates.

To quickly get started, after installing current nuget package, build current project, the output will by default be generated into '_site' folder, which is defined in 'docfx.json' file. If current project is a WEBSITE project, you can navigate to <host>/_site/ to view the generated website! 

For more details on how to customize 'docfx.json' file, please refer to http://aspnet.github.io/docfx/#/tutorial/docfx.exe_user_manual.md. 
