# DocFX Flavored Markdown Preview
------------

[![Current Version](http://vsmarketplacebadge.apphb.com/version/docfxsvc.DfmPreview.svg)](http://marketplace.visualstudio.com/items?itemName=docfxsvc.DfmPreview)
[![Install Count](http://vsmarketplacebadge.apphb.com/installs/docfxsvc.DfmPreview.svg)](https://marketplace.visualstudio.com/items?itemName=docfxsvc.DfmPreview)
[![Open Issues](http://vsmarketplacebadge.apphb.com/rating/docfxsvc.DfmPreview.svg) ](https://marketplace.visualstudio.com/items?itemName=docfxsvc.DfmPreview)

An extension to support [**DFM**](https://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html) for Visual Studio Code! The preview provides the following features:

* Preview the `DFM` to the side
* Preview the `TokenTree` to the side
* Match the markdown file to the tokenTree node
* Match the tokenTree node to the markdown file

# Quick Start
* Install the extension
> **Note:** Upgrade to Visual Studio Code 1.3.0 or above.
* Open a markdown file which includes `DFM` syntax
* Use the `Preview` and `TokenTree`
> **Note:** If you want to use `DFM` features `File include` and `Code Snippets`, You have to open a folder which includes your target markdown files

# Document
For further information and details about DocFX Flavored Markdown, please reference [DocFX Flavored Markdown](https://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html)

# Feature Details
## Live preview
| Shortcuts | command title | command |
|:-------|:--------|:--------|
| `ctrl+shift+q` | `Toggle Dfm Preview` | Preview  |
| `ctrl+k q` | `Open Dfm Preview to the side` | Preview to side |
|  | `show Dfm Show` | Show Source |

  ![PreviewToside](img/previewToSide.gif)

## Token tree
| Shortcuts | command title | command |
|:-------|:--------|:--------|
| `ctrl+shift+t` | `Open Dfm Preview to the side` | TokenTreeToSide  |

  - Expand and collapse the nodes by clicking the circle of node

  - Display the detailed information of node on mouseover

  ![TokenTree](img/Tokentree.gif)

- Match between markdown file with tokenTree Node
  - Click/select the text you want to match to the tokenTree
    > You can select multiple lines.
  - Click the text of node to match to the markdown file

  ![Match](img/Match.gif)

# Found a Bug?
Please file any issue through the [Github Issue](https://github.com/dotnet/docfx/issues) system.

# Development
* First install
  * Visual Studio Code(above 1.3.0)
  * Node.js(Npm included)

* To run and develop do the following:
  * Run  `npm install` under the root dir of this extension
  * Open in Visual Studio Code(run `code .` under the root dir of this extension)
    > Cannot find module 'vscode'? Run `npm run postinstall` under the root dir of this extension, according to [Cannot find module 'vscode' – where is vscode.d.ts now installed? #2810](https://github.com/Microsoft/vscode/issues/2810)
  * Press `F5` to debug

# Source
[docfx/src/VscPreviewExtension](https://github.com/dotnet/docfx/tree/dev/src/VscPreviewExtension)

# Licences
*DocFX* is licensed under the [MIT license](LICENSE).

# Change Log
### Current Version **0.0.21**
* **0.0.21**
  * Bug fix: remove background block of the inline code
* **0.0.20**
  * Improve: remove the `img` folder in the publish version
* **0.0.19**
  * Bug fix: background block of the code snippets missed
* **0.0.18**
  * Improve: remove some useless files in the publish version
* **0.0.17**
  * Improve: enrich detail information of node in token tree
* **0.0.16**
  * Bug fix: file open outside the openfolder
  * Feature add: preview Match
* **0.0.15**
  * Bug fix: refresh the viewSize when resize the tokenTree window
  * Bug fix: bug of note block
* **0.0.14**
  * Bug fix: bug because no open folder
* **0.0.13**
  * Improve: edit README
* **0.0.12**
  * Bug fix: escape error
* **0.0.11**
  * Add feature: tokenTree preview
* **0.0.10**
  * Bug fix: can't toggle the default markdown preview after dfmPreview
* **0.0.9**
  * Bug fix: can't preview a single file(not in a open folder)
  * Bug fix: throw an error if it is not a markdown file
*  **0.0.8**
  * Bug fix: open other preview window
  * Bug fix: some css and JavaScript files to show the html
* **0.0.7**
  * Initial Release!

# TODO
* Support `DFM` feature `YamlHeader`
* Support `DFM` feature `Cross Reference`
* Match between markdown file with preview
* Auto trigger the tokenTree refresh when the textEditor change
* `Cross-platform`
* User configurable

# Found issue
* Preview match: File inclusion
* Match when multiple open editor
