// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { workspace, window, ExtensionContext, Uri } from "vscode";
import * as childProcess from "child_process";
import { MarkdownDocumentContentProvider } from "./markdownDocumentContentProvider";

// Create a child process(DfmRender) by "_spawn" to render a html
export class PreviewCore {
    // TODO: extract the common pattern form this class with TokenTreeCore
    public isFirstTime: boolean;
    public provider: MarkdownDocumentContentProvider;

    private _spawn: childProcess.ChildProcess;
    private _waiting: boolean;
    private _previewContent: string;
    private _isMultipleRead = false;
    private _documentUri: Uri;
    private ENDCODE = 7; // '\a'

    constructor(context: ExtensionContext) {
        // TODO: make path configurable
        let extpath = context.asAbsolutePath("./DfmParse/PreviewCore.exe");
        this._spawn = childProcess.spawn(extpath);
        if (!this._spawn.pid) {
            window.showErrorMessage("Error:DfmProcess lost!");
            return;
        }
        this._waiting = false;
        this.provider = new MarkdownDocumentContentProvider(context);
        let that = this;

        this._spawn.stdout.on("data", function (data) {
            // The output of child process will be cut if it is too long
            let dfmResult = data.toString();
            if (dfmResult.length !== 0) {
                let endCharCode = dfmResult.charCodeAt(dfmResult.length - 1);
                if (that._isMultipleRead) {
                    that._previewContent += dfmResult;
                } else {
                    that._previewContent = dfmResult;
                }
                that._isMultipleRead = !(endCharCode === that.ENDCODE);
                if (!that._isMultipleRead) {
                    that.provider.update(that._documentUri, that._previewContent);
                }
            }
        });

        this._spawn.stderr.on("data", function (data) {
            window.showErrorMessage("Error:" + data + "\n");
        });

        this._spawn.on("exit", function (code) {
            window.showErrorMessage("Child process exit with code " + code);
        });
    }

    private sendText() {
        let editor = window.activeTextEditor;
        if (!editor) {
            return;
        }

        let doc = editor.document;
        let docContent = doc.getText();
        let fileName = doc.fileName;
        let rootPath = workspace.rootPath;
        let filePath;
        if (!rootPath) {
            let indexOfFilename = fileName.lastIndexOf("\\");
            rootPath = fileName.substr(0, indexOfFilename);
            filePath = fileName.substring(indexOfFilename + 1);
        } else {
            let rootPathLength = rootPath.length;
            filePath = fileName.substr(rootPathLength + 1, fileName.length - rootPathLength);
        }
        if (doc.languageId === "markdown") {
            let numOfRow = doc.lineCount;
            this._spawn.stdin.write(this.appendWrap(rootPath));
            this._spawn.stdin.write(this.appendWrap(filePath));
            this._spawn.stdin.write(this.appendWrap(numOfRow));
            this._spawn.stdin.write(this.appendWrap(docContent));
        }
    }

    private appendWrap(content) {
        return content + "\n";
    }

    public callDfm(uri: Uri) {
        this._documentUri = uri;
        if (this.isFirstTime) {
            // In the first time, if wait for the timeout, activeTextEditor will be the preview window.
            this.isFirstTime = false;
            this.sendText();
        } else if (!this._waiting) {
            this._waiting = true;
            setTimeout(() => {
                this._waiting = false;
                this.sendText();
            }, 300);
        }
    }
}