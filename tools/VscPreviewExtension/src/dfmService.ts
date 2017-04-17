// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AxiosError } from 'axios';

import { DfmHttpClient } from './dfmHttpClient';
import { Command } from './constVariables/command';

export class DfmService {
    static async previewAsync(docfxServicePort, content, workspacePath, relativePath, writeTempPreviewFile = false, previewFilePath = null, pageRefreshJsFilePath = null, builtHtmlPath = null) {
        if (!content) {
            return null;
        }

        return await DfmHttpClient.sendPostRequestAsync(docfxServicePort, Command.previewCommand, content, workspacePath, relativePath, writeTempPreviewFile, previewFilePath, pageRefreshJsFilePath, builtHtmlPath);
    }

    static async getTokenTreeAsync(docfxServicePort, content, workspacePath, relativePath) {
        if (!content) {
            return null;
        }

        return await DfmHttpClient.sendPostRequestAsync(docfxServicePort, Command.tokenTreeCommand, content, workspacePath, relativePath);
    }

    static async exitAsync(docfxServicePort) {
        await DfmHttpClient.sendPostRequestAsync(docfxServicePort, Command.exitCommand);
    }
}
