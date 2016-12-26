// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.DataContracts.Common.Attributes;

    [Trait("Owner", "lianwei")]
    public class ModelAttributeHandlerTest
    {
        [Fact]
        public void TestSimpleModelWithUniqueIdentityReferenceAttributeShouldSucceed()
        {
            var model = new SimpleModel
            {
                Identity = "Identity1",
                Identities = new List<string> { "Identity2" }
            };

            var context = Handle(model);

            Assert.Equal(2, context.LinkToUids.Count);
            Assert.Equal(model.Identity, context.LinkToUids[0]);
            Assert.Equal(model.Identities[0], context.LinkToUids[1]);
        }

        [Fact]
        public void TestModelWithInvalidTypeShouldThrow()
        {
            var model = new InvalidModel
            {
                Identity = "identity",
                InvalidIdentity = 1,
            };
            Assert.Throws<NotSupportedException>(
                () => Handle(model)
                );
        }

        [Fact]
        public void TestModelWithInvalidItemTypeShouldThrow()
        {
            var model = new InvalidModel2
            {
                Identities = new List<int> { 0 }
            };
            Assert.Throws<NotSupportedException>(
                () => Handle(model)
                );
        }

        [Fact]
        public void TestComplexModelWithUniqueIdentityReferenceAttributeShouldSucceed()
        {
            var model = new ComplexModel
            {
                Identities = new List<string> { "1", "2", "3" },
                Identity = "0",
                Inner = new ComplexModel
                {
                    Identities = new List<string> { "1.1", "1.2", "1.3" },
                    Identity = "0.0",
                    OtherProperty = "innerothers",
                    Inner = new ComplexModel
                    {
                        Identities = new List<string> { "1.1.1", "1.1.2" },
                        Identity = "0.0.0",
                        OtherProperty = "innersinner"
                    }
                },
                OtherProperty = "others",
                InnerModels = new List<InnerModel>
                {
                    new InnerModel
                    {
                         Identity = "2.1",
                         CrefType = TestCrefType.Cref
                    },
                    new InnerModel
                    {
                         Identity = "2.2",
                         CrefType = TestCrefType.Href
                    }
                }
            };
            var context = Handle(model);

            Assert.Equal(12, context.LinkToUids.Count);
            Assert.Equal(new List<string> {
                "0", "1", "2", "3", "2.2", "0.0", "1.1", "1.2", "1.3", "0.0.0", "1.1.1", "1.1.2"
            }, context.LinkToUids);
        }

        #region Helper Method

        private static HandleModelAttributesContext Handle(object model)
        {
            var handler = new CompositeModelAttributeHandler(new UniqueIdentityReferenceHandler());
            var context = new HandleModelAttributesContext();

            handler.Handle(model, context);
            return context;
        }

        #endregion

        #region Test Data

        private class SimpleModel
        {
            [UniqueIdentityReference]
            public string Identity { get; set; }
            [UniqueIdentityReference]
            public List<string> Identities { get; set; }
        }

        private class InvalidModel
        {
            [UniqueIdentityReference]
            public int InvalidIdentity { get; set; }

            [UniqueIdentityReference]
            public string Identity { get; set; }
        }

        private class InvalidModel2
        {
            [UniqueIdentityReference]
            public List<int> Identities { get; set; }
        }

        private class ComplexModel
        {
            [UniqueIdentityReference]
            public string Identity { get; set; }

            [UniqueIdentityReference]
            public List<string> Identities { get; set; }

            [UniqueIdentityReference]
            public IEnumerable<string> Substitute => InnerModels?.Where(s => s.CrefType == TestCrefType.Href).Select(s => s.Identity);

            public List<InnerModel> InnerModels { get; set; }

            public ComplexModel Inner { get; set; }

            public string OtherProperty { get; set; }
        }

        private class InnerModel
        {
            public string Identity { get; set; }
            public TestCrefType CrefType { get; set; }
        }

        private enum TestCrefType
        {
            Href,
            Cref
        }

        #endregion
    }
}
