// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Uri, window } from "vscode";

import { PreviewType } from "../constVariables/previewType";
import { Proxy } from "../Proxy/proxy";
import { ProxyResponse } from "../Proxy/proxyResponse";

export class PreviewProcessor {
    public static previewType = PreviewType.dfmPreview;
    public initialized;

    private static proxy = Proxy.getInstance();
    private _waiting = false;

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
        PreviewProcessor.proxy.newRequest(uri, PreviewProcessor.previewType, function (err,response) {
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
