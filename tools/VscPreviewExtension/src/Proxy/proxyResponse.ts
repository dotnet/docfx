// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Uri } from "vscode";

export class ProxyResponse{
    markupResult: string;
    fileName: string;
    documentUri: Uri;

    constructor(markupResult:string, fileName: string, documentUri){
        this.markupResult = markupResult;
        this.fileName = fileName;
        this.documentUri = documentUri;
    }
}
