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