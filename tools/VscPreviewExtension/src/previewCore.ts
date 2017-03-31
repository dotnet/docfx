// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Uri, ExtensionContext } from "vscode";
import { MarkdownDocumentContentProvider } from "./markdownDocumentContentProvider";
import { ChildProcessHost } from "./childProcessHost"

// Create a child process(DfmRender) by "_spawn" to render a html
export class PreviewCore extends ChildProcessHost {
    public isFirstTime: boolean;
    public provider: MarkdownDocumentContentProvider;

    public callDfm(uri: Uri) {
        this._documentUri = uri;
        if (this.isFirstTime) {
            // In the first time, if wait for the timeout, activeTextEditor will be the preview window.
            this.isFirstTime = false;
            this.sendMessage();
        } else if (!this._waiting) {
            this._waiting = true;
            setTimeout(() => {
                this._waiting = false;
                this.sendMessage();
            }, 300);
        }
    }

    protected initializeProvider(context: ExtensionContext) {
        this.provider = new MarkdownDocumentContentProvider(context);
    }

    protected writeToStdin(rootPath: string, filePath: string, numOfRow: number, docContent: string) {
        this.provider.fileName = filePath;
        this._spawn.stdin.write(this.appendWrap("dfmmarkup"));
        this._spawn.stdin.write(this.appendWrap(rootPath));
        this._spawn.stdin.write(this.appendWrap(filePath));
        this._spawn.stdin.write(this.appendWrap(numOfRow));
        this._spawn.stdin.write(this.appendWrap(docContent));
    }
}
