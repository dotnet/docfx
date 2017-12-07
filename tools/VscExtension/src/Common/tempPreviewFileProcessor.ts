// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExtensionContext } from "vscode";
import * as path from "path";
import * as fs from "fs";

import * as ConstVariables from "../ConstVariables/commonVariables";
import { TempPreviewFileInformation } from "./tempPreviewFileInformation";
import { Utility } from "./utility";

export class TempPreviewFileProcessor {
    // TODO: Write\Delete temp preview file at client side instead of server side.
    public static initializeTempFileInformation(context: ExtensionContext, navigationPort: string, config) {
        let environmentVariables = Utility.getEnvironmentVariables();
        if (environmentVariables == null) {
            return;
        }

        let basename = path.basename(environmentVariables.relativePath);
        let filenameWithoutExt = basename.substr(0, basename.length - path.extname(environmentVariables.relativePath).length);
        // TODO: Use manifest file to calculate those path
        let originalHtmlPath = path.join(environmentVariables.workspacePath, config.outputFolder, path.dirname(environmentVariables.relativePath), filenameWithoutExt + ".html");
        let tempPreviewFilePath = ConstVariables.filePathPrefix + path.join(environmentVariables.workspacePath, config.outputFolder, path.dirname(environmentVariables.relativePath), ConstVariables.docfxTempPreviewFile);

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