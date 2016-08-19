// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { workspace, window, ExtensionContext, Uri }from "vscode";
import * as child_process from "child_process";
import { MDDocumentContentProvider } from "./MDDocumentContentProvider";

// Create a child process(DfmRender) by "_spawn" to render a html
export class PreviewCore {
    public _isFirstTime: boolean;
    public _provider: MDDocumentContentProvider;

    private _spawn: child_process.ChildProcess;
    private _waiting: boolean;
    private _previewContent: string;
    private _isMultipleRead = false;
    private _documentUri: Uri;
    private ENDCODE = 7; // '\a'

    constructor(context: ExtensionContext) {
        let extpath = context.asAbsolutePath("./DfmParse/PreviewCore.exe");
        this._spawn = child_process.spawn(extpath);
        if (!this._spawn.pid) {
            window.showErrorMessage("Error:DfmProcess lost!");
            return;
        }
        this._waiting = false;
        this._provider = new MDDocumentContentProvider(context);
        let that = this;

        this._spawn.stdout.on("data", function (data) {
            // The output of child process will be cut if it is too long
            let dfmResult = data.toString();
            if (dfmResult.length !== 0) {
                let endCharCode = dfmResult.charCodeAt(dfmResult.length - 1);
                if (!that._isMultipleRead) {
                    that._previewContent = dfmResult;
                    if (endCharCode === that.ENDCODE) {
                        that._provider.update(that._documentUri, that._previewContent);
                    } else {
                        // The first one and the result is truncated
                        that._isMultipleRead = true;
                    }
                } else {
                    that._previewContent += dfmResult;
                    if (endCharCode === that.ENDCODE) {
                        // The result is truncated and this is the last one
                        that._previewContent += dfmResult;
                        that._isMultipleRead = false;
                        that._provider.update(that._documentUri, that._previewContent);
                    }
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

    private sendtext() {
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
            rootPath = fileName.substr(indexOfFilename - 1);
            filePath = fileName.substring(0, indexOfFilename);
        } else {
            let rtpath_length = rootPath.length;
            filePath = fileName.substr(rtpath_length + 1, fileName.length - rtpath_length);
        }
        if (doc.languageId === "markdown") {
            let numOfRow = doc.lineCount;
            // I am not sure which will be better?
            this._spawn.stdin.write([rootPath, filePath, numOfRow, docContent].join("\n"));
            this._spawn.stdin.write("\n");
            /*this._spawn.stdin.write(rootPath + "\n");
            this._spawn.stdin.write(filePath + "\n");
            this._spawn.stdin.write(numOfRow + "\n");
            this._spawn.stdin.write(docContent + "\n");*/
        }
    }

    public callDfm(uri: Uri) {
        this._documentUri = uri;
        if (this._isFirstTime) {
            // In the firt time, if wait for the timeout, activeTextEditor will be translate to the preview window.
            this._isFirstTime = false;
            this.sendtext();
        } else if (!this._waiting) {
            this._waiting = true;
            setTimeout(() => {
                this._waiting = false;
                this.sendtext();
            }, 300);
        }
    }
}