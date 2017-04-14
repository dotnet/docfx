// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExtensionContext, window } from "vscode";
import { TokenTreeContentProvider } from "./tokenTreeContentProvider";
import { ChildProcessHost } from "./childProcessHost";
import * as ConstVariable from "./constVariable";
import { DfmService } from "./dfmService";

export class TokenTreeCore extends ChildProcessHost {
    public provider: TokenTreeContentProvider;

    protected initializeProvider(context: ExtensionContext) {
        this.provider = new TokenTreeContentProvider(context);
    }

    protected async sendHttpRequestCore(rootPath: string, relativePath: string, docContent: string) {
        let that = this;
        try {
            let res = await DfmService.getTokenTreeAsync(ChildProcessHost._serverPort, docContent, rootPath, relativePath);
            that._childProcessStarting = false;
            that.provider.update(that._documentUri, res.data);
        }
        catch (err) {
            if (err.message == ConstVariable.noServiceErrorMessage) {
                if (!that._childProcessStarting) {
                    that.newHttpServerAndStartPreview(that._activeEditor);
                    that._childProcessStarting = true;
                }
            } else {
                window.showErrorMessage(`[Server Error]: ${err}`);
            }
        }
    }
}