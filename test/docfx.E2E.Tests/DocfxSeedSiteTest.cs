// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.E2E.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Net;

    using OpenQA.Selenium;
    using Xunit;
    using YamlDotNet.Serialization;

    public class DocfxSeedSiteTest : IClassFixture<DocfxSeedSiteFixture>
    {
        private readonly IWebDriver _driver;
        private readonly string _urlHomepage;
        private readonly string _urlXrefMap;

        public DocfxSeedSiteTest(DocfxSeedSiteFixture fixture)
        {
            _driver = fixture.Driver;
            _urlHomepage = fixture.Url + @"/index.html";
            _urlXrefMap = fixture.Url + @"/xrefmap.yml";
        }

        [Fact]
        [Trait("Related", "E2Etest")]
        public void TestConceptualPage()
        {
            _driver.Navigate().GoToUrl(_urlHomepage);

            TestPageCommon();

            // check "Improve this Doc" button
            FindAndTestLinkTitle(_driver, "Improve this Doc", "GitHub");

            // check heading 2 and sidebar
            IList<IWebElement> resultsHeading = FindElements(By.TagName("h2"));
            if (resultsHeading.Count > 0)
            {
                IList<IWebElement> resultsSidebar = FindElements(By.XPath("//nav[@id='affix']/ul/li/a"));
                Assert.Equal(resultsHeading.Count, resultsSidebar.Count);
                for (int i = 0; i < resultsSidebar.Count; i++)
                {
                    Assert.Equal(resultsHeading[i].Text, resultsSidebar[i].Text);
                    var id = resultsHeading[i].GetAttribute("id");
                    var href = resultsSidebar[i].GetAttribute("href");
                    Assert.Equal(id, href.Substring(href.Length - id.Length));
                }
            }

            // check heading 3 and sidebar
            resultsHeading = FindElements(By.TagName("h3"));
            if (resultsHeading.Count > 0)
            {
                IList<IWebElement> resultsSidebar = FindElements(By.XPath("//nav[@id='affix']/ul/li/ul/li/a"));
                Assert.Equal(resultsHeading.Count, resultsSidebar.Count);
                for (int i = 0; i < resultsSidebar.Count; i++)
                {
                    var id = resultsHeading[i].GetAttribute("id");
                    var href = resultsSidebar[i].GetAttribute("href");
                    Assert.Equal(id, href.Substring(href.Length - id.Length));
                }
            }

            // check footer
            Assert.NotEmpty(FindElements(By.XPath("//footer/div[@class='footer']/div[@class='container']")));

            // check breadcrumb
            Assert.NotEmpty(FindElements(By.XPath("//div[@id='breadcrumb']/ul/li/a")));

            // go to 'Articles'
            FindElement(By.XPath("//div[@id='navbar']/ul/li/a[@title='Articles']")).Click();

            // check toc
            IList<IWebElement> results = FindElements(By.XPath("//div[@class='sidetoc']/div[@id='toc']/ul/li"));
            Assert.NotEmpty(results);
            Assert.Equal("Getting Started", FindElement(By.XPath("//div[@class='sidetoc']/div[@id='toc']/ul/li[@class='active in']/a")).Text);
            var title = results[results.Count - 1].Text;

            // check filter
            FindElement(By.Id("toc_filter_input")).SendKeys(title);
            Assert.True(results[results.Count - 1].Displayed);
            results[results.Count - 1].Click();
            results = FindElements(By.XPath("//div[@id='toc']/ul/li"));
            Assert.NotEmpty(results);
            Assert.Equal("active in", results[results.Count - 1].GetAttribute("class"));
        }

        [Fact]
        [Trait("Related", "E2Etest")]
        public void TestReferencePage()
        {
            _driver.Navigate().GoToUrl(_urlHomepage);

            // go to reference
            FindElement(By.LinkText("API Documentation")).Click();

            // make sure the namespace page has been loaded
            FindElement(By.Id("classes"));

            // go to class page
            FindElements(By.XPath("//h4/a[contains(@class, 'xref')]"))[0].Click();

            // make sure the class page has been loaded
            FindElement(By.Id("methods"));

            TestPageCommon();

            // check heading 1
            IList<IWebElement> results = FindElements(By.TagName("h1"));
            Assert.NotEmpty(results);
            var title = results[0].Text;

            // check breadcrumb
            results = FindElements(By.XPath("//div[@id='breadcrumb']/ul/li/a"));
            Assert.Contains(results[results.Count - 1].Text, title);

            // check overwrite
            var conceptual = FindElement(By.ClassName("conceptual"));
            // add these lines to help find out what sometime breaks e2e
            Assert.True(conceptual.Text.Contains("This is a class talking about CAT."), $"Actual HTML: {conceptual.GetAttribute("outerHTML")}\n Full HTML:\n {_driver.PageSource}");
            Assert.Contains("This is a class talking about CAT.", conceptual.Text);
            var element = conceptual.FindElement(By.TagName("blockquote"));
            Assert.Equal("NOTE This is a CAT class", element.Text);
            FindAndTestLinkTitle(conceptual, "CAT", "Wikipedia");
            conceptual = FindElement(By.ClassName("conceptual"));
            FindAndTestLinkTitle(conceptual, "IAnimal", "IAnimal");

            // check "View Source" buttons
            results = FindElements(By.LinkText("View Source"));
            Assert.True(results.Count >= 2);
            TestLinkTitle(results[0], "GitHub", "Class1.cs");
            TestLinkTitle(FindElements(By.LinkText("View Source"))[1], "GitHub", "Class1.cs");

            // check "Improve This Doc" buttons
            results = FindElements(By.LinkText("Improve this Doc"));
            Assert.True(results.Count >= 2);
            TestLinkTitle(results[0], "GitHub");
            TestLinkTitle(FindElements(By.LinkText("Improve this Doc"))[1], "GitHub");

            // check heading 3 and sidebar
            IList<IWebElement> resultsHeading = FindElements(By.TagName("h3"));
            if (resultsHeading.Count > 0)
            {
                results = FindElements(By.XPath("//nav[@id='affix']/ul/li/a"));
                Assert.Equal(resultsHeading.Count, results.Count);
                for (int i = 0; i < results.Count; i++)
                {
                    var id = resultsHeading[i].GetAttribute("id");
                    var href = results[i].GetAttribute("href");
                    Assert.Equal(id, href.Substring(href.Length - id.Length));
                }
            }

            // check heading 4 and sidebar
            resultsHeading = FindElements(By.TagName("h4"));
            if (resultsHeading.Count > 0)
            {
                results = FindElements(By.XPath("//nav[@id='affix']/ul/li/ul/li/a"));
                Assert.Equal(resultsHeading.Count, results.Count);
                for (int i = 0; i < results.Count; i++)
                {
                    var id = resultsHeading[i].GetAttribute("id");
                    var href = results[i].GetAttribute("href");
                    Assert.Equal(id, href.Substring(href.Length - id.Length));
                }
            }

            // check footer
            Assert.NotEmpty(FindElements(By.XPath("//footer/div[@class='footer']/div[@class='container']")));

            // check toc
            results = FindElements(By.XPath("//div[@class='sidetoc']/div[@id='toc']/ul/li"));
            Assert.NotEmpty(results);
            results = FindElements(By.XPath("//div[@class='sidetoc']/div[@id='toc']/ul/li/ul/li[@class='active in']/a"));
            Assert.NotEmpty(results);
            Assert.Contains(results[0].Text, title);

            // check spec name in parameters' type
            element = FindElement(By.XPath("//h4[@id='CatLibrary_Cat_2_op_Addition_CatLibrary_Cat__0__1__System_Int32_']/following-sibling::div/table/tbody/tr/td"));
            Assert.NotNull(element);
            Assert.Equal("Cat<T, K>", element.Text);

            // check extension methods
            results = FindElements(By.XPath("//h3[@id='extensionmethods']"));
            if (results.Count > 0)
            {
                Assert.Equal(1, results.Count);
                results = FindElements(By.XPath("//h3[@id='extensionmethods']/following-sibling::div/a"));
                var value = results[0].Text;
                Assert.NotEmpty(value);
                results[0].Click();
            }

            // check filter
            FindElement(By.Id("toc_filter_input")).SendKeys("tomfrombase");
            results = FindElements(By.XPath("//div[@id='toc']/ul/li/ul/li[@class='show']/a"));
            Assert.NotEmpty(results);
            Assert.Contains("tomfrombaseclass", results[0].Text.ToLower());
            Assert.True(results[0].Displayed);
            results[0].Click();
            results = FindElements(By.XPath("//div[@class='sidetoc']/div[@id='toc']/ul/li/ul/li[@class='active in']"));
            Assert.NotEmpty(results);
            Assert.Equal("TomFromBaseClass", results[0].Text);

            // check inheritance
            results = FindElements(By.XPath("//div[@class='inheritance']/div/a"));
            if (results.Count > 0)
            {
                var titleBase = results[0].Text; // TODO: check each base class
                results[0].Click();
                results = FindElements(By.TagName("h1"));
                Assert.NotEmpty(results);
                Assert.Contains(titleBase, results[0].Text);
            }
        }

        [Fact]
        [Trait("Related", "E2Etest")]
        public void TestRestApiPage()
        {
            _driver.Navigate().GoToUrl(_urlHomepage);

            // go to reference
            FindElement(By.LinkText("REST API")).Click();

            // check link to file in overwrite
            var results = FindElements(By.XPath("//div[@class='markdown level0 api-footer']/ul/li/a"));
            Assert.NotEmpty(results);
            var href = results[0].GetAttribute("href");
            Assert.True(CheckIfLinkValid(href));
        }

        [Fact]
        [Trait("Related", "E2Etest")]
        public void TestXRefMap()
        {
            _driver.Navigate().GoToUrl(_urlXrefMap);
            var deserializer = new Deserializer();
            string contents;
            using (var wc = new WebClient())
            {
                contents = wc.DownloadString(_urlXrefMap);
            }
            Assert.NotEmpty(contents);
            var xrefMap = deserializer.Deserialize<Dictionary<string, object>>(new StringReader(contents));
            Assert.NotEmpty(xrefMap);
            var references = (List<object>)xrefMap["references"];
            Assert.NotEmpty(references);

            var namespaceItem = (Dictionary<object, object>)references[0];
            Assert.Equal("CatLibrary", namespaceItem["uid"]);
            Assert.Equal("api/CatLibrary.html", namespaceItem["href"]);

            var classItem = (Dictionary<object, object>)references[1];
            Assert.Equal("CatLibrary.Cat`2", "CatLibrary.Cat`2");
            Assert.Equal("api/CatLibrary.Cat-2.html", classItem["href"]);

            var methodItem = (Dictionary<object, object>)references[8];
            Assert.Equal("CatLibrary.Cat`2.CalculateFood(System.DateTime)", methodItem["uid"]);
            Assert.Equal("api/CatLibrary.Cat-2.html#CatLibrary_Cat_2_CalculateFood_System_DateTime_", methodItem["href"]);

            var restRootItem = (Dictionary<object, object>)references[93];
            Assert.Equal("petstore.swagger.io/v2/Swagger Petstore/1.0.0", restRootItem["uid"]);
            Assert.Equal("restapi/petstore.html", restRootItem["href"]);

            var restChildItem = (Dictionary<object, object>)references[98];
            Assert.Equal("petstore.swagger.io/v2/Swagger Petstore/1.0.0/deleteOrder", restChildItem["uid"]);
            Assert.Equal("restapi/petstore.html#petstore_swagger_io_v2_Swagger_Petstore_1_0_0_deleteOrder", restChildItem["href"]);

            var restTagItem = (Dictionary<object, object>)references[110];
            Assert.Equal("petstore.swagger.io/v2/Swagger Petstore/1.0.0/tag/pet", restTagItem["uid"]);
            Assert.Equal("restapi/petstore.html#petstore_swagger_io_v2_Swagger_Petstore_1_0_0_tag_pet", restTagItem["href"]);
        }

        private void TestPageCommon()
        {
            // check logo
            Assert.NotEmpty(FindElements(By.Id("logo")));

            // check title
            Assert.Contains(FindElement(By.TagName("h1")).Text, _driver.Title);

            // check navbar
            Assert.NotEmpty(FindElements(By.XPath("//div[@id='navbar']/ul/li/a")));

            // check article
            Assert.NotEmpty(FindElements(By.XPath("//article[@id='_content']")));
        }

        private void FindAndTestLinkTitle(ISearchContext context, string linkText, params string[] titlePart)
        {
            TestLinkTitle(context.FindElement(By.LinkText(linkText)), titlePart);
        }

        private void TestLinkTitle(IWebElement element, params string[] titlePart)
        {
            // 1. scroll to the element before clicking on it
            // 2. scroll up 100px to prevent element from being covered by navbar
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView(true);window.scrollBy(0,-100);", element);
            element.Click();
            foreach (var part in titlePart)
            {
                Assert.Contains(part, _driver.Title);
            }
            _driver.Navigate().Back();
        }

        private bool CheckIfLinkValid(string href)
        {
            var originHref = _driver.Url;
            try
            {
                _driver.Navigate().GoToUrl(href);
                _driver.Navigate().GoToUrl(originHref);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private IWebElement FindElement(By by)
        {
            try
            {
                return _driver.FindElement(by);
            }
            catch (NoSuchElementException e)
            {
                throw new NoSuchElementException($"URL: {_driver.Url}\nFull HTML:\n {_driver.PageSource}", e);
            }
        }

        private ReadOnlyCollection<IWebElement> FindElements(By by)
        {
            var result = _driver.FindElements(by);
            if (result.Count == 0)
            {
                throw new NoSuchElementException($"URL: {_driver.Url}\nFull HTML:\n {_driver.PageSource}");
            }
            return result;
        }
    }
}
