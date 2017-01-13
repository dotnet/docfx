// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AxiosError } from 'axios';

import { DfmHttpClient } from './dfmHttpClient';
import { DfmServiceResult } from './dfmServiceResult';

export class DfmService {
    private static client = new DfmHttpClient();

    static async previewAsync(content: String): Promise<DfmServiceResult> {
        if (!content) {
            return null;
        }

        return await DfmService.client.sendPostRequestAsync("preview", content);
    }

    static async getTokenTreeAsync(content: String): Promise<DfmServiceResult> {
        if (!content) {
            return null;
        }

        return await DfmService.client.sendPostRequestAsync("generateTokenTree", content);
    }

    static async exitAsync() {
        await DfmService.client.sendPostRequestAsync("exit", null);
    }
}