// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExtensionContext, window, workspace } from "vscode";
import * as path from "path";
import * as fs from "fs";

import * as ConstVariables from "../ConstVariables/commonVariables";
import { TempPreviewFileInformation } from "./tempPreviewFileInformation";

export class TempPreviewFileProcessor {
    // TODO: Write\Delete temp preview file at client side instead of server side.
    public static initializeTempFileInformation(context: ExtensionContext, navigationPort: string, config) {
        let workspacePath = workspace.rootPath;
        let editor = window.activeTextEditor;
        let doc = editor.document;
        let fileName = doc.fileName;
        let rootPathLength = workspacePath.length;
        let relativePath = fileName.substr(rootPathLength + 1, fileName.length - rootPathLength);

        let filename = path.basename(relativePath);
        let filenameWithoutExt = filename.substr(0, filename.length - path.extname(relativePath).length);
        // TODO: Use manifest file to calculate those path
        let originalHtmlPath = path.join(workspacePath, config.outputFolder, path.dirname(relativePath), filenameWithoutExt + ".html");
        let tempPreviewFilePath = ConstVariables.filePathPrefix + path.join(workspacePath, config.outputFolder, path.dirname(relativePath), ConstVariables.docfxTempPreviewFile);

        let pageRefreshJsFilePath = context.asAbsolutePath(path.join("media", "js", "htmlUpdate.js"));
        if (!fs.existsSync(originalHtmlPath)) {
            throw new Error(`Please local build this project before DocFX preview`);
        }
        if (!fs.existsSync(pageRefreshJsFilePath)) {
            throw new Error("Page refresh js file missed");
        }
        return new TempPreviewFileInformation(originalHtmlPath, tempPreviewFilePath, pageRefreshJsFilePath, navigationPort);
    }
}