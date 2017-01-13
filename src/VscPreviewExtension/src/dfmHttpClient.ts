// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import axios from 'axios';
import { AxiosResponse } from 'axios'

import { DfmServiceResult } from './dfmServiceResult';

export class DfmHttpClient {
    // TODO: make the urlPrefix configurable
    private urlPrefix = "http://localhost:4001";

    async SendPostRequestAsync(command: String, content: String): Promise<DfmServiceResult> {
        let promise = axios.post(this.urlPrefix, {
            name: command,
            documentation: content
        });

        let response : AxiosResponse;
        try {
            response = await promise;
        } catch (err) {
            let record = err.response;
            if (!record) {
                throw new Error(err)
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
        return new DfmServiceResult(response.data, response.headers["content-type"]);
    }
}