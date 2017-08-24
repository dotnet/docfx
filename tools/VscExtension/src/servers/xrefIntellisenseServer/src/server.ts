/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */
'use strict';

import {
	IPCMessageReader, IPCMessageWriter,
	createConnection, IConnection, TextDocumentSyncKind,
	TextDocuments, TextDocument, Diagnostic, DiagnosticSeverity,
	InitializeParams, InitializeResult, TextDocumentPositionParams,
	CompletionItem, CompletionItemKind, TextDocumentIdentifier, 
	DocumentHighlight, Range, DocumentLinkParams, DocumentLink
} from 'vscode-languageserver';
import {requestUidController} from './controllers/requestUidController';
import * as requestHandler from './handlers/requestHandler';
import {httpRequestFactory} from './utilities/httpRequestFactory';
import * as fs from 'fs';
import * as path from 'path';

export let docfxJson;
// Create a connection for the server. The connection uses Node's IPC as a transport
let connection: IConnection = createConnection(new IPCMessageReader(process), new IPCMessageWriter(process));

// Create a simple text document manager. The text document manager
// supports full document sync only
let documents: TextDocuments = new TextDocuments();
// Make the text document manager listen on the connection
// for open, change and close text document events
documents.listen(connection);
requestHandler.documents.listen(connection);

// After the server has started the client sends an initialize request. The server receives
// in the passed params the rootPath of the workspace plus the client capabilities. 
let workspaceRoot: string;
connection.onInitialize((params): InitializeResult => {
	workspaceRoot = params.rootPath;
	if(workspaceRoot != null) {
		let fullPath = path.join(workspaceRoot,'docfx.json');
		if(fs.existsSync(fullPath)) docfxJson = JSON.parse(fs.readFileSync(fullPath, 'utf8'));
	}
	if(docfxJson != undefined) {
		httpRequestFactory.xrefService = docfxJson.xrefService;
		if(httpRequestFactory.xrefService != undefined) httpRequestFactory.isDocfxProject = true;
	}
	console.log("xrefIntellisense enabled");
	if(httpRequestFactory.isDocfxProject) {
		// This handler provides the initial list of the completion items.
		connection.onCompletion(requestHandler.completionHandler);
		connection.onDocumentLinks(requestHandler.documentLinkHandler);
		return {
			capabilities: {
				// Tell the client that the server works in FULL text document sync mode
				textDocumentSync: documents.syncKind,
				// Tell the client that the server support code complete
				completionProvider: {
					resolveProvider: false
					//triggerCharacters:["M"]
				},
				documentLinkProvider: {
					resolveProvider: false
				}
			}
		}
	}
	
	return {
		capabilities: {
			// Tell the client that the server works in FULL text document sync mode
			textDocumentSync: documents.syncKind
		}
	}
});

// Listen on the connection
connection.listen();