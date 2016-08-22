// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

"use strict";

import { workspace, window, ExtensionContext, commands, Event, Uri, ViewColumn, TextDocument } from "vscode";
import * as path from "path";
import { PreviewCore } from "./PreviewCore";

const MARKDOWNSCHEME = "markdown";

export function activate(context: ExtensionContext) {
    let dfmProcess = new PreviewCore(context);
    let providerRegistration = workspace.registerTextDocumentContentProvider("markdown", dfmProcess._provider);

    // Event register
    let showPreviewRegistration = commands.registerCommand("DFM.showPreview", uri => showPreview(dfmProcess));
    let showPreviewToSideRegistration = commands.registerCommand("DFM.showPreviewToSide", uri => showPreview(dfmProcess, uri, true));
    let showSourceRegistration = commands.registerCommand("DFM.showSource", showSource);

    context.subscriptions.push(showPreviewRegistration, showPreviewToSideRegistration, showSourceRegistration, providerRegistration);

    workspace.onDidSaveTextDocument(document => {
        if (isMarkdownFile(document)) {
            const uri = getMarkdownUri(document.uri);
            dfmProcess.callDfm(uri);
        }
    });

    workspace.onDidChangeTextDocument(event => {
        if (isMarkdownFile(event.document)) {
            const uri = getMarkdownUri(event.document.uri);
            dfmProcess.callDfm(uri);
        }
    });

    workspace.onDidChangeConfiguration(() => {
        workspace.textDocuments.forEach(document => {
            if (document.uri.scheme === MARKDOWNSCHEME) {
                dfmProcess.callDfm(document.uri);
            }
        });
    });
}

// Check the file type
function isMarkdownFile(document: TextDocument) {
    // Prevent processing of own documents
    return document.languageId === "markdown" && document.uri.scheme === "file";
}

function getMarkdownUri(uri: Uri) {
    return uri.with({ scheme: MARKDOWNSCHEME, path: uri.path + ".renderedDfm", query: uri.toString() });
}

function showPreview(dfmPreview: PreviewCore, uri?: Uri, sideBySide: boolean = false) {
    if (window.activeTextEditor.document.languageId !== "markdown") {
        window.showErrorMessage("This is not a markdown file!");
        return;
    }
    dfmPreview._isFirstTime = true;
    let resource = uri;
    if (!(resource instanceof Uri)) {
        if (window.activeTextEditor) {
            resource = window.activeTextEditor.document.uri;
        }
    }

    if (!(resource instanceof Uri)) {
        if (!window.activeTextEditor) {
            // This is most likely toggling the preview
            return commands.executeCommand("markdown.showSource");
        }
        // Nothing found that could be shown or toggled
        return;
    }

    let thenable = commands.executeCommand("vscode.previewHtml",
        getMarkdownUri(resource),
        getViewColumn(sideBySide),
        `Preview "${path.basename(resource.fsPath)}"`);

    dfmPreview.callDfm(getMarkdownUri(resource));
    return thenable;
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

function showSource(mdUri: Uri) {
    if (!mdUri) {
        return commands.executeCommand("workbench.action.navigateBack");
    }

    const docUri = Uri.parse(mdUri.query);

    for (let editor of window.visibleTextEditors) {
        if (editor.document.uri.toString() === docUri.toString()) {
            return window.showTextDocument(editor.document, editor.viewColumn);
        }
    }

    return workspace.openTextDocument(docUri).then(doc => {
        return window.showTextDocument(doc);
    });
}