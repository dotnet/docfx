// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AxiosError } from 'axios';

import { DfmHttpClient } from './dfmHttpClient';
import { Command } from './constVariables/command';

export class DfmService {
    static async previewAsync(docfxServicePort, content, relativePath, shouldSeparateMarkupResult = false, tempPreviewFilePath = null, pageRefreshJsFilePath = null, originalHtmlPath = null, navigationPort = null) {
        if (!content) {
            return null;
        }

        return await DfmHttpClient.sendPostRequestAsync(docfxServicePort, Command.previewCommand, content, relativePath, shouldSeparateMarkupResult, tempPreviewFilePath, pageRefreshJsFilePath, originalHtmlPath, navigationPort);
    }

    static async getTokenTreeAsync(docfxServicePort, content, relativePath) {
        if (!content) {
            return null;
        }

        return await DfmHttpClient.sendPostRequestAsync(docfxServicePort, Command.tokenTreeCommand, content, relativePath);
    }

    static async exitAsync(docfxServicePort) {
        await DfmHttpClient.sendPostRequestAsync(docfxServicePort, Command.exitCommand);
    }

    // TODO: Implement delete temp previewFile
}
