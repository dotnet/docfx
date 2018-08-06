# Schema Validation
Schema validation will be executed during SDP building. In v2 we used the Newtonsoft.Json.Schema package to validate the object against the json schema. The problem using the Newtonsoft.Json.Schema package is that the end user of docfx would need to purchase a license to be able to process the validation. So in v3, we decided to use [System.ComponentModel.DataAnnotations](https://msdn.microsoft.com/en-us/library/system.componentmodel.dataannotations(v=vs.110).aspx) which are attached to C# class to define the schema. 

## How it works
```
             parse           schema validation
JSON/YAML   ------->  JToken     ------->       C# object
                                deserialize
```
As described above, JSON/YAML will be parsed into JToken firstly, and then schema validation will be executed while deserializing from JToken to C# object.

Below are the attributes we use with the equivalent keywords in json schema. 

| Attribute | Equivalent Json Schema Keyword | Description | Namespace |
| - | - | - | - |
| MinLengthAttribute | minItems | An array or string instance is valid against if its size is greater than, or equal to, the value of this keyword. | System.ComponentModel.DataAnnotations |
| MaxLengthAttribute | maxItems | An array or string instance is valid against if its size is less than, or equal to, the value of this keyword. | System.ComponentModel.DataAnnotations |
| RegularExpressionAttribute | pattern | A string instance is considered valid if the regular expression matches the instance successfully. | System.ComponentModel.DataAnnotations |
| JsonRequiredAttribute | required | An object instance is valid against this keyword if every required property exists in the instance. Notice that we could not use `RequiredAttribute` here since we execute the validation for each json node with the defined validation attribute, if the required field is missing from json object, we would not execute the requried validation. So we use `JsonRequriedAttribute` from Newtonsoft.Json, which would take effect during the deserialization. | Newtonsoft.Json |
| JsonExtensionDataAttribute | additionalProperties | If no property in the type contains `JsonExtensionDataAttribute`, additional properties are not allowed and unknown field warning will be added if any found in the instance. Otherwise, no warning. | Newtonsoft.Json |

## Example
Here comes an example how the C# class looks like:
```csharp
public class SomeClass
{
    [RegularExpression("[a-z]")]
    public string RegPatternValue { get; set; }
    [MinLength(2), MaxLength(3)]
    public string ValueWithLengthRestriction { get; set; }
    [MinLength(1), MaxLength(3)]
    public List<string> ListValueWithLengthRestriction { get; set; }
    [JsonRequired]
    public string ValueRequired { get; set; }
}
```
And how the equivalent schema json looks like:
```json
{
    "type": [
        "object",
        "null"
    ],
    "properties": {
        "RegPatternValue": {
            "type": "string",
            "pattern": "[a-z]"
        },
        "ValueWithLengthRestriction": {
            "type": "string",
            "minLength": 2,
            "maxLength": 3
        },
        "ListValueWithLengthRestriction": {
            "type": "array",
            "items": {
                "type": "string"
            },
            "minItems": 1,
            "maxItems": 3
        },
        "ValueRequired": {
            "type": "string"
        }
    },
    "required": [
        "ValueRequired"
    ]
}
```
