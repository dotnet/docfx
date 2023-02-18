// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import $ from 'jquery'

declare global {
  interface Window { 
    $: any;
    jQuery: any;
    _docfxReady: any;
  }
}

import '../node_modules/bootstrap/dist/css/bootstrap.css'
import '../node_modules/highlight.js/scss/github.scss'
import './docfx.scss'

window.$ = window.jQuery = $

require('bootstrap');
require('twbs-pagination');
require('mark.js/src/jquery.js');

import AnchorJS from 'anchor-js'
import hljs from 'highlight.js'

const anchors = new AnchorJS();

$(function () {
  const active = 'active';
  const expanded = 'in';
  const collapsed = 'collapsed';
  const filtered = 'filtered';
  const show = 'show';
  const hide = 'hide';
  const util = new utility();

  workAroundFixedHeaderForAnchors();
  highlight();
  enableSearch();

  renderTables();
  renderAlerts();
  renderLinks();
  renderNavbar();
  renderSidebar();
  renderAffix();
  renderFooter();
  renderLogo();

  breakText();
  renderTabs();

  function breakText() {
    $(".xref").addClass("text-break");
    const texts = $(".text-break");
    texts.each(function () {
      $(this).breakWord();
    });
  }

  // Styling for tables in conceptual documents using Bootstrap.
  // See http://getbootstrap.com/css/#tables
  function renderTables() {
    $('table').addClass('table table-bordered table-striped table-condensed').wrap('<div class=\\"table-responsive\\"></div>');
  }

  // Styling for alerts.
  function renderAlerts() {
    $('.NOTE, .TIP').addClass('alert alert-info');
    $('.WARNING').addClass('alert alert-warning');
    $('.IMPORTANT, .CAUTION').addClass('alert alert-danger');
  }

  // Enable anchors for headings.
  (function () {
    anchors.options = {
      placement: 'left',
      visible: 'hover'
    };
    anchors.add('article h2:not(.no-anchor), article h3:not(.no-anchor), article h4:not(.no-anchor)');
  })();

  // Open links to different host in a new window.
  function renderLinks() {
    if ($("meta[property='docfx:newtab']").attr("content") === "true") {
      $(document.links).filter(function () {
        return this.hostname !== window.location.hostname;
      }).attr('target', '_blank');
    }
  }

  // Enable highlight.js
  function highlight() {
    $('pre code').each(function (i, block) {
      hljs.highlightElement(block);
    });
    $('pre code[highlight-lines]').each(function (i, block) {
      if (block.innerHTML === "") return;
      const lines = block.innerHTML.split('\n');

      const queryString = block.getAttribute('highlight-lines');
      if (!queryString) return;

      const ranges = queryString.split(',');
      for (let j = 0, range; range = ranges[j++];) {
        const found = range.match(/^(\d+)\\-(\d+)?$/);
        if (found) {
          // consider region as `{startlinenumber}-{endlinenumber}`, in which {endlinenumber} is optional
          const start = +found[1];
          let end = +found[2];
          if (isNaN(end) || end > lines.length) {
            end = lines.length;
          }
        } else {
          // consider region as a sigine line number
          if (isNaN(range)) continue;
          var start = +range;
          var end = start;
        }
        if (start <= 0 || end <= 0 || start > end || start > lines.length) {
          // skip current region if invalid
          continue;
        }
        lines[start - 1] = '<span class="line-highlight">' + lines[start - 1];
        lines[end - 1] = lines[end - 1] + '</span>';
      }

      block.innerHTML = lines.join('\n');
    });
  }

  // Support full-text-search
  function enableSearch() {
    let query;
    const relHref = $("meta[property='docfx\\:rel']").attr("content");
    if (typeof relHref === 'undefined') {
      return;
    }
    try {
      webWorkerSearch();
      renderSearchBox();
      highlightKeywords();
      addSearchEvent();
    } catch (e) {
      console.error(e);
    }

    //Adjust the position of search box in navbar
    function renderSearchBox() {
      autoCollapse();
      $(window).on('resize', autoCollapse);
      $(document).on('click', '.navbar-collapse.in', function (e) {
        if ($(e.target).is('a')) {
          $(this).collapse('hide');
        }
      });

      function autoCollapse() {
        const navbar = $('#autocollapse');
        if (navbar.height() === null) {
          setTimeout(autoCollapse, 300);
        }
        navbar.removeClass(collapsed);
        if (navbar.height() > 60) {
          navbar.addClass(collapsed);
        }
      }
    }

    function webWorkerSearch() {
      console.log("using Web Worker");
      const indexReady = $.Deferred();

      const worker = new Worker(relHref + 'styles/search-worker.min.js')
      worker.onmessage = function (oEvent) {
        switch (oEvent.data.e) {
          case 'index-ready':
            indexReady.resolve();
            break;
          case 'query-ready':
            handleSearchResults(oEvent.data.d);
            break;
        }
      };

      indexReady.promise().done(function () {
        $("body").bind("queryReady", function () {
          worker.postMessage({ q: query });
        });
        if (query && (query.length >= 3)) {
          worker.postMessage({ q: query });
        }
      });
    }

    // Highlight the searching keywords
    function highlightKeywords() {
      const q = new URLSearchParams(window.location.search).get('q');
      if (q) {
        const keywords = q.split("%20");
        keywords.forEach(function (keyword) {
          if (keyword !== "") {
            $('.data-searchable *').mark(keyword);
            $('article *').mark(keyword);
          }
        });
      }
    }

    function addSearchEvent() {
      $('body').bind("searchEvent", function () {
        $('#search-query').keypress(function (e) {
          return e.which !== 13;
        });

        $('#search-query').keyup(function () {
          query = $(this).val();
          if (query.length < 3) {
            flipContents("show");
          } else {
            flipContents("hide");
            $("body").trigger("queryReady");
            $('#search-results>.search-list>span').text('"' + query + '"');
          }
        }).off("keydown");
      });
    }

    function flipContents(action) {
      if (action === "show") {
        $('.hide-when-search').show();
        $('#search-results').hide();
      } else {
        $('.hide-when-search').hide();
        $('#search-results').show();
      }
    }

    function relativeUrlToAbsoluteUrl(currentUrl, relativeUrl) {
      const currentItems = currentUrl.split(/\/+/);
      const relativeItems = relativeUrl.split(/\/+/);
      let depth = currentItems.length - 1;
      const items = [];
      for (let i = 0; i < relativeItems.length; i++) {
        if (relativeItems[i] === '..') {
          depth--;
        } else if (relativeItems[i] !== '.') {
          items.push(relativeItems[i]);
        }
      }
      return currentItems.slice(0, depth).concat(items).join('/');
    }

    function extractContentBrief(content) {
      const briefOffset = 512;
      const words = query.split(/\s+/g);
      const queryIndex = content.indexOf(words[0]);
      if (queryIndex > briefOffset) {
        return "..." + content.slice(queryIndex - briefOffset, queryIndex + briefOffset) + "...";
      } else if (queryIndex <= briefOffset) {
        return content.slice(0, queryIndex + briefOffset) + "...";
      }
    }

    function handleSearchResults(hits) {
      const numPerPage = 10;
      const pagination = $('#pagination');
      pagination.empty();
      pagination.removeData("twbs-pagination");
      if (hits.length === 0) {
        $('#search-results>.sr-items').html('<p>No results found</p>');
      } else {        
        pagination.twbsPagination({
          first: pagination.data('first'),
          prev: pagination.data('prev'),
          next: pagination.data('next'),
          last: pagination.data('last'),
          totalPages: Math.ceil(hits.length / numPerPage),
          visiblePages: 5,
          onPageClick: function (event, page) {
            const start = (page - 1) * numPerPage;
            const curHits = hits.slice(start, start + numPerPage);
            $('#search-results>.sr-items').empty().append(
              curHits.map(function (hit) {
                const currentUrl = window.location.href;
                const itemRawHref = relativeUrlToAbsoluteUrl(currentUrl, relHref + hit.href);
                const itemHref = relHref + hit.href + "?q=" + query;
                const itemTitle = hit.title;
                const itemBrief = extractContentBrief(hit.keywords);

                const itemNode = $('<div>').attr('class', 'sr-item');
                const itemTitleNode = $('<div>').attr('class', 'item-title').append($('<a>').attr('href', itemHref).attr("target", "_blank").attr("rel", "noopener noreferrer").text(itemTitle));
                const itemHrefNode = $('<div>').attr('class', 'item-href').text(itemRawHref);
                const itemBriefNode = $('<div>').attr('class', 'item-brief').text(itemBrief);
                itemNode.append(itemTitleNode).append(itemHrefNode).append(itemBriefNode);
                return itemNode;
              })
            );
            query.split(/\s+/).forEach(function (word) {
              if (word !== '') {
                $('#search-results>.sr-items *').mark(word);
              }
            });
          }
        });
      }
    }
  }

  // Update href in navbar
  function renderNavbar() {
    const navbar = $('#navbar ul')[0];
    if (typeof (navbar) === 'undefined') {
      loadNavbar();
    } else {
      $('#navbar ul a.active').parents('li').addClass(active);
      renderBreadcrumb();
      showSearch();
    }
    
    function showSearch() {
      if ($('#search-results').length !== 0) {
          $('#search').show();
          $('body').trigger("searchEvent");
      }
    }

    function loadNavbar() {
      let navbarPath = $("meta[property='docfx\\:navrel']").attr("content");
      if (!navbarPath) {
        return;
      }
      navbarPath = navbarPath.replace(/\\/g, '/');
      let tocPath = $("meta[property='docfx\\:tocrel']").attr("content") || '';
      if (tocPath) tocPath = tocPath.replace(/\\/g, '/');
      $.get(navbarPath, function (data) {
        $(data).find("#toc>ul").appendTo("#navbar");
        showSearch();
        const index = navbarPath.lastIndexOf('/');
        let navrel = '';
        if (index > -1) {
          navrel = navbarPath.substr(0, index + 1);
        }
        $('#navbar>ul').addClass('navbar-nav');
        const currentAbsPath = util.getCurrentWindowAbsolutePath();
        // set active item
        $('#navbar').find('a[href]').each(function (i, e) {
          let href = $(e).attr("href");
          if (util.isRelativePath(href)) {
            href = navrel + href;
            $(e).attr("href", href);

            let isActive = false;
            let originalHref = e.name;
            if (originalHref) {
              originalHref = navrel + originalHref;
              if (util.getDirectory(util.getAbsolutePath(originalHref)) === util.getDirectory(util.getAbsolutePath(tocPath))) {
                isActive = true;
              }
            } else {
              if (util.getAbsolutePath(href) === currentAbsPath) {
                const dropdown = $(e).attr('data-toggle') == "dropdown";
                if (!dropdown) {
                  isActive = true;
                }
              }
            }
            if (isActive) {
              $(e).addClass(active);
            }
          }
        });
        renderNavbar();
      });
    }
  }

  function renderSidebar() {
    const sidetoc = $('#sidetoggle .sidetoc')[0];
    if (typeof (sidetoc) === 'undefined') {
      loadToc();
    } else {
      registerTocEvents();
      if ($('footer').is(':visible')) {
        $('.sidetoc').addClass('shiftup');
      }

      // Scroll to active item
      let top = 0;
      $('#toc a.active').parents('li').each(function (i, e) {
        $(e).addClass(active).addClass(expanded);
        $(e).children('a').addClass(active);
      });
      $('#toc a.active').parents('li').each(function (i, e) {
        top += $(e).position().top;
      });
      $('.sidetoc').scrollTop(top - 50);

      if ($('footer').is(':visible')) {
        $('.sidetoc').addClass('shiftup');
      }

      renderBreadcrumb();
    }

    function registerTocEvents() {
      const tocFilterInput = $('#toc_filter_input');
      const tocFilterClearButton = $('#toc_filter_clear');
        
      $('.toc .nav > li > .expand-stub').click(function (e) {
        $(e.target).parent().toggleClass(expanded);
      });
      $('.toc .nav > li > .expand-stub + a:not([href])').click(function (e) {
        $(e.target).parent().toggleClass(expanded);
      });
      tocFilterInput.on('input', function () {
        const val = this.value;
        //Save filter string to local session storage
        if (typeof(Storage) !== "undefined") {
          sessionStorage.filterString = val;
        }
        if (val === '') {
          // Clear 'filtered' class
          $('#toc li').removeClass(filtered).removeClass(hide);
          tocFilterClearButton.fadeOut();
          return;
        }
        tocFilterClearButton.fadeIn();

        // set all parent nodes status
        $('#toc li>a').filter(function (i, e) {
          return $(e).siblings().length > 0;
        }).each(function (i, anchor) {
          const parent = $(anchor).parent();
          parent.addClass(hide);
          parent.removeClass(show);
          parent.removeClass(filtered);
        });
        
        // Get leaf nodes
        $('#toc li>a').filter(function (i, e) {
          return $(e).siblings().length === 0;
        }).each(function (_, anchor) {
          let text = $(anchor).attr('title');
          const parent = $(anchor).parent();
          const parentNodes = parent.parents('ul>li');
          for (let i = 0; i < parentNodes.length; i++) {
            const parentText = $(parentNodes[i]).children('a').attr('title');
            if (parentText) text = parentText + '.' + text;
          }
          if (filterNavItem(text, val)) {
            parent.addClass(show);
            parent.removeClass(hide);
          } else {
            parent.addClass(hide);
            parent.removeClass(show);
          }
        });
        $('#toc li>a').filter(function (i, e) {
          return $(e).siblings().length > 0;
        }).each(function (i, anchor) {
          const parent = $(anchor).parent();
          if (parent.find('li.show').length > 0) {
            parent.addClass(show);
            parent.addClass(filtered);
            parent.removeClass(hide);
          } else {
            parent.addClass(hide);
            parent.removeClass(show);
            parent.removeClass(filtered);
          }
        });

        function filterNavItem(name, text) {
          if (!text) return true;
          if (name && name.toLowerCase().indexOf(text.toLowerCase()) > -1) return true;
          return false;
        }
      });
      
      // toc filter clear button
      tocFilterClearButton.hide();
      tocFilterClearButton.on("click", function(){
        tocFilterInput.val("");
        tocFilterInput.trigger('input');
        if (typeof(Storage) !== "undefined") {
          sessionStorage.filterString = "";
        }
      });

      //Set toc filter from local session storage on page load
      if (typeof(Storage) !== "undefined") {
        tocFilterInput.val(sessionStorage.filterString);
        tocFilterInput.trigger('input');
      }
    }

    function loadToc() {
      let tocPath = $("meta[property='docfx\\:tocrel']").attr("content");
      if (!tocPath) {
        return;
      }
      tocPath = tocPath.replace(/\\/g, '/');
      $('#sidetoc').load(tocPath + " #sidetoggle > div", function () {
        const index = tocPath.lastIndexOf('/');
        let tocrel = '';
        if (index > -1) {
          tocrel = tocPath.substr(0, index + 1);
        }
        let currentHref = util.getCurrentWindowAbsolutePath();
        if(!currentHref.endsWith('.html')) {
          currentHref += '.html';
        }
        $('#sidetoc').find('a[href]').each(function (i, e) {
          let href = $(e).attr("href");
          if (util.isRelativePath(href)) {
            href = tocrel + href;
            $(e).attr("href", href);
          }

          if (util.getAbsolutePath(e.href) === currentHref) {
            $(e).addClass(active);
          }

          $(e).breakWord();
        });

        renderSidebar();
      });
    }
  }

  function renderBreadcrumb() {
    const breadcrumb = [];
    $('#navbar a.active').each(function (i, e) {
      breadcrumb.push({
        href: e.href,
        name: e.innerHTML
      });
    });
    $('#toc a.active').each(function (i, e) {
      breadcrumb.push({
        href: e.href,
        name: e.innerHTML
      });
    });

    const html = util.formList(breadcrumb, 'breadcrumb');
    $('#breadcrumb').html(html);
  }

  //Setup Affix
  function renderAffix() {
    const hierarchy = getHierarchy();
    if (!hierarchy || hierarchy.length <= 0) {
      $("#affix").hide();
    }
    else {
      const html = util.formList(hierarchy, ['nav', 'bs-docs-sidenav']);
      $("#affix>div").empty().append(html);
      if ($('footer').is(':visible')) {
        $(".sideaffix").css("bottom", "70px");
      }
      $('#affix a').click(function(e) {
        const scrollspy = $('[data-spy="scroll"]').data()['bs.scrollspy'];
        const target = e.target.hash;
        if (scrollspy && target) {
          scrollspy.activate(target);
        }
      });
    }

    function getHierarchy() {
      // supported headers are h1, h2, h3, and h4
      const $headers = $($.map(['h1', 'h2', 'h3', 'h4'], function (h) { return ".article article " + h; }).join(", "));

      // a stack of hierarchy items that are currently being built
      const stack = [];
      $headers.each(function (i, e) {
        if (!e.id) {
          return;
        }

        const item = {
          name: htmlEncode($(e).text()),
          href: "#" + e.id,
          items: []
        };

        if (!stack.length) {
          stack.push({ type: e.tagName, siblings: [item] });
          return;
        }

        const frame = stack[stack.length - 1];
        if (e.tagName === frame.type) {
          frame.siblings.push(item);
        } else if (e.tagName[1] > frame.type[1]) {
          // we are looking at a child of the last element of frame.siblings.
          // push a frame onto the stack. After we've finished building this item's children,
          // we'll attach it as a child of the last element
          stack.push({ type: e.tagName, siblings: [item] });
        } else {  // e.tagName[1] < frame.type[1]
          // we are looking at a sibling of an ancestor of the current item.
          // pop frames from the stack, building items as we go, until we reach the correct level at which to attach this item.
          while (e.tagName[1] < stack[stack.length - 1].type[1]) {
            buildParent();
          }
          if (e.tagName === stack[stack.length - 1].type) {
            stack[stack.length - 1].siblings.push(item);
          } else {
            stack.push({ type: e.tagName, siblings: [item] });
          }
        }
      });
      while (stack.length > 1) {
        buildParent();
      }

      function buildParent() {
        const childrenToAttach = stack.pop();
        const parentFrame = stack[stack.length - 1];
        const parent = parentFrame.siblings[parentFrame.siblings.length - 1];
        $.each(childrenToAttach.siblings, function (i, child) {
          parent.items.push(child);
        });
      }
      if (stack.length > 0) {

        const topLevel = stack.pop().siblings;
        if (topLevel.length === 1) {  // if there's only one topmost header, dump it
          return topLevel[0].items;
        }
        return topLevel;
      }
      return undefined;
    }

    function htmlEncode(str) {
      if (!str) return str;
      return str
        .replace(/&/g, '&amp;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
    }
  }

  // Show footer
  function renderFooter() {
    initFooter();
    $(window).on("scroll", showFooterCore);

    function initFooter() {
      if (needFooter()) {
        shiftUpBottomCss();
        $("footer").show();
      } else {
        resetBottomCss();
        $("footer").hide();
      }
    }

    function showFooterCore() {
      if (needFooter()) {
        shiftUpBottomCss();
        $("footer").fadeIn();
      } else {
        resetBottomCss();
        $("footer").fadeOut();
      }
    }

    function needFooter() {
      const scrollHeight = $(document).height();
      const scrollPosition = $(window).height() + $(window).scrollTop();
      return (scrollHeight - scrollPosition) < 1;
    }

    function resetBottomCss() {
      $(".sidetoc").removeClass("shiftup");
      $(".sideaffix").removeClass("shiftup");
    }

    function shiftUpBottomCss() {
      $(".sidetoc").addClass("shiftup");
      $(".sideaffix").addClass("shiftup");
    }
  }

  function renderLogo() {
    // For LOGO SVG
    // Replace SVG with inline SVG
    // http://stackoverflow.com/questions/11978995/how-to-change-color-of-svg-image-using-css-jquery-svg-image-replacement
    $('img.svg').each(function () {
      const $img = $(this);
      const imgID = $img.attr('id');
      const imgClass = $img.attr('class');
      const imgURL = $img.attr('src');

      $.get(imgURL, function (data) {
        // Get the SVG tag, ignore the rest
        let $svg = $(data).find('svg');

        // Add replaced image's ID to the new SVG
        if (typeof imgID !== 'undefined') {
          $svg = $svg.attr('id', imgID);
        }
        // Add replaced image's classes to the new SVG
        if (typeof imgClass !== 'undefined') {
          $svg = $svg.attr('class', imgClass + ' replaced-svg');
        }

        // Remove any invalid XML tags as per http://validator.w3.org
        $svg = $svg.removeAttr('xmlns:a');

        // Replace image with new SVG
        $img.replaceWith($svg);

      }, 'xml');
    });
  }

  function renderTabs() {
    const contentAttrs = {
      id: 'data-bi-id',
      name: 'data-bi-name',
      type: 'data-bi-type'
    };

    const Tab = (function () {
      function Tab(li, a, section) {
        this.li = li;
        this.a = a;
        this.section = section;
      }
      Object.defineProperty(Tab.prototype, "tabIds", {
        get: function () { return this.a.getAttribute('data-tab').split(' '); },
        enumerable: true,
        configurable: true
      });
      Object.defineProperty(Tab.prototype, "condition", {
        get: function () { return this.a.getAttribute('data-condition'); },
        enumerable: true,
        configurable: true
      });
      Object.defineProperty(Tab.prototype, "visible", {
        get: function () { return !this.li.hasAttribute('hidden'); },
        set: function (value) {
          if (value) {
            this.li.removeAttribute('hidden');
            this.li.removeAttribute('aria-hidden');
          }
          else {
            this.li.setAttribute('hidden', 'hidden');
            this.li.setAttribute('aria-hidden', 'true');
          }
        },
        enumerable: true,
        configurable: true
      });
      Object.defineProperty(Tab.prototype, "selected", {
        get: function () { return !this.section.hasAttribute('hidden'); },
        set: function (value) {
          if (value) {
            this.a.setAttribute('aria-selected', 'true');
            this.a.tabIndex = 0;
            this.section.removeAttribute('hidden');
            this.section.removeAttribute('aria-hidden');
          }
          else {
            this.a.setAttribute('aria-selected', 'false');
            this.a.tabIndex = -1;
            this.section.setAttribute('hidden', 'hidden');
            this.section.setAttribute('aria-hidden', 'true');
          }
        },
        enumerable: true,
        configurable: true
      });
      Tab.prototype.focus = function () {
        this.a.focus();
      };
      return Tab;
    }());

    initTabs(document.body);

    function initTabs(container) {
      const queryStringTabs = readTabsQueryStringParam();
      const elements = container.querySelectorAll('.tabGroup');
      const state = { groups: [], selectedTabs: [] };
      for (let i = 0; i < elements.length; i++) {
        const group = initTabGroup(elements.item(i));
        if (!group.independent) {
          updateVisibilityAndSelection(group, state);
          state.groups.push(group);
        }
      }
      container.addEventListener('click', function (event) { return handleClick(event, state); });
      if (state.groups.length === 0) {
        return state;
      }
      selectTabs(queryStringTabs);
      updateTabsQueryStringParam(state);
      notifyContentUpdated();
      return state;
    }

    function initTabGroup(element) {
      const group = {
        independent: element.hasAttribute('data-tab-group-independent'),
        tabs: []
      };
      let li = element.firstElementChild.firstElementChild;
      while (li) {
        const a = li.firstElementChild;
        a.setAttribute(contentAttrs.name, 'tab');
        const dataTab = a.getAttribute('data-tab').replace(/\+/g, ' ');
        a.setAttribute('data-tab', dataTab);
        const section = element.querySelector("[id=\"" + a.getAttribute('aria-controls') + "\"]");
        const tab = new Tab(li, a, section);
        group.tabs.push(tab);
        li = li.nextElementSibling;
      }
      element.setAttribute(contentAttrs.name, 'tab-group');
      element.tabGroup = group;
      return group;
    }

    function updateVisibilityAndSelection(group, state) {
      let anySelected = false;
      let firstVisibleTab;
      for (let _i = 0, _a = group.tabs; _i < _a.length; _i++) {
        const tab = _a[_i];
        tab.visible = tab.condition === null || state.selectedTabs.indexOf(tab.condition) !== -1;
        if (tab.visible) {
          if (!firstVisibleTab) {
            firstVisibleTab = tab;
          }
        }
        tab.selected = tab.visible && arraysIntersect(state.selectedTabs, tab.tabIds);
        anySelected = anySelected || tab.selected;
      }
      if (!anySelected) {
        for (let _b = 0, _c = group.tabs; _b < _c.length; _b++) {
          const tabIds = _c[_b].tabIds;
          for (let _d = 0, tabIds_1 = tabIds; _d < tabIds_1.length; _d++) {
            const tabId = tabIds_1[_d];
            const index = state.selectedTabs.indexOf(tabId);
            if (index === -1) {
              continue;
            }
            state.selectedTabs.splice(index, 1);
          }
        }
        const tab = firstVisibleTab;
        tab.selected = true;
        state.selectedTabs.push(tab.tabIds[0]);
      }
    }

    function getTabInfoFromEvent(event) {
      if (!(event.target instanceof HTMLElement)) {
        return null;
      }
      const anchor = event.target.closest('a[data-tab]');
      if (anchor === null) {
        return null;
      }
      const tabIds = anchor.getAttribute('data-tab').split(' ');
      const group = anchor.parentElement.parentElement.parentElement.tabGroup;
      if (group === undefined) {
        return null;
      }
      return { tabIds: tabIds, group: group, anchor: anchor };
    }

    function handleClick(event, state) {
      const info = getTabInfoFromEvent(event);
      if (info === null) {
        return;
      }
      event.preventDefault();
      info.anchor.href = 'javascript:';
      setTimeout(function () { return info.anchor.href = '#' + info.anchor.getAttribute('aria-controls'); });
      const tabIds = info.tabIds, group = info.group;
      const originalTop = info.anchor.getBoundingClientRect().top;
      if (group.independent) {
        for (let _i = 0, _a = group.tabs; _i < _a.length; _i++) {
          const tab = _a[_i];
          tab.selected = arraysIntersect(tab.tabIds, tabIds);
        }
      }
      else {
        if (arraysIntersect(state.selectedTabs, tabIds)) {
          return;
        }
        const previousTabId = group.tabs.filter(function (t) { return t.selected; })[0].tabIds[0];
        state.selectedTabs.splice(state.selectedTabs.indexOf(previousTabId), 1, tabIds[0]);
        for (let _b = 0, _c = state.groups; _b < _c.length; _b++) {
          const group_1 = _c[_b];
          updateVisibilityAndSelection(group_1, state);
        }
        updateTabsQueryStringParam(state);
      }
      notifyContentUpdated();
      const top = info.anchor.getBoundingClientRect().top;
      if (top !== originalTop && event instanceof MouseEvent) {
        window.scrollTo(0, window.pageYOffset + top - originalTop);
      }
    }

    function selectTabs(tabIds) {
      for (let _i = 0, tabIds_1 = tabIds; _i < tabIds_1.length; _i++) {
        const tabId = tabIds_1[_i];
        const a = document.querySelector(".tabGroup > ul > li > a[data-tab=\"" + tabId + "\"]:not([hidden])");
        if (a === null) {
          return;
        }
        a.dispatchEvent(new CustomEvent('click', { bubbles: true }));
      }
    }

    function readTabsQueryStringParam() {
      const qs = new URLSearchParams(window.location.search);
      const t = qs.get('tabs');
      if (!t) {
        return [];
      }
      return t.split(',');
    }

    function updateTabsQueryStringParam(state) {
      const qs = new URLSearchParams(window.location.search);
      qs.set('tabs', state.selectedTabs.join());
      const url = location.protocol + "//" + location.host + location.pathname + "?" + qs.toString() + location.hash;
      if (location.href === url) {
        return;
      }
      history.replaceState({}, document.title, url);
    }

    function arraysIntersect(a, b) {
      for (let _i = 0, a_1 = a; _i < a_1.length; _i++) {
        const itemA = a_1[_i];
        for (let _a = 0, b_1 = b; _a < b_1.length; _a++) {
          const itemB = b_1[_a];
          if (itemA === itemB) {
            return true;
          }
        }
      }
      return false;
    }

    function notifyContentUpdated() {
      // Dispatch this event when needed
      // window.dispatchEvent(new CustomEvent('content-update'));
    }
  }

  function utility() {
    this.getAbsolutePath = getAbsolutePath;
    this.isRelativePath = isRelativePath;
    this.isAbsolutePath = isAbsolutePath;
    this.getCurrentWindowAbsolutePath = getCurrentWindowAbsolutePath;
    this.getDirectory = getDirectory;
    this.formList = formList;

    function getAbsolutePath(href) {
      if (isAbsolutePath(href)) return href;
      const currentAbsPath = getCurrentWindowAbsolutePath();
      const stack = currentAbsPath.split("/");
      stack.pop();
      const parts = href.split("/");
      for (let i=0; i< parts.length; i++) {
        if (parts[i] == ".") continue;
        if (parts[i] == ".." && stack.length > 0)
          stack.pop();
        else
          stack.push(parts[i]);
      }
      const p = stack.join("/");
      return p;
    }

    function isRelativePath(href) {
      if (href === undefined || href === '' || href[0] === '/') {
        return false;
      }
      return !isAbsolutePath(href);
    }

    function isAbsolutePath(href) {
      return (/^(?:[a-z]+:)?\/\//i).test(href);
    }

    function getCurrentWindowAbsolutePath() {
      return window.location.origin + window.location.pathname;
    }
    function getDirectory(href) {
      if (!href) return '';
      const index = href.lastIndexOf('/');
      if (index == -1) return '';
      if (index > -1) {
        return href.substr(0, index);
      }
    }

    function formList(item, classes) {
      let level = 1;
      const model = {
        items: item
      };
      const cls = [].concat(classes).join(" ");
      return getList(model, cls);

      function getList(model, cls) {
        if (!model || !model.items) return null;
        const l = model.items.length;
        if (l === 0) return null;
        let html = '<ul class="level' + level + ' ' + (cls || '') + '">';
        level++;
        for (let i = 0; i < l; i++) {
          const item = model.items[i];
          const href = item.href;
          const name = item.name;
          if (!name) continue;
          html += href ? '<li><a href="' + href + '">' + name + '</a>' : '<li>' + name;
          html += getList(item, cls) || '';
          html += '</li>';
        }
        html += '</ul>';
        return html;
      }
    }

    /**
     * Add <wbr> into long word.
     * @param {String} text - The word to break. It should be in plain text without HTML tags.
     */
    function breakPlainText(text) {
      if (!text) return text;
      return text.replace(/([a-z])([A-Z])|(\.)(\w)/g, '$1$3<wbr>$2$4');
    }

    /**
     * Add <wbr> into long word. The jQuery element should contain no html tags.
     * If the jQuery element contains tags, this function will not change the element.
     */
    $.fn.breakWord = function () {
      if (!this.html().match(/(<\w*)((\s\/>)|(.*<\/\w*>))/g)) {
        this.html(function (index, text) {
          return breakPlainText(text);
        });
      }
      return this;
    };
  }

  // adjusted from https://stackoverflow.com/a/13067009/1523776
  function workAroundFixedHeaderForAnchors() {
    const HISTORY_SUPPORT = !!(history && history.pushState);
    const ANCHOR_REGEX = /^#[^ ]+$/;

    function getFixedOffset() {
      return $('header').first().height();
    }

    /**
     * If the provided href is an anchor which resolves to an element on the
     * page, scroll to it.
     * @param  {String} href
     * @return {Boolean} - Was the href an anchor.
     */
    function scrollIfAnchor(href, pushToHistory) {
      let rect, anchorOffset;

      if (!ANCHOR_REGEX.test(href)) {
        return false;
      }

      const match = document.getElementById(href.slice(1));

      if (match) {
        rect = match.getBoundingClientRect();
        anchorOffset = window.pageYOffset + rect.top - getFixedOffset();
        window.scrollTo(window.pageXOffset, anchorOffset);

        // Add the state to history as-per normal anchor links
        if (HISTORY_SUPPORT && pushToHistory) {
          history.pushState({}, document.title, location.pathname + href);
        }
      }

      return !!match;
    }

    /**
     * Attempt to scroll to the current location's hash.
     */
    function scrollToCurrent() {
      scrollIfAnchor(window.location.hash, false);
    }

    /**
     * If the click event's target was an anchor, fix the scroll position.
     */
    function delegateAnchors(e) {
      const elem = e.target;

      if (scrollIfAnchor(elem.getAttribute('href'), true)) {
        e.preventDefault();
      }
    }

    $(window).on('hashchange', scrollToCurrent);

    $(window).on('load', function () {
        // scroll to the anchor if present, offset by the header
        scrollToCurrent();
    });

    $(document).ready(function () {
        // Exclude tabbed content case
        $('a:not([data-tab])').click(function (e) { delegateAnchors(e); });
    });

    window._docfxReady = true;
  }
});
