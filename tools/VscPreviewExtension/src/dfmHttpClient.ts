// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { default as Axios, AxiosResponse } from 'axios';

import * as ConstVariable from "./ConstVariable";

export class DfmHttpClient {
    private static urlPrefix = "http://localhost:";

    static async sendPostRequest(port: string, command, content = null, workspacePath = null, relativePath = null, writeTempPreviewFile = false, previewFilePath = null, pageRefreshJsFilePath = null, builtHtmlPath = null): Promise<AxiosResponse> {
        let promise = Axios.post(this.urlPrefix + port, {
            name: command,
            markdownContent: content,
            previewFilePath: previewFilePath,
            workspacePath: workspacePath,
            relativePath: relativePath,
            writeTempPreviewFile: writeTempPreviewFile,
            builtHtmlPath: builtHtmlPath,
            pageRefreshJsFilePath: pageRefreshJsFilePath
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