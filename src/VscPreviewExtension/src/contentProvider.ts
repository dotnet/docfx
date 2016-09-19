// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { workspace, ExtensionContext, TextDocumentContentProvider, EventEmitter, Event, Uri } from "vscode";
import * as path from "path";

export class ContentProvider implements TextDocumentContentProvider {
    private _context: ExtensionContext;
    private _onDidChange = new EventEmitter<Uri>();
    protected _content: string;
    public port;

    constructor(context: ExtensionContext) {
        this._context = context;
    }

    protected getMediaPath(mediaFile): string {
        return this._context.asAbsolutePath(path.join("media", mediaFile));
    }

    protected getNodeModulesPath(resourceName: string): string {
        return this._context.asAbsolutePath(path.join("node_modules", resourceName));
    }

    public provideTextDocumentContent(uri: Uri): Thenable<string> {
        return workspace.openTextDocument(Uri.parse(uri.query)).then(document => {
            const content = "";
            return content;
        });
    }

    get onDidChange(): Event<Uri> {
        return this._onDidChange.event;
    }

    public update(uri: Uri, content: string) {
        this._content = content;
        this._onDidChange.fire(uri);
    }
}