import {
	IPCMessageReader, IPCMessageWriter,
	createConnection, IConnection, TextDocumentSyncKind,
	TextDocuments, TextDocument, Diagnostic, DiagnosticSeverity,
	InitializeParams, InitializeResult, TextDocumentPositionParams,
	CompletionItem, CompletionItemKind, TextDocumentIdentifier, 
	DocumentHighlight, Range, DocumentLinkParams, DocumentLink
} from 'vscode-languageserver';
import {requestUidController} from '../controllers/requestUidController';

let completionItem: CompletionItem[];
export let documents: TextDocuments = new TextDocuments();

export async function completionHandler(textDocumentPosition: TextDocumentPositionParams): Promise<CompletionItem[]>
{
	var text = documents.get(textDocumentPosition.textDocument.uri).getText();
	var tx = text.substring(text.lastIndexOf("@")+1);
	if(completionItem == undefined) {
		completionItem = await requestUidController.getCompletionItem(tx);
		console.log("completion == undefined " + tx);
		return completionItem;
	} else {
		completionItem = completionItem.filter(item => item.label.includes(tx));
		if(completionItem.length < 10) {
			completionItem = await requestUidController.getCompletionItem(tx);
		}
		console.log("completion > 10 " + tx);
		return completionItem;
	}
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
		console.log("docunmentLink:hh"+uid+":"+regEx.lastIndex);
        let xrefSpecs = await requestUidController.getData(uid);
        if(xrefSpecs != undefined && xrefSpecs.length > 0)
        {
            let documentLink: DocumentLink = DocumentLink.create(Range.create(startPos, endPos), xrefSpecs[0].href);
		    documentLinks.push(documentLink);
        }
	}
	return documentLinks;
}