// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Uri, ExtensionContext } from "vscode";
import { PreviewType } from "../constVariables/previewType";

export class ProxyRequest {
    documentUri: Uri;
    previewType: number;
    content: string;
    oldPid: number;
    relativePath: string;
    workspacePath: string;
    callback;

    constructor(documentUri: Uri, previewType: number, oldPid: number, content: string, relativePath: string, workspacePath: string, callback) {
        this.documentUri = documentUri;
        this.previewType = previewType;
        this.oldPid = oldPid;
        this.content = content;
        this.relativePath = relativePath;
        this.workspacePath = workspacePath;
        this.callback = callback;
    }

    public getKeyString(){
        return this.documentUri.toString();
    }
}
