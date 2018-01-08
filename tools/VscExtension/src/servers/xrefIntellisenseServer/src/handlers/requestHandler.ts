import {
	IPCMessageReader, IPCMessageWriter,
	createConnection, IConnection, TextDocumentSyncKind,
	TextDocuments, TextDocument, Diagnostic, DiagnosticSeverity,
	InitializeParams, InitializeResult, TextDocumentPositionParams,
	CompletionItem, CompletionItemKind, TextDocumentIdentifier, 
	DocumentHighlight, Range, DocumentLinkParams, DocumentLink,
	Position, CompletionList
} from 'vscode-languageserver';
import {requestUidController} from '../controllers/requestUidController';

let completionItem: CompletionItem[];
export let documents: TextDocuments = new TextDocuments();

export async function completionHandler(textDocumentPosition: TextDocumentPositionParams): Promise<CompletionList>
{
	var text = documents.get(textDocumentPosition.textDocument.uri).getText();
	let lineStartPosition: Position = Position.create(textDocumentPosition.position.line, 0);
	var startPos = documents.get(textDocumentPosition.textDocument.uri).offsetAt(lineStartPosition);
	var endPos = documents.get(textDocumentPosition.textDocument.uri).offsetAt(textDocumentPosition.position);
	var line = text.substring(startPos, endPos);
	const regEx = /((@[^ \r\n>]+)$)|((<xref:[^ \r\n>]+)$)/g;
	let match = regEx.exec(line);
	let uid: string;
	if(match != null)
	{
		if(match[0][0] == '@')
		{
			uid = match[0].substr(1);
		} 
		else
		{
			uid = match[0].substr(6);
		}
	}
	
	if(completionItem == undefined) {
		completionItem = await requestUidController.getCompletionItem(uid);
	} else {
		let len = completionItem.length;
		completionItem = completionItem.filter(item => item.label.includes(uid));
		if(completionItem.length == 0 || completionItem.length < len * 0.6) {
			completionItem = await requestUidController.getCompletionItem(uid);
		}
	}

	let completionItemList = CompletionList.create(completionItem, true);
	return completionItemList;
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
		let uid;
		if(text[match.index] == '@') {
			uid = text.substr(match.index + 1, match[0].length - 1);
		} else {
			uid = text.substr(match.index + 6, match[0].length - 7);
		}
        let xrefSpecs = await requestUidController.getData(uid);
        if(xrefSpecs != undefined && xrefSpecs.length > 0 && xrefSpecs[0].uid == uid)
        {
            let documentLink: DocumentLink = DocumentLink.create(Range.create(startPos, endPos), xrefSpecs[0].href);
		    documentLinks.push(documentLink);
        }
	}
	return documentLinks;
}