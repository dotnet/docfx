# Credential

This document specifies how DocFX accesses private resources during restore/build/publish. 

The credential may be used to:
- clone a private repository
- download resource from a private blob (e.g. [GitHub user cache](github-user-cache.md))
- update a resource, also like GitHub user cache
- publish files

## How to authorize HTTP requests

In general, DocFX uses standard HTTP request methods to access resources, so that it does not care about the hosting details about the resources. 
- `GET`: to fetch resource
- `PUT`: to upload resource

DocFX need to embed the credential information in the HTTP request. Request header can be the right place.

### Request header

Request header can also contains credential in `Authorization` header. e.g.
```
Authorization: Bearer {token}
```
The value of secrets object can be an object to provide more details of the request header. It can be configured in `docfx.yml` like:
```yml
http:
  https://contoso.com/path/file:
    headers:
      Authorization: Bearer {token}
```

We can also put other required headers here. e.g. Azure Blob requires the `x-ms-blob-type` header when `PUT` a resource.

### Auto Renew 
Sometimes the credential could be expired during DocFX building.   
The credential handler acts as HTTP request middleware intercepts the failed HTTP response with 401 code and auto renew the credentials with max 3 retries.


## How to authorize other requests

There is other types of authorization, like:
- Call GitHub API with personal access token to resolve contributors.

In these cases, the configuration for these specific services can contain a `authToken` field to store the secret token, like:
``` yml
gitHub:
  authToken: {token}
```

## How to authorize in server build
On server side, there is a requirement for different docsets to use different credentials. Given we will not hard code tokens inside `docfx.yml` for security, usually it need a dedicate service to pass credentials.

Here is the steps:
1. Before build, the build system should check the identity of user.
2. Build system call the credential service to collect the necessary secrets, and stores into a partial docfx configure, like:
   ```yml
   http:
     {url}:
       ...
   gitHub:
     authToken: ...
   ```
3. Build system pass the secrets to DocFX to build docsets using them.

