// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { workspace, window } from "vscode";
import * as path from "path";

import { EnvironmentVariables } from "./environmentVariables";

export class Utility{
    static getEnvironmentVariables(): EnvironmentVariables{
        let editor = window.activeTextEditor;
        if (!editor) {
            window.showErrorMessage(`[Extension Error]: "No active editor"`);
            return null;
        }
        let doc = editor.document;
        let docContent = doc.getText();
        let fileName = doc.fileName;
        let workspacePath = workspace.rootPath;
        let relativePath;
        if (!workspacePath || !fileName.includes(workspacePath)) {
            workspacePath = path.dirname(fileName);
            relativePath = path.basename(fileName);
        } else {
            let rootPathLength = workspacePath.length;
            relativePath = fileName.substr(rootPathLength + 1, fileName.length - rootPathLength);
        }
        return new EnvironmentVariables(workspacePath, relativePath, docContent);
    }
}