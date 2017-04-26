// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExtensionContext, window } from "vscode";

import { PreviewProcessor } from "./previewProcessor";
import { ProxyResponse } from "../Proxy/proxyResponse";
import { TokenTreeContentProvider } from "../ContentProvider/tokenTreeContentProvider";

export class TokenTreeProcessor extends PreviewProcessor {
    provider: TokenTreeContentProvider;

    constructor(context: ExtensionContext) {
        super(context);
        this.provider = new TokenTreeContentProvider(context);
    }

    protected pageRefresh(response: ProxyResponse){
        this.provider.update(response.documentUri, response.markupResult);
    }
}
