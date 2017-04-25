// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExtensionContext, window } from "vscode";

import { MarkdownDocumentContentProvider } from "../ContentProvider/markdownDocumentContentProvider";
import { PreviewProcessor } from "./previewProcessor";
import { ProxyResponse } from "../Proxy/proxyResponse";

export class DfmPreviewProcessor extends PreviewProcessor {
    provider: MarkdownDocumentContentProvider;

    constructor(context: ExtensionContext) {
        super(context);
        this.provider = new MarkdownDocumentContentProvider(context);
    }

    protected pageRefresh(response: ProxyResponse){
        this.provider.fileName = response.fileName;
        this.provider.update(response.documentUri, response.markupResult);
    }
}
