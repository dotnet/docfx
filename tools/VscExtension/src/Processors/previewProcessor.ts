// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExtensionContext, Uri, window } from "vscode";

import { PreviewType } from "../constVariables/previewType";
import { requestProxy } from "../Proxy/requestProxy";
import { ProxyResponse } from "../Proxy/proxyResponse";
import { ProxyRequest } from "../Proxy/proxyRequest";
import { Utility } from "../common/utility";

export class PreviewProcessor {
    public static previewType = PreviewType.dfmPreview;
    public initialized;

    protected static context: ExtensionContext;
    protected static proxy = requestProxy.getInstance();

    private _waiting = false;

    constructor(context: ExtensionContext) {
        PreviewProcessor.context = context;

        let environmentVariables = Utility.getEnvironmentVariables();
        if (environmentVariables == null) {
            return;
        }

        PreviewProcessor.proxy.setWorkspacePath(environmentVariables.workspacePath);
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
        let environmentVariables = Utility.getEnvironmentVariables();
        if (environmentVariables == null) {
            return;
        }

        let request = new ProxyRequest(documentUri, previewType, environmentVariables.docContent, environmentVariables.relativePath, context, callback);
        return this.appendTempPreviewFileInformation(request);
    }

    protected appendTempPreviewFileInformation(request) {
        return request;
    }

    protected pageRefresh(response: ProxyResponse) {
        window.showErrorMessage(`[Extension Error]: Not supported`);
    }
}
