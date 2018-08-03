# Schema Validation
Schema validation will be executed during json/yaml deserialization, we use C# class to define the schema. Below are the attributes we use with the equivalent keywords in json schema.

| Attribute | Equivalent Json Schema Keyword | Description | Namespace |
| - | - | - | - |
| MinLengthAttribute | minItems | An array or string instance is valid against if its size is greater than, or equal to, the value of this keyword. | System.ComponentModel.DataAnnotations |
| MaxLengthAttribute | maxItems | An array or string instance is valid against if its size is less than, or equal to, the value of this keyword. | System.ComponentModel.DataAnnotations |
| RegularExpressionAttribute | pattern | A string instance is considered valid if the regular expression matches the instance successfully. | System.ComponentModel.DataAnnotations |
| JsonRequiredAttribute(Not RequiredAttribute) | required | An object instance is valid against this keyword if every required property exists in the instance. | Newtonsoft.Json |
| JsonExtensionDataAttribute | additionalProperties | If no property in the type contains `JsonExtensionDataAttribute`, additional properties are not allowed and unknown field warning will be added if any found in the instance. Otherwise, no warning. | Newtonsoft.Json |
