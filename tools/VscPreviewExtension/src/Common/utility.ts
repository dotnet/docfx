// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { workspace, window } from "vscode";

import { EnvironmentVariables } from "./environmentVariables";

export class Utility{
    static getEnvironmentVariables(): EnvironmentVariables{
        let editor = window.activeTextEditor;
        if (!editor) {
            throw new Error("No active editor");
        }
        let doc = editor.document;
        let docContent = doc.getText();
        let fileName = doc.fileName;
        let workspacePath = workspace.rootPath;
        let relativePath;
        if (!workspacePath || !fileName.includes(workspacePath)) {
            let indexOfFileName = fileName.lastIndexOf("\\");
            workspacePath = fileName.substr(0, indexOfFileName);
            relativePath = fileName.substring(indexOfFileName + 1);
        } else {
            let rootPathLength = workspacePath.length;
            relativePath = fileName.substr(rootPathLength + 1, fileName.length - rootPathLength);
        }
        return new EnvironmentVariables(workspacePath, relativePath, docContent);
    }
}