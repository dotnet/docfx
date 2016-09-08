// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { workspace, ExtensionContext, TextDocumentContentProvider, EventEmitter, Event, Uri } from "vscode";
import * as path from "path";

export class MarkdownDocumentContentProvider implements TextDocumentContentProvider {
    // TODO: extract the common pattern from this class with TokenTreeContentProvider
    private _context: ExtensionContext;
    private _onDidChange = new EventEmitter<Uri>();
    private _htmlContent: string;

    constructor(context: ExtensionContext) {
        this._context = context;
    }

    private getMediaPath(mediaFile): string {
        return this._context.asAbsolutePath(path.join("media", mediaFile));
    }

    public provideTextDocumentContent(uri: Uri): Thenable<string> {
        return workspace.openTextDocument(Uri.parse(uri.query)).then(document => {
            const head = [
                "<!DOCTYPE html>",
                "<html>",
                "<head>",
                `<meta http-equiv="Content-type" content="text/html;charset=UTF-8">`,
                `<link rel="stylesheet" type="text/css" href="${this.getMediaPath("tomorrow.css")}" >`,
                `<link rel="stylesheet" type="text/css" href="${this.getMediaPath("markdown.css")}" >`,
                `<base href="${document.uri.toString(true)}">`,
                "</head>",
                "<body>"].join("\n");

            const body = this._htmlContent || "";

            const tail = [
                `<script type="text/javascript" src="${this.getMediaPath("highlight.pack.js")}"></script>`,
                `<script>hljs.initHighlightingOnLoad();</script>`,
                "</body>",
                "</html>"
            ].join("\n");

            return head + body + tail;
        });
    }

    get onDidChange(): Event<Uri> {
        return this._onDidChange.event;
    }

    public update(uri: Uri, previewContent: string) {
        this._htmlContent = previewContent;
        this._onDidChange.fire(uri);
    }
}