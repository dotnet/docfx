import {CompletionItem, CompletionItemKind} from 'vscode-languageserver';
import {httpRequestFactory} from '../utilities/httpRequestFactory';

export class requestUidController {

    static async getCompletionItem(partUid: string): Promise<CompletionItem[]> {
        let completionItems: CompletionItem[] = [];
        // var text = document.getText(document.lineAt(position).range);
        // var tx = text.substring(text.lastIndexOf("@")+1);
        // if(tx.length > 1 && tx[0] == ' '){
        //     var completionItems = [];
        // }
        var re = await this.getData('http://xrefservice0810.azurewebsites.net/intellisense/', partUid);
        //console.log(re);
        re.forEach( function (element) {
            let completionItem: CompletionItem = CompletionItem.create(element.uid);
            completionItem.kind = element.type;
            //completionItem.commitCharacters = ["c","s"];
            completionItem.detail = element.href;
            //completionItem.filterText = "bbb";
            //completionItem.insertText = new vscode.SnippetString(element);
            completionItems.push(completionItem);
        });
        return completionItems;
    }

    static async getData(url:string, uid:string):Promise<any[]> {
        let encodeUid = encodeURIComponent(uid);
        var data = await httpRequestFactory.getUids(url, encodeUid);
        return data;
	}
}