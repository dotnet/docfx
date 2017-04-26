// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ProxyRequest } from "./proxyRequest";

export class RequestArray {
    private requests: ProxyRequest[] = [];

    public push(request: ProxyRequest) {
        let key = request.getKeyString();
        if (this.requests !== undefined && this.requests.length != 0) {
            this.requests.forEach(item => {
                if (item.getKeyString() === key) {
                    item = request;
                    return;
                }
            })
        }
        this.requests.push(request);
    }

    public pop() {
        if (this.requests.length != 0) {
            return this.requests.pop();
        }
    }
}
