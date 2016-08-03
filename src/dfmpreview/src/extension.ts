'use strict';
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import { workspace, window, ExtensionContext, commands,TextEditor, TextDocumentContentProvider, EventEmitter, Event, Uri, TextDocumentChangeEvent, ViewColumn,
		TextEditorSelectionChangeEvent,TextDocument, Disposable } from "vscode";
import * as fs from "fs";
import * as path from "path";
import * as child_process from 'child_process';


var previewresult = "";
var provider;
var document_uri;
var Is_end = true;
const ENDCODE = 7;

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: ExtensionContext) {

    // Use the console to output diagnostic information (console.log) and errors (console.error)
    // This line of code will only be executed once when your extension is activated
    //console.log('Congratulations, your extension "previewtest-ts" is now active!');


	let  dfm_process  = new Mypreview(context);
	provider = new MDDocumentContentProvider(context);		//the html holder
	let registration = workspace.registerTextDocumentContentProvider('markdown', provider);

    
	//event registe
    let d1 = commands.registerCommand('DFM.showpreview', dfm_process => showPreview(dfm_process));
	let d2 = commands.registerCommand('DFM.showpreviewToside' , uri => showPreview(dfm_process , uri  , true));
	let d3 = commands.registerCommand('DFM.showsource' ,showSource);

    context.subscriptions.push(d1,d2,d3,registration);

    workspace.onDidSaveTextDocument(document => {
		if (isMarkdownFile(document)) {
			document_uri = getMarkdownUri(document.uri);
			dfm_process.callDfm();
			//provider.update(uri);
		}
	});

	workspace.onDidChangeTextDocument(event => {
		if (isMarkdownFile(event.document)) {
			document_uri = getMarkdownUri(event.document.uri);
			//provider.update(uri);
			dfm_process.callDfm();
		}
	});

	workspace.onDidChangeConfiguration(() => {
		workspace.textDocuments.forEach(document => {
			if (document.uri.scheme === 'markdown') {
				// update all generated md documents
				//provider.update(document.uri);
				document_uri = document_uri;
				dfm_process.callDfm();
			}
		});
	});
}

//check the file type 
function isMarkdownFile(document: TextDocument) {
	return document.languageId === 'markdown'
		&& document.uri.scheme !== 'markdown'; // prevent processing of own documents
}


function getMarkdownUri(uri: Uri) {
	return uri.with({ scheme: 'markdown', path: uri.path + '.rendered', query: uri.toString() });
}



function showPreview(dfm_preview: Mypreview , uri?: Uri, sideBySide: boolean = false) {
	dfm_preview._is_firsttime = true;
	let resource = uri;
	if (!(resource instanceof Uri)) {
		if (window.activeTextEditor) {
			// we are relaxed and don't check for markdown files
			resource = window.activeTextEditor.document.uri;
		}
	}

	if (!(resource instanceof Uri)) {
		if (!window.activeTextEditor) {
			// this is most likely toggling the preview
			return commands.executeCommand('markdown.showSource');
		}
		// nothing found that could be shown or toggled
		return;
	}

	let thenable = commands.executeCommand('vscode.previewHtml',
		getMarkdownUri(resource),
		getViewColumn(sideBySide),
		`Preview '${path.basename(resource.fsPath)}'`);
	
	document_uri = getMarkdownUri(resource);
	dfm_preview.callDfm();
	return thenable;
}

function getViewColumn(sideBySide): ViewColumn {
	const active = window.activeTextEditor;
	if (!active) {
		return ViewColumn.One;
	}

	if (!sideBySide) {
		return active.viewColumn;
	}

	switch (active.viewColumn) {
		case ViewColumn.One:
			return ViewColumn.Two;
		case ViewColumn.Two:
			return ViewColumn.Three;
	}

	return active.viewColumn;
}

function showSource(mdUri: Uri) {
	if (!mdUri) {
		return commands.executeCommand('workbench.action.navigateBack');
	}

	const docUri = Uri.parse(mdUri.query);

	for (let editor of window.visibleTextEditors) {
		if (editor.document.uri.toString() === docUri.toString()) {
			return window.showTextDocument(editor.document, editor.viewColumn);
		}
	}

	return workspace.openTextDocument(docUri).then(doc => {
		return window.showTextDocument(doc);
	});
}


// this method is called when your extension is deactivated
//export function deactivate() {
//}

class Mypreview{
    private _spawn : child_process.ChildProcess;
	private _waiting: boolean;
	public _is_firsttime:boolean;
    //public _preview_result : string;

    constructor(context : ExtensionContext ){
        let extpath  = context.asAbsolutePath('./DfmParse/Dfm_test.exe');
        this._spawn = child_process.spawn(extpath);
		this._waiting = false;
		//this._is_firsttime = true;
        //count = 0;
        //this._preview_result = "";

        this._spawn.stdout.on('data' , function(data){
            //console.log(data.toString());
			let tmp = data.toString();
			let endcharcode = tmp.charCodeAt(tmp.length-1);
			if(Is_end && endcharcode == ENDCODE){
				//console.log('this have not been cut');
				previewresult = tmp;
				provider.update(document_uri);
			}
			//the first one if the result benn cut
			else if(Is_end && endcharcode != ENDCODE){
				//console.log('the first one if the result benn cut');
				previewresult = tmp;
				Is_end = false;
			}
			//the result be cut and this is not the last one
			else if(!Is_end && endcharcode != ENDCODE){
				//console.log('the result be cut and this is not the last one');
				previewresult += tmp;
			}
			//the result be cut and this is the last one
			else{
				//console.log('the result be cut and this is the last one');
				previewresult += tmp;
				Is_end = true;
				provider.update(document_uri);
			}
        });

        this._spawn.stderr.on('data' , function(data){
            console.log("error " + data + '\n');
        });

        this._spawn.on('exit' , function(code){
            console.log('child process exit with code ' + code);
        });
    }

	private sendtext(){
		let rtpath = workspace.rootPath;
		//console.log(workspace.rootPath);
		let editor = window.activeTextEditor;
		if(!editor){
			return;
		}
		let doc = editor.document;
		let docContent = doc.getText();
		//console.log(doc.fileName);
		let filename = doc.fileName;
		let rtpath_length = rtpath.length;
		let filepath = filename.substr(rtpath_length+1 , filename.length - rtpath_length);

		if(doc.languageId=== "markdown"){
			let num_of_row = docContent.split("\r\n").length;
			this._spawn.stdin.write(num_of_row + '\n');
			this._spawn.stdin.write(rtpath + '\n');
			this._spawn.stdin.write(filepath + '\n');
			this._spawn.stdin.write(docContent + '\n');
		}
	}

    public callDfm(){
		if(this._is_firsttime){
			//for the firt time , because the activeTextEditor wil be translate to the viewColumn.two.
			this._is_firsttime = false;
			this.sendtext();
		}
		else if (!this._waiting) {
			this._waiting = true;
			setTimeout(() => {
				this._waiting = false;
				this.sendtext();
			}, 300);
		}
    }    
}


class MDDocumentContentProvider implements TextDocumentContentProvider {
	private _context: ExtensionContext;
	private _onDidChange = new EventEmitter<Uri>();
	private _waiting : boolean;
    

	constructor(context: ExtensionContext) {
		this._context = context;
		this._waiting = false;
	}

	private getMediaPath(mediaFile) : string {
		return this._context.asAbsolutePath(path.join('media', mediaFile));
	}

	public provideTextDocumentContent(uri: Uri): Thenable<string> {

		return workspace.openTextDocument(Uri.parse(uri.query)).then(document => {
			const head = [].concat(
				'<!DOCTYPE html>',
				'<html>',
				'<head>',
				'<meta http-equiv="Content-type" content="text/html;charset=UTF-8">',
				//`<link rel="stylesheet" type="text/css" href="${this.getMediaPath('docfx.vendor.css')}" >`,
				//`<link rel="stylesheet" type="text/css" href="${this.getMediaPath('docfx.css')}" >`,
				`<link rel="stylesheet" type="text/css" href="${this.getMediaPath('tomorrow.css')}" >`,
				`<link rel="stylesheet" type="text/css" href="${this.getMediaPath('markdown.css')}" >`,
				`<link rel="stylesheet" type="text/css" href="${this.getMediaPath('main.css')}" >`,
				//this.computeCustomStyleSheetIncludes(uri),
				`<base href="${document.uri.toString(true)}">`,
				'</head>',
				'<body>'
			).join('\n');

            const body = previewresult;

			const tail = [
				`<script type="text/javascript" src="${this.getMediaPath('docfx.vendor.js')}"></script>`,
				//`<script type="text/javascript" src="${this.getMediaPath('docfx.js')}"></script>`,
				`<script type="text/javascript" src="${this.getMediaPath('main.js')}"></script>`,
				`<script>hljs.initHighlightingOnLoad();</script>`,
				'</body>',
				'</html>'
			].join('\n');

			return head + body + tail;
		});
	}

	get onDidChange(): Event<Uri> {
		return this._onDidChange.event;
	}

	public update(uri: Uri) {/*
		if (!this._waiting) {
			this._waiting = true;
			setTimeout(() => {
				this._waiting = false;
				this._onDidChange.fire(uri);
			}, 500);
		}*/
		this._onDidChange.fire(uri);
	}
}