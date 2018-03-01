// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict";

import { workspace, window, ExtensionContext, commands, Event, Uri, ViewColumn, TextDocument, Selection } from "vscode";
import * as path from "path";

import { DfmPreviewProcessor } from "./Processors/dfmPreviewProcessor";
import { DocFXPreviewProcessor } from "./Processors/docfxPreviewProcessor";
import { TokenTreeProcessor } from "./Processors/tokenTreeProcessor";
import { PreviewProcessor } from "./Processors/previewProcessor";
import { ContentProvider } from "./ContentProvider/contentProvider";
import * as ConstVariable from "./constVariables/commonVariables";
import { PreviewType } from "./constVariables/previewType";

export function activate(context: ExtensionContext) {
    let dfmPreviewProcessor = new DfmPreviewProcessor(context);
    let tokenTreeProcessor = new TokenTreeProcessor(context);
    let docFXPreviewProcessor = new DocFXPreviewProcessor(context);
    let previewProviderRegistration = workspace.registerTextDocumentContentProvider(ConstVariable.markdownScheme, dfmPreviewProcessor.provider);
    let tokenTreeProviderRegistration = workspace.registerTextDocumentContentProvider(ConstVariable.tokenTreeScheme, tokenTreeProcessor.provider);

    // Event register
    let showPreviewRegistration = commands.registerCommand("DocFX.showDfmPreview", uri => showPreview(dfmPreviewProcessor));
    let showPreviewToSideRegistration = commands.registerCommand("DocFX.showDfmPreviewToSide", uri => showPreview(dfmPreviewProcessor, uri, true));
    let showDocFXPreviewToSideRegistration = commands.registerCommand("DocFX.showDocFXPreviewToSide", uri => showDocFXPreview(docFXPreviewProcessor, uri, true));
    let showSourceRegistration = commands.registerCommand("DocFX.showSource", showSource);
    let showTokenTreeToSideRegistration = commands.registerCommand("DocFX.showTokenTreeToSide", uri => showTokenTree(tokenTreeProcessor));

    context.subscriptions.push(showPreviewRegistration, showPreviewToSideRegistration, showDocFXPreviewToSideRegistration, showSourceRegistration, showTokenTreeToSideRegistration);
    context.subscriptions.push(previewProviderRegistration, tokenTreeProviderRegistration);

    workspace.onDidSaveTextDocument(document => {
        if (isMarkdownFile(document)) {
            const uri = getMarkdownUri(document.uri);
            switch (PreviewProcessor.previewType) {
                case PreviewType.dfmPreview:
                    dfmPreviewProcessor.updateContent(uri);
                    break;
                case PreviewType.docFXPreview:
                    docFXPreviewProcessor.updateContent(uri);
                    break;
                case PreviewType.tokenTreePreview:
                    // TODO: make token tree change synchronous
                    break;
            }
        }
    });

    workspace.onDidChangeTextDocument(event => {
        if (isMarkdownFile(event.document)) {
            const uri = getMarkdownUri(event.document.uri);
            switch (PreviewProcessor.previewType) {
                case PreviewType.dfmPreview:
                    dfmPreviewProcessor.updateContent(uri);
                    break;
                case PreviewType.docFXPreview:
                    docFXPreviewProcessor.updateContent(uri);
                    break;
                case PreviewType.tokenTreePreview:
                    // TODO: make token tree change synchronous
                    break;
            }
        }
    });

    workspace.onDidChangeConfiguration(() => {
        workspace.textDocuments.forEach(document => {
            if (document.uri.scheme === ConstVariable.markdownScheme) {
                dfmPreviewProcessor.updateContent(document.uri);
            } else if (document.uri.scheme === ConstVariable.tokenTreeScheme) {
                tokenTreeProcessor.updateContent(document.uri);
            }
        });
    });

    let startLine = 0;
    let endLine = 0;

    window.onDidChangeTextEditorSelection(event => {
        startLine = event.selections[0].start.line + 1;
        endLine = event.selections[0].end.line + 1;
    });

    // Http server to communicate with js
    let http = require("http");
    let server = http.createServer();
    server.on("request", function (req, res) {
        let requestInfo = req.url.split("/");
        switch (requestInfo[1]) {
            case ConstVariable.previewContent:
                if (!docFXPreviewProcessor.shouldRefreshPreviewPage) {
                    res.writeHead(304);
                    res.end();
                } else {
                    res.writeHead(200, { "Content-Type": "text/json" });
                    res.write(JSON.stringify(docFXPreviewProcessor.markupResult));
                    res.end();
                    docFXPreviewProcessor.shouldRefreshPreviewPage = false;
                }
                break;
            case ConstVariable.matchFromR2L:
                if (!mapToSelection(parseInt(requestInfo[2]), parseInt(requestInfo[3])))
                    window.showErrorMessage("Selection Range Error");
                res.end();
                break;
            case ConstVariable.matchFromL2R:
                res.writeHead(200, { "Content-Type": "text/plain" });
                res.write(startLine + " " + endLine);
                res.end();
        }
    });

    server.listen(0);
    server.on('listening', function () {
        docFXPreviewProcessor.navigationPort = server.address().port;
        ContentProvider.port = server.address().port;
    })
}

// This method is called when your extension is deactivated
export function deactivate() {
    PreviewProcessor.stopPreview();
}

function mapToSelection(startLineNumber: number, endLineNumber: number) {
    if (startLineNumber > endLineNumber)
        return false;
    // Go back to the Source file editor first
    if (startLineNumber === 0 && endLineNumber === 0) {
        // Click the node markdown
        commands.executeCommand("workbench.action.navigateBack").then(() => {
            endLineNumber = window.activeTextEditor.document.lineCount;
            window.activeTextEditor.selection = new Selection(0, 0, endLineNumber - 1, window.activeTextEditor.document.lineAt(endLineNumber - 1).range.end.character);
        });
    } else {
        commands.executeCommand("workbench.action.navigateBack").then(() => {
            window.activeTextEditor.selection = new Selection(startLineNumber - 1, 0, endLineNumber - 1, window.activeTextEditor.document.lineAt(endLineNumber - 1).range.end.character);
        });
    }
    return true;
}

// Check the file type
function isMarkdownFile(document: TextDocument) {
    // Prevent processing of own documents
    return document.languageId === "markdown" && document.uri.scheme !== "markdown";
}

function getMarkdownUri(uri: Uri) {
    return uri.with({ scheme: ConstVariable.markdownScheme, path: uri.fsPath + ".renderedDfm", query: uri.toString() });
}

function getTokenTreeUri(uri: Uri) {
    return uri.with({ scheme: ConstVariable.tokenTreeScheme, path: uri.fsPath + ".renderedTokenTree", query: uri.toString() });
}

function getViewColumn(sideBySide: boolean): ViewColumn {
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

function showPreview(dfmPreviewProcessor: DfmPreviewProcessor, uri?: Uri, sideBySide: boolean = false) {
    dfmPreviewProcessor.initialized = false;
    let resource = checkUri(uri)
    if (!resource) {
        return commands.executeCommand("DocFX.showSource");
    }

    PreviewProcessor.previewType = PreviewType.dfmPreview;

    let thenable = commands.executeCommand("vscode.previewHtml",
        getMarkdownUri(resource),
        getViewColumn(sideBySide),
        `DfmPreview "${path.basename(resource.fsPath)}"`);

    dfmPreviewProcessor.updateContent(getMarkdownUri(resource));
    return thenable;
}

function showDocFXPreview(docFXPreviewProcessor: DocFXPreviewProcessor, uri?: Uri, sideBySide: boolean = false) {
    PreviewProcessor.previewType = PreviewType.docFXPreview;

    // Restart dfmHttpService
    // For docfx-preview, we support config in docfx.json, which requires service restart
    PreviewProcessor.stopPreview();

    let resource = checkUri(uri)
    if (!resource) {
        return commands.executeCommand("DocFX.showSource");
    }

    docFXPreviewProcessor.startPreview(resource, function (tempPreviewFilePath) {
        let thenable = commands.executeCommand("vscode.previewHtml",
            tempPreviewFilePath,
            getViewColumn(sideBySide),
            `DocFXPreview "${path.basename(resource.fsPath)}"`);
    });
}

function showTokenTree(tokenTreeProcessor: TokenTreeProcessor, uri?: Uri) {
    tokenTreeProcessor.initialized = false;
    let resource = checkUri(uri)
    if (!resource) {
        return commands.executeCommand("DocFX.showSource");
    }

    PreviewProcessor.previewType = PreviewType.tokenTreePreview;

    let thenable = commands.executeCommand("vscode.previewHtml",
        getTokenTreeUri(resource),
        getViewColumn(true),
        `TokenTree '${path.basename(resource.fsPath)}'`);

    tokenTreeProcessor.updateContent(getTokenTreeUri(resource));
    return thenable;
}

function checkUri(uri): Uri {
    let resource = uri;
    if (!(resource instanceof Uri)) {
        if (window.activeTextEditor) {
            resource = window.activeTextEditor.document.uri;
        } else {
            // This is most likely toggling the preview
            return null;
        }
    }
    return resource;
}
