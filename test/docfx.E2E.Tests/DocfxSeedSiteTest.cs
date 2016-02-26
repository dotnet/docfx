// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.E2E.Tests
{
    using System.Collections.Generic;

    using OpenQA.Selenium;
    using Xunit;

    public class DocfxSeedSiteTest : IClassFixture<DocfxSeedSiteFixture>
    {
        private IWebDriver _driver;
        private string _urlHomepage;

        public DocfxSeedSiteTest(DocfxSeedSiteFixture fixture)
        {
            _driver = fixture.Driver;
            _urlHomepage = fixture.Url + @"/index.html";
        }

        [Fact]
        [Trait("Related", "E2Etest")]
        public void TestConceptualPage()
        {
            this._driver.Navigate().GoToUrl(_urlHomepage);

            // check logo
            IWebElement element = _driver.FindElement(By.Id("logo"));
            Assert.Equal("svg", element.TagName);

            // check navbar
            Assert.NotEmpty(_driver.FindElements(By.XPath("//div[@id='navbar']/ul/li/a")));

            // check article
            Assert.NotEmpty(_driver.FindElements(By.XPath("//article[@id='_content']")));

            // check "Improve this Doc" button
            _driver.FindElement(By.PartialLinkText("Improve this Doc")).Click();
            Assert.Contains("GitHub", _driver.Title);
            _driver.Navigate().Back();

            // check heading 2 and sidebar
            IList<IWebElement> resultsHeading = _driver.FindElements(By.TagName("h2"));
            if (resultsHeading.Count > 0)
            {
                IList<IWebElement> resultsSidebar = _driver.FindElements(By.XPath("//nav[@id='affix']/ul/li/a"));
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
            resultsHeading = _driver.FindElements(By.TagName("h3"));
            if (resultsHeading.Count > 0)
            {
                IList<IWebElement> resultsSidebar = _driver.FindElements(By.XPath("//nav[@id='affix']/ul/li/ul/li/a"));
                Assert.Equal(resultsHeading.Count, resultsSidebar.Count);
                for (int i = 0; i < resultsSidebar.Count; i++)
                {
                    var id = resultsHeading[i].GetAttribute("id");
                    var href = resultsSidebar[i].GetAttribute("href");
                    Assert.Equal(id, href.Substring(href.Length - id.Length));
                }
            }

            // check footer
            Assert.NotEmpty(_driver.FindElements(By.XPath("//footer/div[@class='footer']/div[@class='container']")));

            // check breadcrumb
            Assert.NotEmpty(_driver.FindElements(By.XPath("//div[@id='breadcrumb']/ul/li/a")));

            // go to 'Articles'
            _driver.FindElement(By.XPath("//div[@id='navbar']/ul/li/a[@title='Articles']")).Click();

            // check toc
            IList<IWebElement> results = _driver.FindElements(By.XPath("//div[@class='sidetoc']/div[@id='toc']/ul/li"));
            Assert.NotEmpty(results);
            Assert.Equal("Getting Started", _driver.FindElement(By.XPath("//div[@class='sidetoc']/div[@id='toc']/ul/li[@class='active in']/a")).Text);
            var title = results[results.Count - 1].Text;

            // check filter
            _driver.FindElement(By.Id("toc_filter_input")).SendKeys(title);
            Assert.True(results[results.Count - 1].Displayed);
            results[results.Count - 1].Click();
            results = _driver.FindElements(By.XPath("//div[@id='toc']/ul/li"));
            Assert.NotEmpty(results);
            Assert.Equal("active in", results[results.Count - 1].GetAttribute("class"));
        }

        [Fact]
        [Trait("Related", "E2Etest")]
        public void TestReferencePage()
        {
            _driver.Navigate().GoToUrl(_urlHomepage);

            // go to reference
            _driver.FindElement(By.LinkText("API Documentation")).Click(); // TODO: check each namepace page
            _driver.FindElements(By.XPath("//h4/a"))[0].Click(); // TODO: check each object page in current namespace
            System.Threading.Thread.Sleep(1000);

            // check logo
            IWebElement element = _driver.FindElement(By.Id("logo"));
            Assert.Equal("svg", element.TagName);

            // check navbar
            Assert.NotEmpty(_driver.FindElements(By.XPath("//div[@id='navbar']/ul/li/a")));

            // check heading 1
            IList<IWebElement> results = _driver.FindElements(By.TagName("h1"));
            Assert.NotEmpty(results);
            var title = results[0].Text;

            // check breadcrumb
            results = _driver.FindElements(By.XPath("//div[@id='breadcrumb']/ul/li/a"));
            Assert.Contains(results[results.Count - 1].Text, title);

            // check article
            Assert.NotEmpty(_driver.FindElements(By.XPath("//article[@id='_content']")));

            // check overwrite
            element = _driver.FindElement(By.ClassName("conceptual"));
            Assert.Contains("This is a class talking about CAT.", element.Text);
            _driver.FindElement(By.LinkText("CAT")).Click();
            Assert.Contains("Wikipedia", _driver.Title);
            _driver.Navigate().Back();
            element = _driver.FindElement(By.TagName("blockquote"));
            Assert.Equal("NOTE This is a CAT class", element.Text);

            // check "View Source" button
            _driver.FindElement(By.PartialLinkText("View Source")).Click();
            Assert.Contains("GitHub", _driver.Title);
            _driver.Navigate().Back();

            // check "Improve This Doc" button
            _driver.FindElement(By.PartialLinkText("Improve this Doc")).Click();
            Assert.Contains("GitHub", _driver.Title);
            _driver.Navigate().Back();

            // check heading 3 and sidebar
            IList<IWebElement> resultsHeading = _driver.FindElements(By.TagName("h3"));
            if (resultsHeading.Count > 0)
            {
                results = _driver.FindElements(By.XPath("//nav[@id='affix']/ul/li/a"));
                Assert.Equal(resultsHeading.Count, results.Count);
                for (int i = 0; i < results.Count; i++)
                {
                    var id = resultsHeading[i].GetAttribute("id");
                    var href = results[i].GetAttribute("href");
                    Assert.Equal(id, href.Substring(href.Length - id.Length));
                }
            }

            // check heading 4 and sidebar
            resultsHeading = _driver.FindElements(By.TagName("h4"));
            if (resultsHeading.Count > 0)
            {
                results = _driver.FindElements(By.XPath("//nav[@id='affix']/ul/li/ul/li/a"));
                Assert.Equal(resultsHeading.Count, results.Count);
                for (int i = 0; i < results.Count; i++)
                {
                    var id = resultsHeading[i].GetAttribute("id");
                    var href = results[i].GetAttribute("href");
                    Assert.Equal(id, href.Substring(href.Length - id.Length));
                }
            }

            // check footer
            Assert.NotEmpty(_driver.FindElements(By.XPath("//footer/div[@class='footer']/div[@class='container']")));

            // check toc
            results = _driver.FindElements(By.XPath("//div[@class='sidetoc']/div[@id='toc']/ul/li"));
            Assert.NotEmpty(results);
            results = _driver.FindElements(By.XPath("//div[@class='sidetoc']/div[@id='toc']/ul/li/ul/li[@class='active in']/a"));
            Assert.NotEmpty(results);
            Assert.Contains(results[0].Text, title);

            // check filter
            _driver.FindElement(By.Id("toc_filter_input")).SendKeys("tomfrombase");
            results = _driver.FindElements(By.XPath("//div[@id='toc']/ul/li/ul/li[@class='show']/a"));
            Assert.NotEmpty(results);
            Assert.Contains("tomfrombaseclass", results[0].Text.ToLower());
            Assert.True(results[0].Displayed);
            results[0].Click();
            results = _driver.FindElements(By.XPath("//div[@class='sidetoc']/div[@id='toc']/ul/li/ul/li[@class='active in']"));
            Assert.NotEmpty(results);
            Assert.Equal("TomFromBaseClass", results[0].Text);

            // check inheritance
            results = _driver.FindElements(By.XPath("//div[@class='inheritance']/div/a"));
            if (results.Count > 0)
            {
                var titleBase = results[0].Text; // TODO: check each base class
                results[0].Click();
                results = _driver.FindElements(By.TagName("h1"));
                Assert.NotEmpty(results);
                Assert.Contains(titleBase, results[0].Text);
            }
        }
    }
}
