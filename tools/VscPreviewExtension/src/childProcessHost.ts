// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { workspace, window, ExtensionContext, Uri } from "vscode";
import * as childProcess from "child_process";

import { Common } from "./common";
import * as ConstVariable from "./constVariables/commonVariables";
import { DfmService } from "./dfmService";
import { PreviewType } from "./constVariables/previewType";

export class ChildProcessHost {
    public static previewType = PreviewType.dfmPreview;
    public initialized;

    protected static _serverPort = "4002";
    protected _isChildProcessStarting = false;
    protected _activeEditor;
    protected _documentUri: Uri;

    private static _spawn: childProcess.ChildProcess;
    private _waiting = false;
    private _context: ExtensionContext;

    constructor(context: ExtensionContext) {
        this._context = context;
        this.initializeProvider(context);
    }

    public static async killChildProcessAsync() {
        try {
            await DfmService.exitAsync(ChildProcessHost._serverPort);
        } catch (err) {
            window.showErrorMessage(`[Server Error]: ${err}`);
        }
    }

    public updateContent(uri: Uri) {
        if (!this.initialized) {
            // In the first time, if wait for the timeout, activeTextEditor will be the preview window.
            this.initialized = true;
            this.updateContentCore(uri);
        } else if (!this._waiting) {
            this._waiting = true;
            setTimeout(() => {
                this._waiting = false;
                this.updateContentCore(uri);
            }, 300);
        }
    }

    private updateContentCore(uri: Uri) {
        this._documentUri = uri;
        this._activeEditor = window.activeTextEditor;
        this.sendHttpRequest(this._activeEditor);
    }

    protected initializeProvider(context: ExtensionContext) { }

    protected sendHttpRequest(editor) {
        if (!editor) {
            return;
        }

        let doc = editor.document;
        let docContent = doc.getText();
        let fileName = doc.fileName;
        let rootPath = workspace.rootPath;
        let relativePath;
        if (!rootPath || !fileName.includes(rootPath)) {
            let indexOfFileName = fileName.lastIndexOf("\\");
            rootPath = fileName.substr(0, indexOfFileName);
            relativePath = fileName.substring(indexOfFileName + 1);
        } else {
            let rootPathLength = rootPath.length;
            relativePath = fileName.substr(rootPathLength + 1, fileName.length - rootPathLength);
        }
        if (doc.languageId === "markdown") {
            this.sendHttpRequestCoreAsync(rootPath, relativePath, docContent);
        }
    }

    protected async sendHttpRequestCoreAsync(rootPath: string, relativePath: string, docContent: string) {
        window.showErrorMessage(`[Extension Error]: Not supported`);
    }

    protected newHttpServerAndStartPreview(activeTextEditor) {
        if (this._isChildProcessStarting)
            return;
        this._isChildProcessStarting = true;
        window.showInformationMessage("Environment initializing, please wait several seconds!");
        this.getFreePort(port => this.newHttpServerAndStartPreviewCore(port, activeTextEditor));
    }

    private getFreePort(callback) {
        let http = require("http");
        let server = http.createServer();
        server.listen(0);
        server.on('listening', function () {
            var port = server.address().port;
            server.close();
            callback(port);
        })
    }

    private newHttpServerAndStartPreviewCore(port, activeTextEditor) {
        let that = this;
        ChildProcessHost._serverPort = port.toString();
        let exePath = that._context.asAbsolutePath("./DfmHttpService/DfmHttpService.exe");
        try {
            ChildProcessHost._spawn = Common.spawn(exePath + " " + ChildProcessHost._serverPort, {});
        }
        catch (err) {
            window.showErrorMessage(`[Extension Error]: ${err}`);
        }
        if (!ChildProcessHost._spawn.pid) {
            window.showErrorMessage(`[Child process Error]: DfmProcess lost!`);
            return;
        }
        ChildProcessHost._spawn.stdout.on("data", function (data) {
            that.sendHttpRequest(activeTextEditor);
        });
        ChildProcessHost._spawn.stderr.on("data", function (data) {
            window.showErrorMessage(`[Child process Error]: ${data.toString()}`);
        });
    }
}
