import {CompletionItem, CompletionItemKind} from 'vscode-languageserver';
import {httpRequestFactory} from '../utilities/httpRequestFactory';

export class requestUidController {

    public static async getCompletionItem(partUid: string): Promise<CompletionItem[]> {
        let completionItems: CompletionItem[] = [];
        var xrefSpecs = await this.getData(partUid);
        xrefSpecs.forEach(element => {
            let completionItem: CompletionItem = CompletionItem.create(element.uid);
            completionItem.kind = element.type;
            completionItem.detail = element.href;
            completionItems.push(completionItem);
        });
        return completionItems;
    }

    public static async getData(uid:string):Promise<any[]> {
        let encodeUid = encodeURIComponent(uid);
        var data = await httpRequestFactory.getUids(encodeUid);
        return data;
	}
}