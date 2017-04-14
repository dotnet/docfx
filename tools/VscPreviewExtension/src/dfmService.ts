// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AxiosError } from 'axios';

import { DfmHttpClient } from './dfmHttpClient';
import * as ConstVariable from "./ConstVariable";

export class DfmService {
    static async previewAsync(docfxServicePort, content: String, workspacePath, relativePath, writeTempPreviewFile = false, previewFilePath = null, pageRefreshJsFilePath = null, builtHtmlPath = null) {
        if (!content) {
            return null;
        }

        return await DfmHttpClient.sendPostRequest(docfxServicePort, ConstVariable.previewCommand, content, workspacePath, relativePath, writeTempPreviewFile, previewFilePath, pageRefreshJsFilePath, builtHtmlPath);
    }

    static async getTokenTreeAsync(docfxServicePort, content: String, workspacePath, relativePath) {
        if (!content) {
            return null;
        }

        return await DfmHttpClient.sendPostRequest(docfxServicePort, ConstVariable.tokenTreeCommand, content, workspacePath, relativePath);
    }

    static async exitAsync(docfxServicePort) {
        await DfmHttpClient.sendPostRequest(docfxServicePort, ConstVariable.exitCommand);
    }
}