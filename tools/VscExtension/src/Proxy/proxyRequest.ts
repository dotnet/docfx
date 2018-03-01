// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Uri, ExtensionContext } from "vscode";
import { PreviewType } from "../constVariables/previewType";
import { TempPreviewFileInformation } from "../Common/tempPreviewFileInformation";

export class ProxyRequest {
    documentUri: Uri;
    previewType: number;
    content: string;
    oldPid: number;
    relativePath: string;
    tempPreviewFilePath: string;
    originalHtmlPath: string;
    pageRefreshJsFilePath: string;
    navigationPort: string;
    context: ExtensionContext;
    callback;

    constructor(documentUri: Uri, previewType: number, content: string, relativePath: string, context: ExtensionContext, callback) {
        this.documentUri = documentUri;
        this.previewType = previewType;
        this.content = content;
        this.relativePath = relativePath;
        this.context = context;
        this.callback = callback;
    }

    public getKeyString() {
        return this.documentUri.toString();
    }

    public storageChildProcessPid(pid: number) {
        this.oldPid = pid;
    }

    public appendTempPreviewFileInformation(tempPreviewFileInformation: TempPreviewFileInformation) {
        this.tempPreviewFilePath = tempPreviewFileInformation.tempPreviewFilePath;
        this.originalHtmlPath = tempPreviewFileInformation.originalHtmlPath;
        this.pageRefreshJsFilePath = tempPreviewFileInformation.pageRefreshJsFilePath;
        this.navigationPort = tempPreviewFileInformation.navigationPort;
    }
}
