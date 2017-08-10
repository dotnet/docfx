// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

export class TempPreviewFileInformation {
    originalHtmlPath: string;
    tempPreviewFilePath: string;
    pageRefreshJsFilePath: string;
    navigationPort: string;

    constructor(originalHtmlPath: string, tempPreviewFilePath: string, pageRefreshJsFilePath: string, navigationPort) {
        this.originalHtmlPath = originalHtmlPath;
        this.tempPreviewFilePath = tempPreviewFilePath;
        this.pageRefreshJsFilePath = pageRefreshJsFilePath;
        this.navigationPort = navigationPort;
    }
}