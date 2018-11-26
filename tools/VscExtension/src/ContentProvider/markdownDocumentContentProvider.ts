// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { workspace, Uri } from "vscode";
import * as path from "path";
import { ContentProvider } from "./contentProvider";

export class MarkdownDocumentContentProvider extends ContentProvider {
    public fileName;

    public provideTextDocumentContent(uri: Uri): Thenable<string> {
        return workspace.openTextDocument(Uri.parse(uri.query)).then(document => {
            const head = [
                "<!DOCTYPE html>",
                "<html>",
                "<head>",
                `<meta http-equiv="Content-type" content="text/html;charset=UTF-8">`,
                `<meta name="port" content="${ContentProvider.port.toString()}">`,
                `<meta name="fileName" content="${this.fileName}">`,
                `<link rel="stylesheet" type="text/css" href="${this.getNodeModulesPath(path.join("highlightjs", "styles", "tomorrow-night-bright.css"))}" >`,
                `<link rel="stylesheet" type="text/css" href="${this.getMediaCssPath("markdown.css")}" >`,
                `<base href="${document.uri.toString(true)}">`,
                "</head>",
                `<body>`].join("\n");

            const body = this._content || "";

            const tail = [
                `<script type="text/javascript" src="${this.getNodeModulesPath(path.join('jquery', 'dist', 'jquery.min.js'))}"></script>`,
                `<script type="text/javascript" src="${this.getNodeModulesPath(path.join("highlightjs", "highlight.pack.js"))}"></script>`,
                `<script type="text/javascript" src="${this.getMediaJsPath("previewMatch.js")}"></script>`,
                `<script>hljs.initHighlightingOnLoad();</script>`,
                "</body>",
                "</html>"
            ].join("\n");

            return head + body + tail;
        });
    }
}
