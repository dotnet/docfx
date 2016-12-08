---
uid: graph.windows.net/myorganization/Contacts/1.0/update contact
parameters:
    - name: object_id
      description: The new object_id description
    - name: bodyparam
      description: The new bodyparam description
      schema:
        properties:
          objectType:
            description: this is overwrite objectType description
          provisioningErrors:
            items:
              schema:
                properties:
                  errorDetail:
                    readOnly: false
                    description: this is overwrite errorDetail description
---

---
uid: graph.windows.net/myorganization/Contacts/1.0/get contact memberOf links
parameters:
    - name: bodyparam
      description: The new bodyparam description
      schema:
        allOf:
          - null
          - description: this is second overwrite allOf description
            properties:
              location:
                description: this is overwrite location description
          - properties:
              level:
                description: this is overwrite level description
                enum:
                  - Verbose
                  - Info
                  - Warning
---