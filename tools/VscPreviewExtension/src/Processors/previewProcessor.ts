// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExtensionContext, Uri, window, workspace } from "vscode";

import { PreviewType } from "../constVariables/previewType";
import { requestProxy } from "../Proxy/requestProxy";
import { ProxyResponse } from "../Proxy/proxyResponse";
import { ProxyRequest } from "../Proxy/proxyRequest";

export class PreviewProcessor {
    public static previewType = PreviewType.dfmPreview;
    public initialized;

    protected static context: ExtensionContext;

    private static proxy = requestProxy.getInstance();
    private _waiting = false;

    constructor(context: ExtensionContext) {
        PreviewProcessor.context = context;
    }

    public static stopPreview() {
        this.proxy.stopProxy();
    }

    public updateContent(uri: Uri) {
        if (!this.initialized) {
            // In the first time, if wait for the timeout, activeTextEditor will be the preview window.
            this.initialized = true;
            this.updateContentCoreAsync(uri);
        } else if (!this._waiting) {
            this._waiting = true;
            setTimeout(() => {
                this._waiting = false;
                this.updateContentCoreAsync(uri);
            }, 300);
        }
    }

    private updateContentCoreAsync(uri: Uri) {
        let that = this;
        PreviewProcessor.proxy.newRequest(this.prepareRequestData(uri, PreviewProcessor.previewType, PreviewProcessor.context, function (err, response) {
            if (err) {
                window.showErrorMessage(`[Proxy Error]: ${err}`);
            } else {
                that.pageRefresh(response);
            }
        }));
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

        let request = new ProxyRequest(documentUri, previewType, docContent, relativePath, rootPath, context, callback);
        return this.appendTempPreviewFileInformation(request);
    }

    protected appendTempPreviewFileInformation(request) {
        return request;
    }

    protected pageRefresh(response: ProxyResponse) {
        window.showErrorMessage(`[Extension Error]: Not supported`);
    }
}
