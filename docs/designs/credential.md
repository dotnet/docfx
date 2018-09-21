# Credential

This document specifies how DocFX accesses private resoruces during restore/build/publish. 

The credential may be used to:
- clone a private repository
- download resource from a private blob (e.g. [GitHub user cache](github-user-cache.md))
- update a resource, also like GitHub user cache
- publish files

## How to authorize HTTP requests

In general, DocFX uses standard HTTP request methods to access resources, so that it does not care about the hosting details about the resources. 
- `GET`: to fetch resource
- `PUT`: to upload resource

DocFX need to embed the credential information in the HTTP request. URL query and request header can be the right place.

### URL query

URL can embed the credential in itself query string. e.g.
```
https://contoso.com/path/file?sig={token}
```

It can be configured in `docfx.yml`:
```yml
referenceToAResource: "https://contoso.com/path/file"
http:
  secrets:
    "https://contoso.com/path/file": "?sig={token}"
```
The value of secrets object can be a simple string, which indicates the param string. 

In this case, we can also embed the token into URL like `referenceToAResource: "https://contoso.com/path/file?sig={token}"`. However, the config cannot be public as it contains secret information. Have a separate `secrets` part can help store them in more private place, like a local machine.

### Request header

Request header can also contains credential in `Authorization` header. e.g.
```
Authorization: Bearer {token}
```
The value of secrets object can be an object to provide more details of the request header. It can be configured in `docfx.yml` like:
```yml
http:
  secrets:
  - baseUrl: "https://contoso.com/path/file":
    query: "?sig={token}"
    headers:
      Authorization: Bearer {token}
```

We can also put other required headers here. e.g. Azure Blob requires the `x-ms-blob-type` header when `PUT` a resource.

## How to authorize non-HTTP requests

There is other types of authorization, like:
- Clone a repository from GitHub/Azure DevOps/GitLab... with personal access token.
- Call GitHub API with personal access token to resolve contributors.

In these cases, the configuration for these specific services can contain a `authToken` field to store the secret token, like:
``` yml
git:
  authToken: {token}
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
     secrets:
       ...
   git:
     authToken: ...
   gitHub:
     authToken: ...
   ```
3. Build system pass the secrets to DocFX to build docsets using them.