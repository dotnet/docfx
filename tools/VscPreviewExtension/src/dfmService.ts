// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AxiosError } from 'axios';

import { DfmHttpClient } from './dfmHttpClient';
import * as ConstVariable from "./ConstVariable";

export class DfmService {
    static preview(docfxServicePort, content: String, workspacePath, relativePath, writeTempPreviewFile = false, previewFilePath = null, pageRefreshJsFilePath = null, builtHtmlPath = null) {
        if (!content) {
            return null;
        }

        return new Promise(function (fulfill, reject) {
            DfmHttpClient.sendPostRequest(docfxServicePort, ConstVariable.previewCommand, content, workspacePath, relativePath, writeTempPreviewFile, previewFilePath, pageRefreshJsFilePath, builtHtmlPath)
                .then(function (res) {
                    fulfill(res);
                })
                .catch(function (err) {
                    reject(err);
                })
        })
    }

    static getTokenTree(docfxServicePort, content: String, workspacePath, relativePath) {
        if (!content) {
            return null;
        }

        return new Promise(function (fulfill, reject) {
            DfmHttpClient.sendPostRequest(docfxServicePort, ConstVariable.tokenTreeCommand, content, workspacePath, relativePath)
                .then(function (res) {
                    fulfill(res);
                })
                .catch(function (err) {
                    reject(err);
                })
        })
    }

    static exit(docfxServicePort) {
        return new Promise(function (fulfill, reject) {
            DfmHttpClient.sendPostRequest(docfxServicePort, ConstVariable.exitCommand)
                .then(function (res) {
                    fulfill(res);
                })
                .catch(function (err) {
                    reject(err);
                })
        })
    }
}