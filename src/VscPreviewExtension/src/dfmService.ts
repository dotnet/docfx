// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AxiosError } from 'axios';

import { DfmHttpClient } from './dfmHttpClient';
import { DfmServiceResult } from './dfmServiceResult';

export class DfmService {
    private static client = new DfmHttpClient();

    static async PreviewAsync(content: String): Promise<DfmServiceResult> {
        if (!content) {
            return null;
        }

        let markup = await DfmService.client.SendPostRequestAsync("preview", content);
        return markup;
    }

    static async GetTokenTreeAsync(content: String): Promise<DfmServiceResult> {
        if (!content) {
            return null;
        }

        let tokenTree = await DfmService.client.SendPostRequestAsync("generateTokenTree", content);
        return tokenTree;
    }

    static async ExitAsync() {
        await DfmService.client.SendPostRequestAsync("exit", null);
    }
}