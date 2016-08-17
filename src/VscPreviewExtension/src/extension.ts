// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

'use strict';

import {workspace, window, ExtensionContext, commands, TextDocumentContentProvider, EventEmitter, Event, Uri, ViewColumn, TextDocument }from "vscode";
import * as path from "path";
import * as child_process from 'child_process';

let previewResult = "";
let provider;
let documentUri;
let multipleRead = false;
const ENDCODE = 7;// '\a'

export function activate(context: ExtensionContext) {
    let dfmProcess = new PreviewCore(context);
    provider = new MDDocumentContentProvider(context);
    let providerRegistration = workspace.registerTextDocumentContentProvider('markdown', provider);

    // Event register
    let showPreviewRegistration = commands.registerCommand('DFM.showPreview', uri => showPreview(dfmProcess));
    let showPreviewToSideRegistration = commands.registerCommand('DFM.showPreviewToSide', uri => showPreview(dfmProcess, uri, true));
    let showSourceRegistration = commands.registerCommand('DFM.showSource', showSource);

    context.subscriptions.push(showPreviewRegistration, showPreviewToSideRegistration, showSourceRegistration, providerRegistration);

    workspace.onDidSaveTextDocument(document => {
        if (isMarkdownFile(document)) {
            documentUri = getMarkdownUri(document.uri);
            dfmProcess.callDfm();
        }
    });

    workspace.onDidChangeTextDocument(event => {
        if (isMarkdownFile(event.document)) {
            documentUri = getMarkdownUri(event.document.uri);
            dfmProcess.callDfm();
        }
    });

    workspace.onDidChangeConfiguration(() => {
        workspace.textDocuments.forEach(document => {
            if (document.uri.scheme === 'markdown') {
                documentUri = documentUri;
                dfmProcess.callDfm();
            }
        });
    });
}

// Check the file type
function isMarkdownFile(document: TextDocument) {
    // Prevent processing of own documents
    return document.languageId === 'markdown' && document.uri.scheme !== 'markdown';
}

function getMarkdownUri(uri: Uri) {
    return uri.with({ scheme: 'markdown', path: uri.path + '.rendered', query: uri.toString() });
}

function showPreview(dfmPreview: PreviewCore, uri?: Uri, sideBySide: boolean = false) {
    if (window.activeTextEditor.document.languageId !== 'markdown') {
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
            return commands.executeCommand('markdown.showSource');
        }
        // Nothing found that could be shown or toggled
        return;
    }

    let thenable = commands.executeCommand('vscode.previewHtml',
        getMarkdownUri(resource),
        getViewColumn(sideBySide),
        `Preview '${path.basename(resource.fsPath)}'`);

    documentUri = getMarkdownUri(resource);
    dfmPreview.callDfm();
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
        return commands.executeCommand('workbench.action.navigateBack');
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

// Create a child process(DfmRender) by '_spawn' to render a html
class PreviewCore {
    private _spawn: child_process.ChildProcess;
    private _waiting: boolean;
    public _isFirstTime: boolean;

    constructor(context: ExtensionContext) {
        let extpath = context.asAbsolutePath('./DfmParse/PreviewCore.exe');
        this._spawn = child_process.spawn(extpath);
        this._waiting = false;

        this._spawn.stdout.on('data', function (data) {
            let tmp = data.toString();
            let endCharCode = tmp.charCodeAt(tmp.length - 1);
            if (!multipleRead) {
                if (endCharCode == ENDCODE) {
                    previewResult = tmp;
                    provider.update(documentUri);
                }
                else {
                    // The first one and the result is truncated
                    previewResult = tmp;
                    multipleRead = true;
                }
            }
            else {
                if (endCharCode != ENDCODE) {
                    // The result is truncated and this is not the last one
                    previewResult += tmp;
                }
                else {
                    // The result is truncated and this is the last one
                    previewResult += tmp;
                    multipleRead = false;
                    provider.update(documentUri);
                }
            }
        });

        this._spawn.stderr.on('data', function (data) {
            console.log("error " + data + '\n');
        });

        this._spawn.on('exit', function (code) {
            console.log('child process exit with code ' + code);
        });
    }

    private sendtext() {
        let editor = window.activeTextEditor;
        if (!editor) {
            return;
        }
        let doc = editor.document;
        let docContent = doc.getText();
        let fileName = doc.fileName;
        let rtPath = workspace.rootPath;
        let filePath;
        if (!rtPath) {
            let indexOfFilename = fileName.lastIndexOf('\\');
            rtPath = fileName.substr(indexOfFilename - 1);
            filePath = fileName.substring(0, indexOfFilename);
        }
        else {
            let rtpath_length = rtPath.length;
            filePath = fileName.substr(rtpath_length + 1, fileName.length - rtpath_length);
        }
        if (doc.languageId === "markdown") {
            let numOfRow = doc.lineCount;
            this._spawn.stdin.write(rtPath + '\n');
            this._spawn.stdin.write(filePath + '\n');
            this._spawn.stdin.write(numOfRow + '\n');
            this._spawn.stdin.write(docContent + '\n');
        }
    }

    public callDfm() {
        if (this._isFirstTime) {
            // In the firt time, if wait for the timeout, activeTextEditor will be translate to the preview window.
            this._isFirstTime = false;
            this.sendtext();
        }
        else if (!this._waiting) {
            this._waiting = true;
            setTimeout(() => {
                this._waiting = false;
                this.sendtext();
            }, 300);
        }
    }
}

class MDDocumentContentProvider implements TextDocumentContentProvider {
    private _context: ExtensionContext;
    private _onDidChange = new EventEmitter<Uri>();
    private _waiting: boolean;

    constructor(context: ExtensionContext) {
        this._context = context;
        this._waiting = false;
    }

    private getMediaPath(mediaFile): string {
        return this._context.asAbsolutePath(path.join('media', mediaFile));
    }

    public provideTextDocumentContent(uri: Uri): Thenable<string> {
        return workspace.openTextDocument(Uri.parse(uri.query)).then(document => {
            const head = [
                '<!DOCTYPE html>',
                '<html>',
                '<head>',
                '<meta http-equiv="Content-type" content="text/html;charset=UTF-8">',
                `<link rel="stylesheet" type="text/css" href="${this.getMediaPath('tomorrow.css')}" >`,
                `<link rel="stylesheet" type="text/css" href="${this.getMediaPath('markdown.css')}" >`,
                `<base href="${document.uri.toString(true)}">`,
                '</head>',
                '<body>'].join('\n');

            const body = previewResult;

            const tail = [
                `<script type="text/javascript" src="${this.getMediaPath('docfx.vendor.js')}"></script>`,
                `<script>hljs.initHighlightingOnLoad();</script>`,
                '</body>',
                '</html>'
            ].join('\n');

            return head + body + tail;
        });
    }

    get onDidChange(): Event<Uri> {
        return this._onDidChange.event;
    }

    public update(uri: Uri) {
        this._onDidChange.fire(uri);
    }
}