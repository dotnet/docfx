/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */
'use strict';

import * as path from 'path';
import * as fs from 'fs';
import * as vscode from 'vscode';
import { LanguageClient, LanguageClientOptions, SettingMonitor, ServerOptions, TransportKind } from 'vscode-languageclient';

export function connectServer(context: vscode.ExtensionContext)
{
	// The server is implemented in node
	let serverModule = context.asAbsolutePath(path.join('server', 'server.js'));
	// The debug options for the server
	let debugOptions = { execArgv: ["--nolazy", "--debug=6009"] };
	
	// If the extension is launched in debug mode then the debug server options are used
	// Otherwise the run options are used
	let serverOptions: ServerOptions = {
		run : { module: serverModule, transport: TransportKind.ipc },
		debug: { module: serverModule, transport: TransportKind.ipc, options: debugOptions }
	};
	
	// Options to control the language client
	let clientOptions: LanguageClientOptions = {
		// Register the server for markdown documents
		documentSelector: ['markdown'],
		synchronize: {
			// Notify the server about file changes to '.clientrc files contain in the workspace
			fileEvents: vscode.workspace.createFileSystemWatcher('**/.clientrc')
		}
	};
	
	// Create the language client and start the client.
	let disposable = new LanguageClient('xrefIntellisense', 'XRef Intellisense', serverOptions, clientOptions).start();
	
	// Push the disposable to the context's subscriptions so that the 
	// client can be deactivated on extension deactivation
	context.subscriptions.push(disposable);
	isDocfxProject(context);
}

function isDocfxProject(context: vscode.ExtensionContext) 
{
	var workspaceRoot = vscode.workspace.rootPath;
	if(workspaceRoot != undefined) {
		let fullPath = path.join(workspaceRoot,'docfx.json');
		if(fs.existsSync(fullPath)) highlight(context);
	}
}

function highlight(context: vscode.ExtensionContext)
{
	// create a decorator type that we use to decorate special words;
	const specialWordsDecorationType = vscode.window.createTextEditorDecorationType({
		cursor: 'crosshair',
		color: 'rgba(255,255,0,1)',
		border:"1px",
		borderRadius:"20"
	});

	let activeEditor = vscode.window.activeTextEditor;
	if (activeEditor) {
		triggerUpdateDecorations();
	}

	vscode.window.onDidChangeActiveTextEditor(editor => {
		activeEditor = editor;
		if (editor) {
			triggerUpdateDecorations();
		}
	}, null, context.subscriptions);

	vscode.workspace.onDidChangeTextDocument(event => {
		if (activeEditor && event.document === activeEditor.document) {
			triggerUpdateDecorations();
		}
	}, null, context.subscriptions);

	var timeout = null;
	function triggerUpdateDecorations() {
		if (timeout) {
			clearTimeout(timeout);
		}
		timeout = setTimeout(updateDecorations, 500);
	}

	function updateDecorations() {
		if (!activeEditor) {
			return;
		}
		const regEx = /(@([^ >]+))|(<xref:([^ >]+)>)/g;
		const text = activeEditor.document.getText();
		const specialWords: vscode.DecorationOptions[] = [];
		let match;
		while (match = regEx.exec(text)) {
			const startPos = activeEditor.document.positionAt(match.index);
			const endPos = activeEditor.document.positionAt(match.index + match[0].length);
			const decoration = { range: new vscode.Range(startPos, endPos), hoverMessage: 'Uid **' + match[0] + '**' };
			specialWords.push(decoration);
		}
		activeEditor.setDecorations(specialWordsDecorationType, specialWords);
	}
}