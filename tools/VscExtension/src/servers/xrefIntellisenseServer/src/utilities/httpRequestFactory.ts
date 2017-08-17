import axios from 'axios';

export class httpRequestFactory {

    private static _client = axios.create({
        //baseURL: 'http://restfulapiwebservice0627.azurewebsites.net/',
        headers: { 'Content-type': 'application/json', 'Accept-type': 'application/json' }
    });

    static async getUids(url: string, uid: string) {
        //var data;
        let promise = this._client.get(url + uid + "/");
        let response = await promise;
        //= 
        //console.log(response.data);
        let data = await response.data;
        return data;
        
    }

}