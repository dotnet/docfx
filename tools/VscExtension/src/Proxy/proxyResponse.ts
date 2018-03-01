// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Uri } from "vscode";

export class ProxyResponse{
    markupResult;
    fileName: string;
    documentUri: Uri;

    constructor(markupResult, fileName: string, documentUri: Uri){
        this.markupResult = markupResult;
        this.fileName = fileName;
        this.documentUri = documentUri;
    }
}
