// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference.Tests
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Build.JavaScriptReference;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Common.Git;
    using Microsoft.DocAsCode.DataContracts.Common;

    using AutoMapper;
    using Xunit;

    [Trait("Owner", "renzeyu")]
    [Trait("EntityType", "JavaScriptDocumentProcessor")]
    public class JavaScriptAutoMapperTest
    {
        private static readonly string[] SupportedLanguages = { Constants.DevLang.JavaScript };

        [Fact]
        public void MapToApiReferenceBuildOutputShouldSucceed()
        {
            // Arrange
            var reference = new ReferenceViewModel
            {
                Uid = "uid",
                Parent = "parent",
                Definition = "definition",
                IsExternal = false,
                Href = "href",
                Name = "name",
                NameWithType = "nameWithType",
                FullName = "fullName"
            };
            reference.Additional["metaKey"] = "metaValue";

            var expected = new ApiReferenceBuildOutput
            {
                Uid = "uid",
                Parent = "parent",
                Definition = "definition",
                IsExternal = false,
                Href = "href",
                Name = new List<ApiLanguageValuePair>()
                {
                    new ApiLanguageValuePair()
                    {
                        Language = Constants.DevLang.JavaScript,
                        Value = "name"
                    }
                },
                NameWithType = new List<ApiLanguageValuePair>
                {
                    new ApiLanguageValuePair
                    {
                        Language = Constants.DevLang.JavaScript,
                        Value = "nameWithType"
                    }
                },
                FullName = new List<ApiLanguageValuePair>
                {
                    new ApiLanguageValuePair
                    {
                        Language = Constants.DevLang.JavaScript,
                        Value = "fullName"
                    }
                },
                Metadata = { ["metaKey"] = "metaValue" },
            };

            // Act
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile(new ApiReferenceBuildOutputProfile(SupportedLanguages));
            });
            Mapper.Configuration.AssertConfigurationIsValid();
            var dto = Mapper.Map<ReferenceViewModel, ApiReferenceBuildOutput>(reference);

            // Assert
            Assert.Equal(JsonUtility.Serialize(expected), JsonUtility.Serialize(dto));
        }


        [Fact]
        public void MapToApiBuildOutputWithoutReferenceShouldSucceed()
        {
            // Arrange
            var input = new ItemViewModel
            {
                Uid = "uid",
                Parent = "parent",
                Href = "href",
                Name = "name",
                NameWithType = "nameWithType",
                FullName = "fullName",
                Type = MemberType.Class,
                Source = new SourceDetail
                {
                    Remote = new GitDetail
                    {
                        RelativePath = "sourcePath"
                    },
                    BasePath = "sourceBasePath",
                    StartLine = 0,
                    EndLine = 0
                },
                Documentation = new SourceDetail
                {
                    Remote = new GitDetail
                    {
                        RelativePath = "documentPath"
                    },
                    BasePath = "documentBasePath",
                    StartLine = 0,
                    EndLine = 0
                },
                PackageNameList = new List<string> { "package" },
                NamespaceName = "namespace",
                Summary = "summary",
                Remarks = "remarks",
                Examples = new List<string> { "example" },
                Syntax = new SyntaxDetailViewModel
                {
                    Content = "syntax",
                    Parameters = new List<ApiParameter>
                    {
                        new ApiParameter
                        {
                            Description = "this is a param",
                            Type = new List<string> {"string"},
                            Optional = false
                        }
                    }
                },
                Overridden = "overridden",
                Exceptions = new List<ExceptionInfo>
                {
                    new ExceptionInfo
                    {
                        Type = "exceptionType",
                        Description = "exceptionDescription"
                    }
                },
                SeeAlsos = new List<LinkInfo>
                {
                    new LinkInfo
                    {
                        LinkType = LinkType.HRef,
                        LinkId = "linkid.com",
                        AltText = "linkText"
                    },
                    new LinkInfo
                    {
                        LinkType = LinkType.CRef,
                        LinkId = "type"
                    }
                },
                Inheritance = new List<string> { "inheritance" },
                Metadata = { ["metaKey"] = "metaValue" }
            };
            var expected = new ApiBuildOutput
            {
                Uid = "uid",
                Parent = new ApiReferenceBuildOutput
                {
                    Spec = new List<ApiLanguageValuePair>
                    {
                        new ApiLanguageValuePair
                        {
                            Language = Constants.DevLang.JavaScript,
                            Value = "parent"
                        }
                    }
                },
                Href = "href",
                Name = new List<ApiLanguageValuePair>
                {
                    new ApiLanguageValuePair
                    {
                        Language = Constants.DevLang.JavaScript,
                        Value = "name"
                    }
                },
                NameWithType = new List<ApiLanguageValuePair>
                {
                    new ApiLanguageValuePair
                    {
                        Language = Constants.DevLang.JavaScript,
                        Value = "nameWithType"
                    }
                },
                FullName = new List<ApiLanguageValuePair>
                {
                    new ApiLanguageValuePair
                    {
                        Language = Constants.DevLang.JavaScript,
                        Value = "fullName"
                    }
                },
                Type = MemberType.Class,
                Source = new SourceDetail
                {
                    Remote = new GitDetail
                    {
                        RelativePath = "sourcePath"
                    },
                    BasePath = "sourceBasePath",
                    StartLine = 0,
                    EndLine = 0
                },
                Documentation = new SourceDetail
                {
                    Remote = new GitDetail
                    {
                        RelativePath = "documentPath"
                    },
                    BasePath = "documentBasePath",
                    StartLine = 0,
                    EndLine = 0
                },
                PackageNameList = new List<string> { "package" },
                NamespaceName = new ApiReferenceBuildOutput
                {
                    Spec = new List<ApiLanguageValuePair>
                    {
                        new ApiLanguageValuePair
                        {
                            Language = Constants.DevLang.JavaScript,
                            Value = "namespace"
                        }
                    }
                },
                Summary = "summary",
                Remarks = "remarks",
                Examples = new List<string> { "example" },
                Syntax = new ApiSyntaxBuildOutput
                {
                    Content = new List<ApiLanguageValuePair>
                    {
                        new ApiLanguageValuePair
                        {
                            Language = Constants.DevLang.JavaScript,
                            Value = "syntax"
                        }
                    },
                    Parameters = new List<ApiParameterBuildOutput>
                    {
                        new ApiParameterBuildOutput
                        {
                            Description = "this is a param",
                            Type = new List<ApiReferenceBuildOutput>
                            {
                                new ApiReferenceBuildOutput
                                {
                                    Spec = new List<ApiLanguageValuePair>
                                    {
                                        new ApiLanguageValuePair
                                        {
                                            Language = Constants.DevLang.JavaScript,
                                            Value = "string"
                                        }
                                    }
                                }
                            },
                            Optional = false
                        }
                    }
                },
                Overridden = new ApiReferenceBuildOutput
                {
                    Spec = new List<ApiLanguageValuePair>
                    {
                        new ApiLanguageValuePair
                        {
                            Language = Constants.DevLang.JavaScript,
                            Value = "overridden"
                        }
                    }
                },
                Exceptions = new List<ApiExceptionInfoBuildOutput>
                {
                    new ApiExceptionInfoBuildOutput
                    {
                        Type = new ApiReferenceBuildOutput
                        {
                            Spec = new List<ApiLanguageValuePair>
                            {
                                new ApiLanguageValuePair
                                {
                                    Language = Constants.DevLang.JavaScript,
                                    Value = "exceptionType"
                                }
                            }
                        },
                        Description = "exceptionDescription"
                    }
                },
                SeeAlsos = new List<ApiLinkInfoBuildOutput>
                {
                    new ApiLinkInfoBuildOutput
                    {
                        LinkType = LinkType.HRef,
                        Url = "<span><a href=\"linkid.com\">linkText</a></span>"
                    },
                    new ApiLinkInfoBuildOutput
                    {
                        LinkType = LinkType.CRef,
                        Type = new ApiReferenceBuildOutput
                        {
                            Spec = new List<ApiLanguageValuePair>
                            {
                                new ApiLanguageValuePair
                                {
                                    Language = Constants.DevLang.JavaScript,
                                    Value = "type"
                                }
                            }
                        }
                    }
                },
                Inheritance = new List<ApiReferenceBuildOutput>
                {
                    new ApiReferenceBuildOutput
                    {
                        Spec = new List<ApiLanguageValuePair>
                        {
                            new ApiLanguageValuePair
                            {
                                Language = Constants.DevLang.JavaScript,
                                Value = "inheritance"
                            }
                        }
                    }
                },
                Metadata = { ["metaKey"] = "metaValue" }
            };

            // Act
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile(new ApiBuildOutputProfile(SupportedLanguages));
            });
            Mapper.Configuration.AssertConfigurationIsValid();
            var dto = Mapper.Map<ItemViewModel, ApiBuildOutput>(input);

            // Assert
            Assert.Equal(JsonUtility.Serialize(expected), JsonUtility.Serialize(dto));
        }

        [Fact]
        public void MapToApiBuildOutputWithReferenceShouldSucceed()
        {
            // Arrange
            var input = new PageViewModel
            {
                Items = new List<ItemViewModel>
                {
                    new ItemViewModel
                    {
                        Uid = "KeyVaultClient",
                        Id = "KeyVaultClient",
                        Name = "KeyVaultClient",
                        Summary = "class summary",
                        FullName = "KeyVaultClient",
                        Type = MemberType.Class,
                        Children = new List<string>
                        {
                            "KeyVaultClient.#ctor"
                        }
                    },
                    new ItemViewModel
                    {
                        Uid = "KeyVaultClient.#ctor",
                        Id = "KeyVaultClient.#ctor",
                        Parent = "KeyVaultClient",
                        Name = "KeyVaultClient(credentials, options)",
                        FullName = "KeyVaultClient.KeyVaultClient(credentials, options)",
                        Summary = "Initializes a new instance of the KeyVaultClient class",
                        Type = MemberType.Constructor,
                        Syntax = new SyntaxDetailViewModel
                        {
                            Parameters = new List<ApiParameter>
                            {
                                new ApiParameter
                                {
                                    Name = "credentials",
                                    Type = new List<string>
                                    {
                                        "credentials"
                                    },
                                    Description = "Credentials needed for the client to connect to Azure."
                                },
                                new ApiParameter
                                {
                                    Name = "options",
                                    Type = new List<string>
                                    {
                                        "Array"
                                    },
                                    Description = "The parameter options."
                                }
                            },
                            Content = "new KeyVaultClient(credentials, options)"
                        }
                    }
                },
                References = new List<ReferenceViewModel>
                {
                    new ReferenceViewModel
                    {
                        Uid = "credentials",
                        Name = "credentials",
                        FullName = "credentials",
                        IsExternal = true
                    },
                    new ReferenceViewModel
                    {
                        Uid = "Array",
                        Name = "Array",
                        FullName = "Array",
                        IsExternal = true
                    }
                }
            };
            var expected = new ApiBuildOutput
            {
                Uid = "KeyVaultClient",
                Name = new List<ApiLanguageValuePair>
                {
                    new ApiLanguageValuePair
                    {
                        Language = Constants.DevLang.JavaScript,
                        Value = "KeyVaultClient"
                    }
                },
                Summary = "class summary",
                FullName = new List<ApiLanguageValuePair>
                {
                    new ApiLanguageValuePair
                    {
                        Language = Constants.DevLang.JavaScript,
                        Value = "KeyVaultClient"
                    }
                },
                Type = MemberType.Class,
                Children = new List<ApiBuildOutput>
                {
                    new ApiBuildOutput
                    {
                        Uid = "KeyVaultClient.#ctor",
                        Parent = new ApiReferenceBuildOutput
                        {
                            Spec = new List<ApiLanguageValuePair>
                            {
                                new ApiLanguageValuePair
                                {
                                    Language = Constants.DevLang.JavaScript,
                                    Value = "KeyVaultClient",
                                }
                            }
                        },
                        Name = new List<ApiLanguageValuePair>
                        {
                            new ApiLanguageValuePair
                            {
                                Language = Constants.DevLang.JavaScript,
                                Value = "KeyVaultClient(credentials, options)"
                            }
                        },
                        FullName = new List<ApiLanguageValuePair>
                        {
                            new ApiLanguageValuePair
                            {
                                Language = Constants.DevLang.JavaScript,
                                Value = "KeyVaultClient.KeyVaultClient(credentials, options)"
                            }
                        },
                        Summary = "Initializes a new instance of the KeyVaultClient class",
                        Type = MemberType.Constructor,
                        Syntax = new ApiSyntaxBuildOutput
                        {
                            Parameters = new List<ApiParameterBuildOutput>
                            {
                                new ApiParameterBuildOutput
                                {
                                    Name = "credentials",
                                    Type = new List<ApiReferenceBuildOutput>
                                    {
                                        new ApiReferenceBuildOutput
                                        {
                                            Uid = "credentials",
                                            IsExternal = true,
                                            Name = new List<ApiLanguageValuePair>
                                            {
                                                new ApiLanguageValuePair
                                                {
                                                    Language = Constants.DevLang.JavaScript,
                                                    Value = "credentials"
                                                }
                                            },
                                            FullName = new List<ApiLanguageValuePair>
                                            {
                                                new ApiLanguageValuePair
                                                {
                                                    Language = Constants.DevLang.JavaScript,
                                                    Value = "credentials"
                                                }
                                            }
                                        }
                                    },
                                    Description = "Credentials needed for the client to connect to Azure."
                                },
                                new ApiParameterBuildOutput
                                {
                                    Name = "options",
                                    Type = new List<ApiReferenceBuildOutput>
                                    {
                                        new ApiReferenceBuildOutput
                                        {
                                            Uid = "Array",
                                            IsExternal = true,
                                            Name = new List<ApiLanguageValuePair>
                                            {
                                                new ApiLanguageValuePair
                                                {
                                                    Language = Constants.DevLang.JavaScript,
                                                    Value = "Array"
                                                }
                                            },
                                            FullName = new List<ApiLanguageValuePair>
                                            {
                                                new ApiLanguageValuePair
                                                {
                                                    Language = Constants.DevLang.JavaScript,
                                                    Value = "Array"
                                                }
                                            }
                                        }
                                    },
                                    Description = "The parameter options."
                                }
                            },
                            Content = new List<ApiLanguageValuePair>
                            {
                                new ApiLanguageValuePair
                                {
                                    Language = Constants.DevLang.JavaScript,
                                    Value = "new KeyVaultClient(credentials, options)"
                                }
                            }
                        }
                    }
                }
            };

            // Act
            var dto = input.ToApiBuildOutput();

            // Assert
            Assert.Equal(JsonUtility.Serialize(expected), JsonUtility.Serialize(dto));
        }
    }
}
