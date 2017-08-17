import {
	IPCMessageReader, IPCMessageWriter,
	createConnection, IConnection, TextDocumentSyncKind,
	TextDocuments, TextDocument, Diagnostic, DiagnosticSeverity,
	InitializeParams, InitializeResult, TextDocumentPositionParams,
	CompletionItem, CompletionItemKind, TextDocumentIdentifier, 
	DocumentHighlight, Range, DocumentLinkParams, DocumentLink
} from 'vscode-languageserver';
import {requestUidController} from '../controllers/requestUidController';

export let documents: TextDocuments = new TextDocuments();

export async function completionHandler(textDocumentPosition: TextDocumentPositionParams): Promise<CompletionItem[]>
{
	// The pass parameter contains the position of the text document in 
	// which code complete got requested. For the example we ignore this
	// info and always provide the same completion items.
	//console.log("hehe:"+textDocumentPosition.textDocument.uri);
	var text = documents.get(textDocumentPosition.textDocument.uri).getText();
	var tx = text.substring(text.lastIndexOf("@")+1);
	// var f = vscode.workspace.openTextDocument;
	// var t = f(textDocumentPosition.textDocument.uri);
	// .then(document=>{
	// 	let text = document.getText(document.lineAt(textDocumentPosition.position.line).range);
    // 	var tx = text.substring(text.lastIndexOf("@")+1);
	// });
	
	var data = 	await requestUidController.getCompletionItem(tx);
	console.log("data::");
	return data;
	// return [
	// 	{
	// 		label: 'TypeScript',
	// 		kind: CompletionItemKind.Text,
	// 		data: 1
	// 	},
	// 	{
	// 		label: 'JavaScript',
	// 		kind: CompletionItemKind.Text,
	// 		data: 2
	// 	}
	// ]
}

export async function highlightHandler(textDocumentPosition: TextDocumentPositionParams): Promise<DocumentHighlight[]>
{
    console.log("highlight");
	const regEx = /(@([^ \r\n>]+))|(<xref:([^ \r\n>]+)>)/g;
	let textDocument = documents.get(textDocumentPosition.textDocument.uri);
	const text = textDocument.getText();
	let match;
	let documentHighlight: DocumentHighlight[] = [];
	while(match = regEx.exec(text)) {
		const startPos = textDocument.positionAt(match.index);
		const endPos = textDocument.positionAt(match.index + match[0].length);
		let highLight = DocumentHighlight.create(Range.create(startPos, endPos),2);
		documentHighlight.push(highLight);
	}
	return documentHighlight;
}

export async function documentLinkHandler(documentLinkParams: DocumentLinkParams): Promise<DocumentLink[]>
{
	const regEx = /(@([^ \r\n>]+))|(<xref:([^ \r\n>]+)>)/g;
	let textDocument = documents.get(documentLinkParams.textDocument.uri);
	const text = textDocument.getText();
	let match;
	let documentLinks: DocumentLink[] = [];
	while(match = regEx.exec(text)) {
		const startPos = textDocument.positionAt(match.index);
		const endPos = textDocument.positionAt(match.index + match[0].length);
		let tx = textDocument.getText();
		let uid;
		if(tx[match.index] == '@') {
			uid = tx.substr(match.index + 1, match[0].length - 1);
		} else {
			uid = tx.substr(match.index + 6, match[0].length - 7);
		}
		console.log("docunmentLink:"+uid+":"+regEx.lastIndex);
        let xrefSpecs = await requestUidController.getData('http://xrefservice0810.azurewebsites.net/uids/', uid);
        if(xrefSpecs.length > 0)
        {
            let documentLink: DocumentLink = DocumentLink.create(Range.create(startPos, endPos), xrefSpecs[0].href);
		    documentLinks.push(documentLink);
        }
	}
	return documentLinks;
}