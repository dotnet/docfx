// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Uri, window, workspace } from "vscode";
import * as fs from "fs";
import * as path from "path";

import * as ConstVariables from "../ConstVariables/commonVariables";
import { PreviewProcessor } from "./previewProcessor";
import { ProxyResponse } from "../Proxy/proxyResponse";
import { ProxyRequest } from "../Proxy/proxyRequest";
import { MarkupResult } from "../Common/markupResult";
import { TempPreviewFileProcessor } from "../Common/tempPreviewFileProcessor";
import { TempPreviewFileInformation } from "../Common/tempPreviewFileInformation";

export class DocFXPreviewProcessor extends PreviewProcessor {
    public navigationPort: string;
    public shouldRefreshPreviewPage: boolean = false;
    public markupResult: MarkupResult = new MarkupResult();

    private _isFirstTime: boolean = false;
    private _openPreviewPageCallback;
    private _shouldWriteTempPreviewFile: boolean = false;
    private _tempPreviewFileInformation: TempPreviewFileInformation;

    constructor(context) {
        super(context);
    }

    public startPreview(uri: Uri, callback) {
        let docfxConfigFilePath = this.validConfig();
        if (docfxConfigFilePath != undefined && docfxConfigFilePath != null) {
            try {
                var config = this.parseConfig(docfxConfigFilePath);
                PreviewProcessor.proxy.setLegacyMode(config.isDfmLatest);

                this._tempPreviewFileInformation = TempPreviewFileProcessor.initializeTempFileInformation(PreviewProcessor.context, this.navigationPort, config);
            } catch (err) {
                window.showErrorMessage(`[Environment Error]: ${err}`);
            }
            this._shouldWriteTempPreviewFile = true;
            this._isFirstTime = true;
            this._openPreviewPageCallback = callback;
            this.updateContent(uri);
        } else {
            window.showErrorMessage(`[Exntension Error]: Please Open a DocFX project folder`);
        }
    }

    private validConfig() {
        let workspacePath = workspace.rootPath;
        if (!workspacePath) {
            return null;
        } else {
            let previewConfigFilePath = path.join(workspacePath, ConstVariables.previewConfigFileName);
            let docfxConfigFileName = ConstVariables.docfxConfigFileName;
            if (fs.existsSync(previewConfigFilePath)) {
                let previewconfig = JSON.parse(fs.readFileSync(previewConfigFilePath).toString());
                if (previewconfig.docfxConfigFileName != null) {
                    docfxConfigFileName = previewconfig.docfxConfigFileName;
                }
            }
            if (fs.existsSync(path.join(workspacePath, docfxConfigFileName))) {
                return path.join(workspacePath, docfxConfigFileName);
            } else {
                return null;
            }
        }
    }

    protected appendTempPreviewFileInformation(request: ProxyRequest) {
        if (this._shouldWriteTempPreviewFile) {
            this._shouldWriteTempPreviewFile = false;
            request.appendTempPreviewFileInformation(this._tempPreviewFileInformation);
        }
        return request;
    }

    private parseConfig(docfxConfigFilePath) {
        var defaultConfig = {
            "outputFolder": "_site",
            "isDfmLatest": false
        };

        // The first char maybe \uFEFF
        let docfxConfig = JSON.parse(fs.readFileSync(docfxConfigFilePath).toString().replace(/^\uFEFF/, ''));

        // OutputFolder
        var outputFolder = defaultConfig["outputFolder"];
        if (docfxConfig.build.dest != undefined) {
            outputFolder = docfxConfig.build.dest;
        }

        // LagacyMode
        var isDfmLatest = defaultConfig["isDfmLatest"];
        if(docfxConfig.build.markdownEngineName === "dfm-latest"){
            isDfmLatest = true;
        }

        return {
            "outputFolder": outputFolder,
            "isDfmLatest": isDfmLatest
        };
    }

    protected pageRefresh(response: ProxyResponse) {
        if (this._isFirstTime) {
            this._isFirstTime = false;
            this._openPreviewPageCallback(this._tempPreviewFileInformation.tempPreviewFilePath);
        }
        this.shouldRefreshPreviewPage = true;
        this.markupResult.rawTitle = response.markupResult.rawTitle;
        this.markupResult.contentWithoutRawTitle = response.markupResult.contentWithoutRawTitle;
        this.markupResult.content = response.markupResult.rawTitle + response.markupResult.contentWithoutRawTitle;
    }
}