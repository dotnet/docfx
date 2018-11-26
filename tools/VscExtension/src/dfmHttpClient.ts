// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { default as Axios, AxiosResponse } from 'axios';

import * as ConstVariable from "./constVariables/commonVariables";

export class DfmHttpClient {
    private static urlPrefix = "http://localhost:";

    static async sendPostRequestAsync(port: string, command: string, content = null, relativePath = null, shouldSeparateMarkupResult = false, tempPreviewFilePath = null, pageRefreshJsFilePath = null, originalHtmlPath = null, navigationPort = null): Promise<AxiosResponse> {
        let promise = Axios.post(this.urlPrefix + port, {
            name: command,
            markdownContent: content,
            tempPreviewFilePath: tempPreviewFilePath,
            relativePath: relativePath,
            shouldSeparateMarkupResult: shouldSeparateMarkupResult,
            originalHtmlPath: originalHtmlPath,
            pageRefreshJsFilePath: pageRefreshJsFilePath,
            navigationPort: navigationPort
        });

        let response: AxiosResponse;
        try {
            response = await promise;
        } catch (err) {
            let record = err.response;
            if (!record) {
                throw new Error(ConstVariable.noServiceErrorMessage);
            }

            switch (record.status) {
                case 400:
                    throw new Error(`[Client Error]: ${record.statusText}`);
                case 500:
                    throw new Error(`[Server Error]: ${record.statusText}`);
                default:
                    throw new Error(err);
            }
        }
        return response;
    }
}