// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExtensionContext, window } from "vscode";

import { PreviewProcesser } from "./previewProcesser";
import { ProxyResponse } from "../Proxy/proxyResponse";
import { TokenTreeContentProvider } from "../ContentProvider/tokenTreeContentProvider";

export class TokenTreeProcesser extends PreviewProcesser {
    provider: TokenTreeContentProvider;

    constructor(context: ExtensionContext) {
        super();
        this.provider = new TokenTreeContentProvider(context);
    }

    protected pageRefresh(response: ProxyResponse){
        this.provider.update(response.documentUri, response.markupResult);
    }
}
