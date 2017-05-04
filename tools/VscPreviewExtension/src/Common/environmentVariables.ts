// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

export class EnvironmentVariables {
    workspacePath: string;
    relativePath: string;
    docContent: string;

    constructor(workspacePath, relativePath, docContent) {
        this.workspacePath = workspacePath;
        this.relativePath = relativePath;
        this.docContent = docContent;
    }
}