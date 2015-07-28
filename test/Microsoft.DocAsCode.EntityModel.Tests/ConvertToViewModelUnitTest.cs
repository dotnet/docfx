// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.BackEnd.Tests
{
    using EntityModel;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    [Trait("Owner", "vwxyzh")]
    [Trait("EntityType", "ViewModel")]
    public class ConvertToViewModelUnitTest
    {
        #region Data

        private readonly MetadataItem model = new MetadataItem
        {
            Type = MemberType.Assembly,
            Items = new List<MetadataItem>
            {
                new MetadataItem
                {
                    Name = "N1",
                    Type = MemberType.Namespace,
                    DisplayNames = new SortedList<SyntaxLanguage, string>
                    {
                        { SyntaxLanguage.CSharp, "N1" },
                        { SyntaxLanguage.VB, "N1" },
                    },
                    DisplayQualifiedNames = new SortedList<SyntaxLanguage, string>()
                    {
                        { SyntaxLanguage.CSharp, "N1" },
                        { SyntaxLanguage.VB, "N1" },
                    },
                    References = new Dictionary<string, ReferenceItem>
                    {
                        {
                            "N1.C1",
                            new ReferenceItem
                            {
                                IsDefinition = true,
                                Type = MemberType.Class,
                                Parent = "N1",
                                Summary = "Summary!",
                                Parts = new SortedList<SyntaxLanguage, List<LinkItem>>
                                {
                                    {
                                        SyntaxLanguage.CSharp,
                                        new List<LinkItem>
                                        {
                                            new LinkItem
                                            {
                                                DisplayName = "C1",
                                                DisplayQualifiedNames = "N1.C1",
                                                Name = "N1.C1",
                                                IsExternalPath = false,
                                                Href = "href!",
                                            }
                                        }
                                    },
                                    {
                                        SyntaxLanguage.VB,
                                        new List<LinkItem>
                                        {
                                            new LinkItem
                                            {
                                                DisplayName = "C1",
                                                DisplayQualifiedNames = "N1.C1",
                                                Name = "N1.C1",
                                                IsExternalPath = false,
                                                Href = "href!",
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    Items = new List<MetadataItem>
                    {
                        new MetadataItem
                        {
                            Name = "N1.C1",
                            Type = MemberType.Class,
                            DisplayNames = new SortedList<SyntaxLanguage, string>
                            {
                                { SyntaxLanguage.CSharp, "C1" },
                                { SyntaxLanguage.VB, "C1" },
                            },
                            DisplayQualifiedNames = new SortedList<SyntaxLanguage, string>()
                            {
                                { SyntaxLanguage.CSharp, "N1.C1" },
                                { SyntaxLanguage.VB, "N1.C1" },
                            },
                            Inheritance = new List<string>
                            {
                                "System.Object",
                                "System.Collections.Generic.List{System.Object}",
                            },
                            Implements = new List<string>
                            {
                                "System.Collections.Generic.IList{System.Object}",
                                "System.Collections.Generic.ICollection{System.Object}",
                                "System.Collections.Generic.IEnumerable{System.Object}",
                                "System.Collections.IEnumerable",
                            },
                            InheritedMembers = new List<string>
                            {
                                "System.Object.GetHashCode",
                            },
                            References = new Dictionary<string, ReferenceItem>
                            {
                                {
                                    "System",
                                    new ReferenceItem
                                    {
                                        IsDefinition = true,
                                        Parts = new SortedList<SyntaxLanguage, List<LinkItem>>
                                        {
                                            {
                                                SyntaxLanguage.CSharp,
                                                new List<LinkItem>
                                                {
                                                    new LinkItem
                                                    {
                                                        DisplayName = "System",
                                                        DisplayQualifiedNames= "System",
                                                        Name = "System",
                                                        IsExternalPath = true,
                                                    }
                                                }
                                            },
                                            {
                                                SyntaxLanguage.VB,
                                                new List<LinkItem>
                                                {
                                                    new LinkItem
                                                    {
                                                        DisplayName = "System",
                                                        DisplayQualifiedNames= "System",
                                                        Name = "System",
                                                        IsExternalPath = true,
                                                    }
                                                }
                                            },
                                        }
                                    }
                                },
                                {
                                    "System.Collections.Generic",
                                    new ReferenceItem
                                    {
                                        IsDefinition = true,
                                        Parts = new SortedList<SyntaxLanguage, List<LinkItem>>
                                        {
                                            {
                                                SyntaxLanguage.CSharp,
                                                new List<LinkItem>
                                                {
                                                    new LinkItem
                                                    {
                                                        DisplayName = "System.Collections.Generic",
                                                        DisplayQualifiedNames= "System.Collections.Generic",
                                                        Name = "System.Collections.Generic",
                                                        IsExternalPath = true,
                                                    }
                                                }
                                            },
                                            {
                                                SyntaxLanguage.VB,
                                                new List<LinkItem>
                                                {
                                                    new LinkItem
                                                    {
                                                        DisplayName = "System.Collections.Generic",
                                                        DisplayQualifiedNames= "System.Collections.Generic",
                                                        Name = "System.Collections.Generic",
                                                        IsExternalPath = true,
                                                    }
                                                }
                                            },
                                        }
                                    }
                                },
                                {
                                    "System.Object",
                                    new ReferenceItem
                                    {
                                        IsDefinition = true,
                                        Parent = "System",
                                        Parts = new SortedList<SyntaxLanguage, List<LinkItem>>
                                        {
                                            {
                                                SyntaxLanguage.CSharp,
                                                new List<LinkItem>
                                                {
                                                    new LinkItem
                                                    {
                                                        DisplayName = "Object",
                                                        DisplayQualifiedNames= "System.Object",
                                                        Name = "System.Object",
                                                        IsExternalPath = true,
                                                    }
                                                }
                                            },
                                            {
                                                SyntaxLanguage.VB,
                                                new List<LinkItem>
                                                {
                                                    new LinkItem
                                                    {
                                                        DisplayName = "Object",
                                                        DisplayQualifiedNames= "System.Object",
                                                        Name = "System.Object",
                                                        IsExternalPath = true,
                                                    }
                                                }
                                            },
                                        },
                                    }
                                },
                                {
                                    "System.Object.GetHashCode",
                                    new ReferenceItem
                                    {
                                        IsDefinition = true,
                                        Parent = "System.Object",
                                        Parts = new SortedList<SyntaxLanguage, List<LinkItem>>
                                        {
                                            {
                                                SyntaxLanguage.CSharp,
                                                new List<LinkItem>
                                                {
                                                    new LinkItem
                                                    {
                                                        DisplayName = "GetHashCode()",
                                                        DisplayQualifiedNames= "System.Object.GetHashCode()",
                                                        Name = "System.Object.GetHashCode",
                                                        IsExternalPath = true,
                                                    }
                                                }
                                            },
                                            {
                                                SyntaxLanguage.VB,
                                                new List<LinkItem>
                                                {
                                                    new LinkItem
                                                    {
                                                        DisplayName = "GetHashCode()",
                                                        DisplayQualifiedNames= "System.Object.GetHashCode()",
                                                        Name = "System.Object.GetHashCode",
                                                        IsExternalPath = true,
                                                    }
                                                }
                                            },
                                        },
                                    }
                                },
                                {
                                    "System.Collections.Generic.List`1",
                                    new ReferenceItem
                                    {
                                        IsDefinition = true,
                                        Parent = "System.Collections.Generic",
                                        Parts = new SortedList<SyntaxLanguage, List<LinkItem>>
                                        {
                                            {
                                                SyntaxLanguage.CSharp,
                                                new List<LinkItem>
                                                {
                                                    new LinkItem
                                                    {
                                                        DisplayName = "List<T>",
                                                        DisplayQualifiedNames = "System.Collections.Generic.List<T>",
                                                        Name = "System.Collections.Generic.List`1",
                                                        IsExternalPath = true,
                                                    }
                                                }
                                            },
                                            {
                                                SyntaxLanguage.VB,
                                                new List<LinkItem>
                                                {
                                                    new LinkItem
                                                    {
                                                        DisplayName = "List(Of T)",
                                                        DisplayQualifiedNames = "System.Collections.Generic.List(Of T)",
                                                        Name = "System.Collections.Generic.List`1",
                                                        IsExternalPath = true,
                                                    }
                                                }
                                            },
                                        },
                                    }
                                },
                                {
                                    "System.Collections.Generic.List{System.Object}",
                                    new ReferenceItem
                                    {
                                        IsDefinition = false,
                                        Definition = "System.Collections.Generic.List`1",
                                        Parent = "System.Collections.Generic",
                                        Parts = new SortedList<SyntaxLanguage, List<LinkItem>>
                                        {
                                            {
                                                SyntaxLanguage.CSharp,
                                                new List<LinkItem>
                                                {
                                                    new LinkItem
                                                    {
                                                        DisplayName = "List",
                                                        DisplayQualifiedNames = "System.Collections.Generic.List",
                                                        Name = "System.Collections.Generic.List`1",
                                                        IsExternalPath = true
                                                    },
                                                    new LinkItem { DisplayName = "<", DisplayQualifiedNames = "<" },
                                                    new LinkItem
                                                    {
                                                        DisplayName = "Object",
                                                        DisplayQualifiedNames = "System.Object",
                                                        Name = "System.Object",
                                                        IsExternalPath = true
                                                    },
                                                    new LinkItem { DisplayName = ">", DisplayQualifiedNames = ">" },
                                                }
                                            },
                                            {
                                                SyntaxLanguage.VB,
                                                new List<LinkItem>
                                                {
                                                    new LinkItem
                                                    {
                                                        DisplayName = "List",
                                                        DisplayQualifiedNames = "System.Collections.Generic.List",
                                                        Name = "System.Collections.Generic.List`1",
                                                        IsExternalPath = true,
                                                    },
                                                    new LinkItem { DisplayName = "(Of ", DisplayQualifiedNames = "(Of " },
                                                    new LinkItem
                                                    {
                                                        DisplayName = "Object",
                                                        DisplayQualifiedNames = "System.Object",
                                                        Name = "System.Object",
                                                        IsExternalPath = true,
                                                    },
                                                    new LinkItem { DisplayName = ")", DisplayQualifiedNames = ")" },
                                                }
                                            },
                                        },
                                    }
                                },
                            }
                        },
                    }
                }
            },
        };

        #endregion

        [Trait("Related", "Reference")]
        [Fact]
        public void TestConvertNamespace()
        {
            var vm = EntityModel.ViewModels.PageViewModel.FromModel(model.Items[0]);
            Assert.NotNull(vm);
            Assert.NotNull(vm.Items);
            Assert.Equal(1, vm.Items.Count);
            Assert.NotNull(vm.Items[0].Children);
            Assert.Equal(1, vm.Items[0].Children.Count);
            Assert.Equal("N1.C1", vm.Items[0].Children[0]);

            Assert.NotNull(vm.References);
            Assert.Equal(1, vm.References.Count);

            var reference = vm.References.Find(x => x.Uid == "N1.C1");
            Assert.NotNull(reference);
            Assert.Equal(MemberType.Class, reference.Type);
            Assert.Equal("Summary!", reference.Summary);
            Assert.Equal("C1", reference.Name);
            Assert.Null(reference.NameForCSharp);
            Assert.Null(reference.NameForVB);
            Assert.Equal("N1.C1", reference.FullName);
            Assert.Null(reference.FullNameForCSharp);
            Assert.Null(reference.FullNameForVB);
            Assert.False(reference.IsExternal);
            Assert.Equal("href!", reference.Href);
        }

        [Trait("Related", "Generic")]
        [Trait("Related", "Reference")]
        [Fact]
        public void TestConvertType()
        {
            var vm = EntityModel.ViewModels.PageViewModel.FromModel(model.Items[0].Items[0]);
            Assert.NotNull(vm);
            Assert.Null(vm.Items[0].Children);
            var inheritance = vm.Items[0].Inheritance;
            Assert.NotNull(inheritance);
            Assert.Equal(2, inheritance.Count);
            Assert.Equal(new[] { "System.Object", "System.Collections.Generic.List{System.Object}" }, inheritance.ToList());

            var implements = vm.Items[0].Implements;
            Assert.NotNull(implements);
            Assert.Equal(
                new[]
                {
                    "System.Collections.Generic.IList{System.Object}",
                    "System.Collections.Generic.ICollection{System.Object}",
                    "System.Collections.Generic.IEnumerable{System.Object}",
                    "System.Collections.IEnumerable",
                }.OrderBy(s => s),
                implements.OrderBy(s => s));

            var inheritedMembers = vm.Items[0].InheritedMembers;
            Assert.NotNull(inheritedMembers);
            Assert.Equal(1, inheritedMembers.Count);
            Assert.Equal(new[] { "System.Object.GetHashCode" }, inheritedMembers.ToList());

            Assert.NotNull(vm.References);
            Console.WriteLine(string.Join(";", from r in vm.References select r.Uid));
            Assert.Equal(
                new[]
                {
                        "System",
                        "System.Collections.Generic",
                        "System.Object",
                        "System.Object.GetHashCode",
                        "System.Collections.Generic.List`1",
                        "System.Collections.Generic.List{System.Object}"
                }.OrderBy(s => s),
                from r in vm.References orderby r.Uid select r.Uid);

            var reference = vm.References.Find(x => x.Uid == "System.Object");
            Assert.NotNull(reference);
            Assert.Equal("Object", reference.Name);
            Assert.Null(reference.NameForCSharp);
            Assert.Null(reference.NameForVB);
            Assert.Equal("System.Object", reference.FullName);
            Assert.Null(reference.FullNameForCSharp);
            Assert.Null(reference.FullNameForVB);
            Assert.True(reference.IsExternal);
            Assert.Equal("System", reference.Parent);
            Assert.Null(reference.Href);

            reference = vm.References.Find(x => x.Uid == "System.Object.GetHashCode");
            Assert.NotNull(reference);
            Assert.Equal("GetHashCode()", reference.Name);
            Assert.Null(reference.NameForCSharp);
            Assert.Null(reference.NameForVB);
            Assert.Equal("System.Object.GetHashCode()", reference.FullName);
            Assert.Null(reference.FullNameForCSharp);
            Assert.Null(reference.FullNameForVB);
            Assert.True(reference.IsExternal);
            Assert.Equal("System.Object", reference.Parent);
            Assert.Null(reference.Href);

            reference = vm.References.Find(x => x.Uid == "System.Collections.Generic.List{System.Object}");
            Assert.NotNull(reference);
            Assert.Equal("List<Object>", reference.Name);
            Assert.Null(reference.NameForCSharp);
            Assert.Equal("List(Of Object)", reference.NameForVB);
            Assert.Equal("System.Collections.Generic.List<System.Object>", reference.FullName);
            Assert.Null(reference.FullNameForCSharp);
            Assert.Equal("System.Collections.Generic.List(Of System.Object)", reference.FullNameForVB);
            {
                var list = reference.SpecForCSharp;
                Assert.NotNull(list);
                Assert.Equal(new[] { "List", "<", "Object", ">" }, (from x in list select x.Name).ToList());
                Assert.Equal(new[] { "System.Collections.Generic.List", "<", "System.Object", ">" }, (from x in list select x.FullName).ToList());
                Assert.Equal("System.Collections.Generic.List`1", list[0].Uid);
                Assert.Null(list[1].Uid);
                Assert.Equal("System.Object", list[2].Uid);
                Assert.Null(list[3].Uid);
                Assert.True(list[0].IsExternal);
                Assert.False(list[1].IsExternal);
                Assert.True(list[2].IsExternal);
                Assert.False(list[3].IsExternal);
                Assert.Null(list[0].Href);
                Assert.Null(list[1].Href);
                Assert.Null(list[2].Href);
                Assert.Null(list[3].Href);
            }
            {
                var list = reference.SpecForVB;
                Assert.NotNull(list);
                Assert.Equal(new[] { "List", "(Of ", "Object", ")" }, (from x in list select x.Name).ToList());
                Assert.Equal(new[] { "System.Collections.Generic.List", "(Of ", "System.Object", ")" }, (from x in list select x.FullName).ToList());
                Assert.Equal("System.Collections.Generic.List`1", list[0].Uid);
                Assert.Null(list[1].Uid);
                Assert.Equal("System.Object", list[2].Uid);
                Assert.Null(list[3].Uid);
                Assert.True(list[0].IsExternal);
                Assert.False(list[1].IsExternal);
                Assert.True(list[2].IsExternal);
                Assert.False(list[3].IsExternal);
                Assert.Null(list[0].Href);
                Assert.Null(list[1].Href);
                Assert.Null(list[2].Href);
                Assert.Null(list[3].Href);
            }
            Assert.Equal("System.Collections.Generic.List`1", reference.Definition);
            Assert.Equal("System.Collections.Generic", reference.Parent);
            Assert.Null(reference.IsExternal);
            Assert.Null(reference.Href);

            Assert.True(vm.References.Any(x => x.Uid == "System.Collections.Generic.List`1"));
        }

    }
}
