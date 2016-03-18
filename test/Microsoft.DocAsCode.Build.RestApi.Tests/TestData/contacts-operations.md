---
uid: graph.windows.net/myorganization/Contacts
title: Azure AD Graph API Operations on Contacts
ms.TocTitle: Operations on contacts
ms.ContentId: 477161a7-aebf-4d4b-981d-fcbc148e25c1
ms.topic: reference (API)
ms.date: 01/26/2016
_displayItems:
  - graph.windows.net/myorganization/Contacts/1.0/get contacts
  - graph.windows.net/myorganization/Contacts/1.0/get contact by id
  - graph.windows.net/myorganization/Contacts/1.0/update contact
  - graph.windows.net/myorganization/Contacts/1.0/delete contact
  - graph.windows.net/myorganization/Contacts/1.0/get contact manager link
  - graph.windows.net/myorganization/Contacts/1.0/update contact manager
  - graph.windows.net/myorganization/Contacts/1.0/get contact direct reports links
  - graph.windows.net/myorganization/Contacts/1.0/get contact memberOf links
---

# Operations on contacts | Graph API reference


 _**Applies to:** Graph API | Azure Active Directory_


<a id="Overview"> </a>
This topic discusses how to perform operations on organizational contacts using the Azure Active Directory (AD) Graph API. Organizational contacts typically represent users who are external to your company or organization. They are different from O365 Outlook personal contacts. With the Azure AD Graph API, you can read organizational contacts and their relationships to other directory objects, such as their manager, direct reports, and group memberships. The ability to write organizational contacts is limited to those contacts that are not currently synced from an on-premises directory (the **dirSyncEnabled** property is  **null** or **false**). For such contacts, you can update or delete the contact itself or its manager property. You cannot create organizational contacts with the Graph API. For more information about organizational contacts, including how they are created in your tenant, see [Contact].

The Graph API is a REST-based API that provides programmatic access to directory objects in Azure Active Directory, such as users, groups, organizational contacts, and applications.

> [!IMPORTANT]
> Azure AD Graph API functionality is also available through [Microsoft Graph](https://graph.microsoft.io/), a unified API that also includes APIs from other Microsoft services like Outlook, OneDrive, OneNote, Planner, and Office Graph, all accessed through a single endpoint with a single access token.

## Performing REST operations on contacts

To perform operations on organizational contacts with the Graph API, you send HTTP requests with a supported method (GET, POST, PATCH, PUT, or DELETE) to an endpoint that targets the contacts resource collection, a specific contact, a navigation property of a contact, or a function or action that can be called on a contact.

Graph API requests use the following basic URL:
```no-highlight
https://graph.windows.net/{tenant_id}/{resource_path}?{api_version}[odata_query_parameters]
```

> [!IMPORTANT]
> Requests sent to the Graph API must be well-formed, target a valid endpoint and version of the Graph API, and carry a valid access token obtained from Azure AD in their `Authorization` header. For more detailed information about creating requests and receiving responses with the Graph API, see [Operations Overview].

You specify the `{resource_path}` differently depending on whether you are targeting the collection of all contacts in your tenant, an individual contact, or a navigation property of a specific contact.

- `/contacts` targets the contact resource collection. You can use this resource path to read all contacts or a filtered list of contacts in your tenant.
- `/contacts/{object_id}` targets an individual contact in your tenant. You specify the target contact with its object ID (GUID). You can use this resource path to get the declared properties of a contact. For contacts that are not synced from an on-premises directory, you can use this resource path to modify the declared properties of a contact, or to delete a contact.
- `/contacts/{object_id}/{nav_property}` targets the specified navigation property of a contact. You can use it to return the object or objects referenced by the target navigation property of the specified contact. **Note**: This form of addressing is only available for reads.
- `/contacts/{object_id}/$links/{nav_property}` targets the specified navigation property of a contact. You can use this form of addressing to both read and modify a navigation property. On reads, the objects referenced by the property are returned as one or more links in the response body. Only the manager navigation property of contacts that are not synced from an on-premises directory can be modified. The manager is  specified as a link in the request body.

For example, the following request returns a link to the specified contact's manager:

```no-highlight
GET https://graph.windows.net/myorganization/contacts/a2fb3752-08b4-413d-af6f-1d99c4c131d9/$links/manager?api-version=1.6
```

## Basic operations on contacts <a id="BasicContactOperations"> </a>
You can perform read operations on contacts by targeting either the contact resource collection or a specific contact. You can update and delete contacts that are not synced from an on-premises directory by targeting a specific contact. The Graph API does not support creating contacts. Nor does it support updating or deleting contacts that are synced from an on-premises directory (the **dirSyncEnabled** property is **true**). The following topics show you how to perform basic operations on contacts.

****

---
uid: graph.windows.net/myorganization/Contacts/1.0/get contacts
codeGenerator: true
---

### Get contacts <a id="GetContacts"> </a>
Gets a collection of contacts. You can add OData query parameters to the request to filter, sort, and page the response. For more information, see [Supported Queries, Filters, and Paging Options].

On success, returns a collection of [Contact] objects; otherwise, the response body contains error details. For more information about errors, see [Error Codes and Error Handling].

```RESTAPIdocs
{
    "api":  "Contacts",
    "operation":    "get contacts",
     "showComponents": {
        "codeGenerator": "true"
    }
}
```

****

---
uid: graph.windows.net/myorganization/Contacts/1.0/get contact by id
codeGenerator: true
---

###Get a contact <a id="GetAContact"> </a>

Gets a specified contact. You use the object ID (GUID) to identify the target contact.

On success, returns the [Contact] object for the specified contact; otherwise, the response body contains error details. For more information about errors, see [Error Codes and Error Handling].

```RESTAPIdocs
{
    "api":  "Contacts",
    "operation":    "get contact by id",
     "showComponents": {
        "codeGenerator":    "true"
    }
}
```

****
---
uid: graph.windows.net/myorganization/Contacts/1.0/update contact
---

### Update a contact <a id="UpdateContact"> </a>

Update a contact's properties. Specify any writable [Contact] property in the request body. Only the properties that you specify are changed. You can  update only contacts that are not synced from an on-premises directory (the **dirSyncEnabled** property is **null** or **false**).

On success, no response body is returned; otherwise, the response body contains error details. For more information about errors, see [Error Codes and Error Handling].

```RESTAPIdocs
{
    "api":  "Contacts",
    "operation":    "update contact"
}
```
****
---
uid: graph.windows.net/myorganization/Contacts/1.0/delete contact
---
### Delete a contact <a id="DeleteContact"> </a>

Deletes a contact. You can delete only contacts that are not synced from an on-premises directory (the **dirSyncEnabled** property is **null** or  **false**).

On success, no response body is returned; otherwise, the response body contains error details. For more information about errors, see [Error Codes and Error Handling].

```RESTAPIdocs
{
    "api":  "Contacts",
    "operation":    "delete contact"
}
```

****

## Operations on contact navigation properties <a id="ContactNavigationOps"> </a>

Relationships between a contact and other objects in the directory such as the contact's manager, direct group memberships, and direct reports are exposed through navigation properties. You can read and, in some cases, modify these relationships by targeting these navigation properties in your requests.

---
uid: graph.windows.net/myorganization/Contacts/1.0/get contact manager link
codeGenerator: true
---

### Get a contact's manager <a id="GetContactsManager"> </a>

Gets the contact's manager from the **manager** navigation property.

On success, returns a link to the [User] or [Contact] assigned as the contact's manager; otherwise, the response body contains error details. For more information about errors, see [Error Codes and Error Handling].

**Note**: You can remove the "$links" segment from the URL to return the [User] or [Contact] object instead of a link.

```RESTAPIdocs
{
    "api":  "Contacts",
    "operation":    "get contact manager link",
     "showComponents": {
        "codeGenerator":    "true",
    }
}
```

****

---
uid: graph.windows.net/myorganization/Contacts/1.0/update contact manager
---
### Assign a contact's manager <a id="AssignContactsManager"> </a>

Assigns a contact's manager through the **manager** property. Either a user or a contact may be assigned. The request body contains a link to the [User] or [Contact] to assign. You can update the manager only for contacts that are not synced from an on-premises directory (the **dirSyncEnabled** property is **null** or **false**).

On success, no response body is returned; otherwise, the response body contains error details. For more information about errors, see [Error Codes and Error Handling].

```RESTAPIdocs
{
    "api":  "Contacts",
    "operation":    "update contact manager"
}
```

****
---
uid: graph.windows.net/myorganization/Contacts/1.0/get contact direct reports links
codeGenerator: true
---
### Get a contact's direct reports <a id="GetContactsDirectReports"> </a>

Gets the contact's direct reports from the **directReports** navigation property.

On success, returns a collection of links to the [User]'s and [Contact]'s for whom this contact is assigned as manager; otherwise, the response body contains error details. For more information about errors, see [Error Codes and Error Handling].

**Note**: You can remove the "$links" segment from the URL to return  [DirectoryObject]s for the users and contacts instead of links.

```RESTAPIdocs
{
    "api":  "Contacts",
    "operation":    "get contact direct reports links",
     "showComponents": {
        "codeGenerator":    "true"
    }
}
```

****
---
uid: graph.windows.net/myorganization/Contacts/1.0/get contact memberOf links
codeGenerator: true
---
### Get a contact's group memberships <a id="GetContactsMemberships"> </a>

Gets the contact's group  memberships from the **memberOf** navigation property.

This property returns only groups that the contact is a direct member of. To get all of the groups that the contact has direct or transitive membership in, call the [getMemberGroups] function.

On success, returns a collection of links to the [Group]s that this contact is a member of; otherwise, the response body contains error details. For more information about errors, see [Error Codes and Error Handling].

**Note**: You can remove the "$links" segment from the URL to return the [DirectoryObject]s for the groups instead of links.

```RESTAPIdocs
{
    "api":  "Contacts",
    "operation":    "get contact memberOf links",
     "showComponents": {
        "codeGenerator":    "true"
    }
}
```

****
## Functions and actions on contacts <a id="ContactFunctions"> </a>
You can call any of the following functions on a contact.

### Check membership in a specific group (transitive)
You can call the [isMemberOf] function to check for membership in a specific group. The check is transitive.

### Check membership in a list of groups (transitive)
You can call the [checkMemberGroups] function to check for membership in a list of groups. The check is transitive.

### Get all group memberships (transitive)
You can call the [getMemberGroups] function to return all the groups that the contact is a member of. The check is transitive, unlike reading the [memberOf](#GetContactsMemberships) navigation property, which returns only the groups that the contact is a direct member of.

****

##Additional Resources

- Learn more about Graph API supported features, capabilities, and preview features in [Graph API concepts](../howto/azure-ad-graph-api-operations-overview.md)


[Application]: ./entity-and-complex-type-reference.md#ApplicationEntity
[AppRoleAssignment]: ./entity-and-complex-type-reference.md#AppRoleAssignmentEntity
[Contact]: ./entity-and-complex-type-reference.md#ContactEntity
[Contract]: ./entity-and-complex-type-reference.md#ContractEntity
[Device]: ./entity-and-complex-type-reference.md#DeviceEntity
[DirectoryLinkChange]: ./entity-and-complex-type-reference.md#DirectoryLinkChangeEntity
[DirectoryObject]: ./entity-and-complex-type-reference.md#DirectoryObjectEntity
[DirectoryRole]: ./entity-and-complex-type-reference.md#DirectoryRoleEntity
[DirectoryRoleTemplate]: ./entity-and-complex-type-reference.md#DirectoryRoleTemplateEntity
[Domain (preview)]: ./entity-and-complex-type-reference.md#DomainEntity
[DomainDnsRecord]: ./entity-and-complex-type-reference.md#DomainDnsRecordEntity
[DomainDnsCnameRecord]: ./entity-and-complex-type-reference.md#DomainDnsCnameRecordEntity
[DomainDnsMxRecord]: ./entity-and-complex-type-reference.md#DomainDnsMxRecordEntity
[DomainDnsSrvRecord]: ./entity-and-complex-type-reference.md#DomainDnsSrvRecordEntity
[DomainDnsTxtRecord]: ./entity-and-complex-type-reference.md#DomainDnsTxtRecordEntity
[DomainDnsUnavailableRecord]: ./entity-and-complex-type-reference.md#DomainDnsUnavailableRecordEntity
[ExtensionProperty]: ./entity-and-complex-type-reference.md#ExtensionPropertyEntity
[Group]: ./entity-and-complex-type-reference.md#GroupEntity
[OAuth2PermissionGrant]: ./entity-and-complex-type-reference.md#OAuth2PermissionGrantEntity
[ServicePrincipal]: ./entity-and-complex-type-reference.md#ServicePrincipalEntity
[SubscribedSku]: ./entity-and-complex-type-reference.md#SubscribedSkuEntity
[TenantDetail]: ./entity-and-complex-type-reference.md#TenantDetailEntity
[User]: ./entity-and-complex-type-reference.md#UserEntity

[AlternativeSecurityId]: ./entity-and-complex-type-reference.md#AlternativeSecurityIdType
[AppRole]: ./entity-and-complex-type-reference.md#AppRoleType
[AssignedLicense]: ./entity-and-complex-type-reference.md#AssignedLicenseType
[AssignedPlan]: ./entity-and-complex-type-reference.md#AssignedPlanType
[KeyCredential]: ./entity-and-complex-type-reference.md#KeyCredentialType
[LicenseUnitsDetail]: ./entity-and-complex-type-reference.md#LicenseUnitsDetailType
[OAuth2Permission]: ./entity-and-complex-type-reference.md#OAuth2PermissionType
[PasswordCredential]: ./entity-and-complex-type-reference.md#PasswordCredentialType
[PasswordProfile]: ./entity-and-complex-type-reference.md#PasswordProfileType
[ProvisionedPlan]: ./entity-and-complex-type-reference.md#ProvisionedPlanType
[ProvisioningError]: ./entity-and-complex-type-reference.md#ProvisioningErrorType
[RequiredResourceAccess]: ./entity-and-complex-type-reference.md#RequiredResourceAccessType
[ResourceAccess]: ./entity-and-complex-type-reference.md#ResourceAccessType
[ServicePlanInfo]: ./entity-and-complex-type-reference.md#ServicePlanInfoType
[ServicePrincipalAuthenticationPolicy]: ./entity-and-complex-type-reference.md#ServicePrincipalAuthenticationPolicyType
[VerifiedDomain]: ./entity-and-complex-type-reference.md#VerifiedDomainType

[assignLicense]: ./functions-and-actions.md#assignLicense
[checkMemberGroups]: ./functions-and-actions.md#checkMemberGroups
[getAvailableExtensionProperties]: ./functions-and-actions.md#getAvailableExtensionProperties
[getMemberGroups]: ./functions-and-actions.md#getMemberGroups
[getMemberObjects]: ./functions-and-actions.md#getMemberObjects
[getObjectsByObjectIds]: ./functions-and-actions.md#getObjectsByObjectIds
[isMemberOf]: ./functions-and-actions.md#isMemberOf
[restore]: ./functions-and-actions.md#restore
[verify (preview)]: ./functions-and-actions.md#verify

[Graph API QuickStart on Azure on Azure.com]: https://azure.microsoft.com/documentation/articles/active-directory-graph-api-quickstart/
[Operations Overview]: ../howto/azure-ad-graph-api-operations-overview.md
[Graph API Versioning]: ../howto/azure-ad-graph-api-versioning.md
[Graph API Permission Scopes]: ../howto/azure-ad-graph-api-permission-scopes.md
[Supported Queries, Filters, and Paging Options]: ../howto/azure-ad-graph-api-supported-queries-filters-and-paging-options.md
[Differential Query]: ../howto/azure-ad-graph-api-differential-query.md
[Batch Processing]: ../howto/azure-ad-graph-api-batch-processing.md
[Directory Schema Extensions]: ../howto/azure-ad-graph-api-directory-schema-extensions.md
[Error Codes and Error Handling]: ../howto/azure-ad-graph-api-error-codes-and-error-handling.md
[Azure AD Administrative Units Preview]: ../howto/azure-ad-administrative-units-preview.md
[Azure AD Reports and Events Preview]: ../howto/azure-ad-reports-and-events-preview.md

[!CODE-RESTAPI_Swagger [Contacts_swagger2](./contacts_swagger2.json)]
