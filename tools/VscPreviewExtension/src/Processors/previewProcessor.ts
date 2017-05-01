// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Uri, window, ExtensionContext } from "vscode";

import { PreviewType } from "../constVariables/previewType";
import { requestProxy } from "../Proxy/requestProxy";
import { ProxyResponse } from "../Proxy/proxyResponse";

export class PreviewProcessor {
    public static previewType = PreviewType.dfmPreview;
    public initialized;

    private static _context: ExtensionContext;
    private static proxy = requestProxy.getInstance();
    private _waiting = false;

    constructor(context: ExtensionContext){
        PreviewProcessor._context = context;
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
        PreviewProcessor.proxy.newRequest(uri, PreviewProcessor.previewType, PreviewProcessor._context, function (err,response) {
            if (err) {
                window.showErrorMessage(`[Proxy Error]: ${err}`);
            } else {
                that.pageRefresh(response);
            }
        });
    }

    protected pageRefresh(response: ProxyResponse) {
        window.showErrorMessage(`[Extension Error]: Not supported`);
    }
}
