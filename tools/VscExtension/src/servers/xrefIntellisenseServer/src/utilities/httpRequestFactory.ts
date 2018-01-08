import axios from 'axios';

export class httpRequestFactory {

    public static isDocfxProject: boolean = false;
    public static xrefService: string[] = [];
    private static _client = axios.create({
        headers: { 'Content-type': 'application/json', 'Accept-type': 'application/json' }
    });

    public static async getUids(uid: string) {
        if(httpRequestFactory.xrefService != undefined)
        {
            for(var i=0; i < httpRequestFactory.xrefService.length; i++)
            {   
                try {
                    let promise = await this._client.get(httpRequestFactory.xrefService[i] + uid);
                    let data = promise.data;
                    if(data != undefined) return data;
                } catch (error) {
                    console.log(error);
                }
            }
        }
        return [];
    }
}