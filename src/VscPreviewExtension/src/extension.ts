// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict";

import { workspace, window, ExtensionContext, commands, Event, Uri, ViewColumn, TextDocument, Selection } from "vscode";
import * as path from "path";
import { PreviewCore } from "./previewCore";
import { TokenTreeCore } from "./tokenTreeCore";

const markdownScheme = "markdown";
const tokenTreeScheme = "tokenTree";
const port = 8080;

let startLine = 0;
let endLine = 0;

export function activate(context: ExtensionContext) {
    let dfmProcess = new PreviewCore(context);
    let tokenTreeProcess = new TokenTreeCore(context, port);
    let previewProviderRegistration = workspace.registerTextDocumentContentProvider(markdownScheme, dfmProcess.provider);
    let tokenTreeProviderRegistration = workspace.registerTextDocumentContentProvider(tokenTreeScheme, tokenTreeProcess.provider);

    // Event register
    let showPreviewRegistration = commands.registerCommand("DFM.showPreview", uri => showPreview(dfmProcess));
    let showPreviewToSideRegistration = commands.registerCommand("DFM.showPreviewToSide", uri => showPreview(dfmProcess, uri, true));
    let showSourceRegistration = commands.registerCommand("DFM.showSource", showSource);
    let showTokenTreeToSideRegistration = commands.registerCommand("DFM.showTokenTreeToSide", uri => showTokenTree(tokenTreeProcess));

    context.subscriptions.push(showPreviewRegistration, showPreviewToSideRegistration, showSourceRegistration, showTokenTreeToSideRegistration);
    context.subscriptions.push(previewProviderRegistration, tokenTreeProviderRegistration);

    workspace.onDidSaveTextDocument(document => {
        if (isMarkdownFile(document)) {
            const uri = getMarkdownUri(document.uri);
            dfmProcess.callDfm(uri);

            tokenTreeProcess.callDfm(getTokenTreeUri(document.uri));
        }
    });

    workspace.onDidChangeTextDocument(event => {
        if (isMarkdownFile(event.document)) {
            const uri = getMarkdownUri(event.document.uri);
            dfmProcess.callDfm(uri);
            // TODO: make token tree change synchronous
            // tokenTreeProcess.callDfm(getTokenTreeUri(event.document.uri));
        }
    });

    workspace.onDidChangeConfiguration(() => {
        workspace.textDocuments.forEach(document => {
            if (document.uri.scheme === markdownScheme) {
                dfmProcess.callDfm(document.uri);
            } else if (document.uri.scheme === tokenTreeScheme) {
                tokenTreeProcess.callDfm(document.uri);
            }
        });
    });

    window.onDidChangeTextEditorSelection(event => {
        startLine = event.selections[0].start.line + 1;
        endLine = event.selections[0].end.line + 1;
    });

    // Http server to communicate with js
    let http = require("http");
    let server = http.createServer();
    server.on("request", function (req, res) {
        let requestInfo = req.url.split("/");
        if (requestInfo[1] === "lineNumber") {
            mapToSelection(parseInt(requestInfo[2]), parseInt(requestInfo[3]));
            res.writeHead(200, { "Content-Type": "text/plain" });
            res.end("success");
        } else {
            res.writeHead(200, { "Content-Type": "text/plain" });
            res.write(startLine + " " + endLine);
            res.end();
        }
    });
    try {
        server.listen(port);
    }
    catch (e) {
        window.showErrorMessage(e);
    }
}

function mapToSelection(startLineNumber, endLineNumber) {
    return commands.executeCommand("workbench.action.navigateBack").then(() => {
        window.activeTextEditor.selection = new Selection(startLineNumber - 1, 0, endLineNumber - 1, window.activeTextEditor.document.lineAt(endLineNumber - 1).range.end.character);
    });
}

// Check the file type
function isMarkdownFile(document: TextDocument) {
    // Prevent processing of own documents
    return document.languageId === "markdown" && document.uri.scheme === "file";
}

function getMarkdownUri(uri: Uri) {
    return uri.with({ scheme: markdownScheme, path: uri.path + ".renderedDfm", query: uri.toString() });
}

function getTokenTreeUri(uri: Uri) {
    return uri.with({ scheme: tokenTreeScheme, path: uri.path + ".renderedTokenTree", query: uri.toString() });
}

function getViewColumn(sideBySide): ViewColumn {
    const active = window.activeTextEditor;
    if (!active) {
        return ViewColumn.One;
    }

    if (!sideBySide) {
        return active.viewColumn;
    }

    switch (active.viewColumn) {
        case ViewColumn.One:
            return ViewColumn.Two;
        case ViewColumn.Two:
            return ViewColumn.Three;
    }

    return active.viewColumn;
}

function showSource() {
    return commands.executeCommand("workbench.action.navigateBack");
}

function showPreview(dfmPreview: PreviewCore, uri?: Uri, sideBySide: boolean = false) {
    dfmPreview.isFirstTime = true;
    let resource = uri;
    if (!(resource instanceof Uri)) {
        if (window.activeTextEditor) {
            resource = window.activeTextEditor.document.uri;
        } else {
            // This is most likely toggling the preview
            return commands.executeCommand("DFM.showSource");
        }
    }

    let thenable = commands.executeCommand("vscode.previewHtml",
        getMarkdownUri(resource),
        getViewColumn(sideBySide),
        `DfmPreview "${path.basename(resource.fsPath)}"`);

    dfmPreview.callDfm(getMarkdownUri(resource));
    return thenable;
}

function showTokenTree(tokenTree: TokenTreeCore, uri?: Uri) {
    let resource = uri;
    if (!(resource instanceof Uri)) {
        if (window.activeTextEditor) {
            resource = window.activeTextEditor.document.uri;
        } else {
            // This is most likely toggling the preview
            return commands.executeCommand("DFM.showSource");
        }
    }

    let thenable = commands.executeCommand("vscode.previewHtml",
        getTokenTreeUri(resource),
        getViewColumn(true),
        `TokenTree '${path.basename(resource.fsPath)}'`);

    tokenTree.callDfm(getTokenTreeUri(resource));
    return thenable;
}