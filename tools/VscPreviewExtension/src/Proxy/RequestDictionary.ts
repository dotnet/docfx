// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ProxyRequest } from "./proxyRequest";

export class RequestArray {
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
            let request = this[key];
            delete this[key];
            return request;
        }
    }

    private push(request: ProxyRequest) {
        let key = request.documentUri.toString();
        this[key] = request;
        this._keys.push(key);
    }

    private update(request: ProxyRequest) {
        let key = request.documentUri.toString();
        this[key] = request;
    }

    private exist(request: ProxyRequest) {
        let key = request.documentUri.toString();
        if (typeof this[key] === "undefined") {
            return false;
        } else {
            return true;
        }
    }
}
