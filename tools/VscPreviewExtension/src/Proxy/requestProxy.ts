// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExtensionContext, Uri, window, workspace } from "vscode";
import * as childProcess from "child_process";

import * as ConstVariable from "../constVariables/commonVariables";
import { Common } from "../common";
import { DfmService } from "../dfmService";
import { PreviewType } from "../constVariables/previewType";
import { ProxyRequest } from "./proxyRequest";
import { ProxyResponse } from "./proxyResponse";
import { RequestArray } from "./RequestArray";

export class requestProxy {
    private static _instance: requestProxy = new requestProxy();

    private _isChildProcessStarting: boolean = false;
    private _requestArray = new RequestArray();
    private _spawn: childProcess.ChildProcess;
    private _serverPort = "4002";

    public static getInstance(): requestProxy {
        return requestProxy._instance;
    }

    public newRequest(documentUri: Uri, previewType: number, context:ExtensionContext, callback) {
        let request = this.prepareRequestData(documentUri, previewType, context, callback);
        this._requestArray.push(request);
        this.requestProcess();
    }

    public async stopProxy() {
        try {
            await DfmService.exitAsync(this._serverPort);
        } catch (err) {
            window.showErrorMessage(`[Server Error]: ${err}`);
        }
    }

    private prepareRequestData(documentUri: Uri, previewType: number, context, callback): ProxyRequest {
        let editor = window.activeTextEditor;
        if (!editor) {
            window.showErrorMessage(`[Extension Error]: No ActiveEditor`);
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

        return new ProxyRequest(documentUri, previewType, this._spawn ? this._spawn.pid : 0, docContent, relativePath, rootPath, context, callback);
    }

    private requestProcess() {
        let request;
        while ((request = this._requestArray.pop()) != null) {
            if (this._isChildProcessStarting) {
                this._requestArray.push(request);
                return;
            } else {
                this.requestProcessCore(request);
            }
        }
    }

    private async requestProcessCore(request: ProxyRequest) {
        try {
            let res;
            switch (request.previewType) {
                case PreviewType.dfmPreview:
                    res = await DfmService.previewAsync(this._serverPort, request.content, request.workspacePath, request.relativePath);
                    break;
                case PreviewType.tokenTreePreview:
                    res = await DfmService.getTokenTreeAsync(this._serverPort, request.content, request.workspacePath, request.relativePath);
                    break;
            }
            request.callback(null, new ProxyResponse(res ? res.data : "", request.relativePath, request.documentUri));
        } catch (err) {
            if (err.message == ConstVariable.noServiceErrorMessage) {
                this._requestArray.push(request);
                let currentPid = this._spawn ? this._spawn.pid : 0;
                if (currentPid === request.oldPid) {
                    this.newHttpServerAndStartPreview(request.context);
                } else {
                    this.requestProcess();
                }
            } else {
                request.callback(err);
            }
        }
    }

    private newHttpServerAndStartPreview(context: ExtensionContext) {
        if (this._isChildProcessStarting)
            return;
        this._isChildProcessStarting = true;
        window.showInformationMessage("Environment initializing, please wait for several seconds!");
        this.getFreePort(port => this.newHttpServerAndStartPreviewCore(port, context));
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

    private newHttpServerAndStartPreviewCore(port, context: ExtensionContext) {
        let that = this;
        this._serverPort = port.toString();
        let exePath = context.asAbsolutePath("./DfmHttpService/DfmHttpService.exe");
        try {
            this._spawn = Common.spawn(exePath + " " + this._serverPort, {});
        }
        catch (err) {
            window.showErrorMessage(`[Extension Error]: ${err}`);
        }
        if (!this._spawn.pid) {
            window.showErrorMessage(`[Child process Error]: DfmProcess lost!`);
            return;
        }
        this._spawn.stdout.on("data", function (data) {
            that._isChildProcessStarting = false;
            that.requestProcess();
        });
        this._spawn.stderr.on("data", function (data) {
            window.showErrorMessage(`[Child process Error]: ${data.toString()}`);
        });
    }
}
