// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as childProcess from "child_process";

export class Common {
    static spawn(command: string, options) : childProcess.ChildProcess {
        let file, args;
        if (process.platform === 'win32') {
            file = 'cmd.exe';
            args = ['/s', '/c', '"' + command + '"'];
            options = Object.assign({}, options);
            options.windowsVerbatimArguments = true;
        }
        else {
            file = '/bin/sh';
            args = ['-c', command];
        }
        return childProcess.spawn(file, args, options);
    };
}
