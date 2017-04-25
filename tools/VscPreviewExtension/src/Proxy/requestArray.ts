// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ProxyRequest } from "./proxyRequest";

export class RequestArray {
    private requests: { [id: string]: ProxyRequest } = {};
    private _keys: string[] = [];

    public add(request: ProxyRequest) {
        if (this.exist(request)) {
            this.update(request);
        } else {
            this.push(request);
        }
    }

    public pop() {
        if (this._keys.length == 0) {
            return null;
        } else {
            let key = this._keys[0];
            this._keys.splice(0, 1);
            let request = this.requests[key];
            delete this.requests[key];
            return request;
        }
    }

    private push(request: ProxyRequest) {
        let key = request.getKeyString();
        this.requests[key] = request;
        this._keys.push(key);
    }

    private update(request: ProxyRequest) {
        let key = request.getKeyString();
        if (this.exist(request)) {
            this.requests[key] = request;
        }
    }

    private exist(request: ProxyRequest) {
        let key = request.getKeyString();
        return typeof this.requests[key] !== "undefined";
    }
}
