// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { workspace, window, ExtensionContext, Uri } from "vscode";
import * as childProcess from "child_process";
import * as fs from "fs";

import { ContentProvider } from "./contentProvider";
import * as ConstVariable from "./constVariable";
import { Common } from "./common";

export class ChildProcessHost {
    public provider: ContentProvider;

    protected _spawn: childProcess.ChildProcess;
    protected _waiting: boolean;
    protected _documentUri: Uri;

    private _content: string;
    private _isMultipleRead = false;
    private ENDCODE = 7; // '\a'

    constructor(context: ExtensionContext) {
        // TODO: make path configurable
        let exePath = context.asAbsolutePath("./DfmHttpService/DfmHttpService.exe");
        this._spawn = Common.spawn(exePath, {});
        if (!this._spawn.pid) {
            window.showErrorMessage("Error: DfmProcess lost!");
            return;
        }
        this._waiting = false;
        this.initializeProvider(context);
        let that = this;

        this._spawn.stdout.on("data", function (data) {
            // The output of child process will be cut if it is too long
            let dfmResult = data.toString();
            if (dfmResult.length > 0) {
                let endCharCode = dfmResult.charCodeAt(dfmResult.length - 1);
                if (that._isMultipleRead) {
                    that._content += dfmResult;
                } else {
                    that._content = dfmResult;
                }
                that._isMultipleRead = endCharCode !== that.ENDCODE;
                if (!that._isMultipleRead) {
                    that.provider.update(that._documentUri, that._content);
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

    public callDfm(uri: Uri) {
        this._documentUri = uri;
        this.sendMessage();
    }

    protected initializeProvider(context: ExtensionContext) { }

    protected sendMessage() {
        let editor = window.activeTextEditor;
        if (!editor) {
            return;
        }

        let doc = editor.document;
        let docContent = doc.getText();
        let fileName = doc.fileName;
        let rootPath = workspace.rootPath;
        let filePath;
        if (!rootPath || !fileName.includes(rootPath)) {
            let indexOfFilename = fileName.lastIndexOf("\\");
            rootPath = fileName.substr(0, indexOfFilename);
            filePath = fileName.substring(indexOfFilename + 1);
        } else {
            let rootPathLength = rootPath.length;
            filePath = fileName.substr(rootPathLength + 1, fileName.length - rootPathLength);
        }
        if (doc.languageId === ConstVariable.languageId) {
            let numOfRow = doc.lineCount;
            this.writeToStdin(rootPath, filePath, numOfRow, docContent);
        }
    }

    protected writeToStdin(rootPath: string, filePath: string, numOfRow: number, docContent: string) { }

    protected appendWrap(content) {
        return content + "\n";
    }
}