// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { workspace, ExtensionContext, TextDocumentContentProvider, EventEmitter, Event, Uri } from "vscode";
import * as path from "path";

export class TokenTreeContentProvider implements TextDocumentContentProvider {
    private _context: ExtensionContext;
    private _onDidChange = new EventEmitter<Uri>();
    private _jsonContent = "";
    private _port;

    constructor(context: ExtensionContext , port) {
        this._port = port;
        this._context = context;
    }

    private getMediaPath(mediaFile): string {
        return this._context.asAbsolutePath(path.join("media", mediaFile));
    }

    public provideTextDocumentContent(uri: Uri): Thenable<string> {
        return workspace.openTextDocument(Uri.parse(uri.query)).then(document => {
            const content = [
                "<!DOCTYPE html>",
                "<html>",
                "<head>",
                `<meta http-equiv="Content-type" content="text/html;charset=UTF-8">`,
                `<link rel="stylesheet" type="text/css" href="${this.getMediaPath(`token.css`)}" >`,
                `</head>`,
                `<body><!-- ` + this._jsonContent.substring(0, this._jsonContent.length - 1) + `--><!--` + this._port.toString() + `-->`,
                `<div id="tree-container"></div>`,
                `<script type="text/javascript" src="${this.getMediaPath(`jquery-1.6.2.min.js`)}"></script>`,
                `<script type="text/javascript" src="${this.getMediaPath(`d3.v3.min.js`)}"></script>`,
                `<script type="text/javascript" src="${this.getMediaPath(`buildTree.js`)}"></script>`,
                `<script>`,
                `var Jsonstr = document.body.firstChild.nodeValue;`,

                `var treeData = JSON.parse(Jsonstr);`,
                `buildTree("#tree-container" , treeData);`,
                `</script>`,
                `</body>`,
                `</html>`
            ].join("\n");

            return content;
        });
    }

    get onDidChange(): Event<Uri> {
        return this._onDidChange.event;
    }

    public update(uri: Uri, jsonContent: string) {
        this._jsonContent = jsonContent;
        this._onDidChange.fire(uri);
    }
}