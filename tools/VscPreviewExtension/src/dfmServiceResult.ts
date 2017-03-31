// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

export class DfmServiceResult {
    data: String;
    type: String;

    constructor(data: String, type: String) {
        this.data = data;
        this.type = type;
    }
}