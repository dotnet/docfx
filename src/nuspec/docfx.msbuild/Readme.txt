Doc-As-Code
===========
Easily build and publish your API documentation. We currently support C# and VB projects.

Code Walkthrough Quick Start
---------------
### BackEnd projects under `BackEnd` folder
BackEnd code is using Roslyn to compiler and analysis code, exporting API metadata to YAML format, as described in [DotNet Metadata Specification](http://vicancy.github.io/docascode/#/specs!metadata_format_spec.md).

### FrontEnd projects under `FrontEnd` folder
FrontEnd code is written in AngularJs, and using Grunt to manage code, karma to run tests.

Under FrontEnd folder, run `grunt server` to playaround with a sample website.

Start Using DocFx
---------------
Refer to [Getting Started with DocFx](http://vicancy.github.io/docascode/#/README.md) to play around DocFx.
