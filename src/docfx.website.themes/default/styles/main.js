$(function () {
  var active = 'active';
  var expanded = 'in';
  var collapsed = 'collapsed';
  var filtered = 'filtered';
  var show = 'show';
  var hide = 'hide';
  $('.toc .nav > li > .expand-stub').click(function (e) {
    $(e.toElement).parent().toggleClass(expanded);
  });

  // For TOC FILTER
  (function () {
    $('#toc_filter_input').on('input', function (e) {
      var val = this.value;
      if (val === '') {
        // Clear 'filtered' class
        $('#toc li').removeClass(filtered).removeClass(hide);
        return;
      }

      // Get leaf nodes
      $('#toc li>a').filter(function (i, e) {
        return $(e).siblings().length === 0
      }).each(function (i, anchor) {
        var text = $(anchor).text();
        var parent = $(anchor).parent();
        var parentNodes = parent.parents('ul>li');
        for (var i = 0; i < parentNodes.length; i++) {
          var parentText = $(parentNodes[i]).children('a').text();
          if (parentText) text = parentText + '.' + text;
        };
        if (filterNavItem(text, val)) {
          parent.addClass(show);
          parent.removeClass(hide);
        } else {
          parent.addClass(hide);
          parent.removeClass(show);
        }
      });
      $('#toc li>a').filter(function (i, e) {
        return $(e).siblings().length > 0
      }).each(function (i, anchor) {
        var parent = $(anchor).parent();
        if (parent.find('li.show').length > 0) {
          parent.addClass(show);
          parent.addClass(filtered);
          parent.removeClass(hide);
        } else {
          parent.addClass(hide);
          parent.removeClass(show);
          parent.removeClass(filtered);
        }
      })
    });

    function filterNavItem(name, text) {
      if (!text) return true;
      if (name.toLowerCase().indexOf(text.toLowerCase()) > -1) return true;
      return false;
    }
  })();

  // Update href in navbar
  (function () {
    var toc = $('#sidetoc');
    var breadcrumb = new Breadcrumb();
    console.log("hello");
    loadNavbar();
    onTocLoaded(updateTocHref);

    function loadNavbar() {
      var navbarPath = $("meta[property='docfx\\:navrel']").attr("content");
      var tocPath = $("meta[property='docfx\\:tocrel']").attr("content");
      if (tocPath) tocPath = tocPath.replace(/\\/g, '/');
      if (navbarPath) navbarPath = navbarPath.replace(/\\/g, '/');
      $('#navbar').load(navbarPath + " #toc>ul", function () {
        var index = navbarPath.lastIndexOf('/');
        var navrel = '';
        if (index > -1) {
          navrel = navbarPath.substr(0, index + 1);
        }

        $('#navbar>ul').addClass('navbar-nav');
        var currentAbsPath = getAbsolutePath(window.location.pathname);
        // set active item
        $('#navbar').find('a[href]').each(function (i, e) {
          var href = $(e).attr("href");
          if (isRelativePath(href)) {
            href = navrel + href;
            $(e).attr("href", href);

            // TODO: currently only support one level navbar
            var isActive = false;
            var originalHref = e.name;
            if (originalHref) {
              originalHref = navrel + originalHref;
              if (getDirectory(getAbsolutePath(originalHref)) === getDirectory(getAbsolutePath(tocPath))) {
                isActive = true;
              }
            } else {
              if (getAbsolutePath(href) === currentAbsPath) {
                isActive = true;
              }
            }
            if (isActive) {
              $(e).parent().addClass(active);
              breadcrumb.insert({
                href: e.href,
                name: e.innerHTML
              }, 0);
            } else {
              $(e).parent().removeClass(active)
            }
          }
        });
      });
    }

    // $(iframe).load() is not always working
    function onTocLoaded(callback) {
      if (toc.length === 0) return;
      var iframe = $('body>*', toc.contents());
      if (iframe.length === 0) {
        setTimeout(function () {
          onTocLoaded(callback);
        }, 100);
        return;
      }
      callback();
    }

    function updateTocHref() {
      var currentHref = window.location.href;
      // remove hash
      var hashIndex = currentHref.indexOf('#');
      if (hashIndex > -1) currentHref = currentHref.substr(0, hashIndex);
      toc.contents().find('a[href]').each(function (i, e) {
        if (e.href === currentHref) {
          $(e).parent().addClass(active);
          var parent = $(e).parent().parents('li').children('a');
          if (parent.length > 0) {
            parent.addClass(active);
            breadcrumb.push({
              href: parent[0].href,
              name: parent[0].innerHTML
            });
          }
          // for active li, expand it
          $(e).parents('ul.nav>li').addClass(expanded);

          breadcrumb.push({
            href: e.href,
            name: e.innerHTML
          });
          // Scroll to active item
          var top = 0;
          $(e).parents('li').each(function (i, e) {
            top += $(e).position().top;
          });
          // 50 is the size of the filter box
          toc[0].contentWindow.scrollTo(0, top - 50);
        } else {
          $(e).parent().removeClass(active);
          $(e).parents('li').children('a').removeClass(active);
        }
      })
    }

    function Breadcrumb() {
      var breadcrumb = [];
      this.push = pushBreadcrumb;
      this.insert = insertBreadcrumb;

      function pushBreadcrumb(obj) {
        breadcrumb.push(obj);
        setupBreadCrumb(breadcrumb);
      }

      function insertBreadcrumb(obj, index) {
        breadcrumb.splice(index, 0, obj);
        setupBreadCrumb(breadcrumb);
      }

      function setupBreadCrumb() {
        var html = formList(breadcrumb, 'breadcrumb');
        $('#breadcrumb').html(html);
      }
    }

    function getAbsolutePath(href) {
      if (isAbsolutePath(href)) return href;
      // Use anchor to normalize href
      return $('<a href="' + href + '"></a>')[0].pathname;
    }

    function isRelativePath(href) {
      return !isAbsolutePath(href);
    }

    function isAbsolutePath(href) {
      return (/^(?:[a-z]+:)?\/\//i).test(href);
    }

    function getDirectory(href) {
      if (!href) return '';
      var index = href.lastIndexOf('/');
      if (index == -1) return '';
      if (index > -1) {
        return href.substr(0, index);
      }
    }

    function getRelativePath(key) {
      var rel = $("meta[property='docfx\\:" + key + "'").attr("content");
      if (typeof rel === 'undefined') rel = ''; // If relative path is not set, set to current folder
      if (rel && rel[rel.length - 1] !== '/') rel += '/';
      return rel;
    }
  })();

  //Setup Affix
  (function () {
    var hierarchy = getHierarchy();
    if (hierarchy.length > 0) {
      var html = '<h5 class="title">In This Article</h5>'
      html += formList(hierarchy, ['nav', 'bs-docs-sidenav']);
      $("#affix").append(html);
    }

    function getHierarchy() {
      // supported headers are h1, h2, h3, and h4
      // The topest header is ignored
      var selector = ".article article";
      var affixSelector = "#affix";
      var headers = ['h4', 'h3', 'h2', 'h1'];
      var hierarchy = [];
      var toppestIndex = -1;
      var startIndex = -1;
      // 1. get header hierarchy
      for (var i = headers.length - 1; i >= 0; i--) {
        var header = $(selector + " " + headers[i]);
        var length = header.length;

        // If contains no header in current selector, find the next one
        if (length === 0) continue;

        // If the toppest header contains only one item, e.g. title, ignore
        if (length === 1 && hierarchy.length === 0 && toppestIndex < 0) {
          toppestIndex = i;
          continue;
        }

        // Get second level children
        var nextLevelSelector = i > 0 ? headers[i - 1] : null;
        var prevSelector;
        for (var j = length - 1; j >= 0; j--) {
          var e = header[j];
          var id = e.id;
          if (!id) continue; // For affix, id is a must-have
          var item = {
            name: e.innerHTML,
            href: "#" + id,
            items: []
          };
          if (nextLevelSelector) {
            var selector = '#' + id + "~" + nextLevelSelector;
            var currentSelector = selector;
            if (prevSelector) currentSelector += ":not(" + prevSelector + ")";
            $(header[j]).siblings(currentSelector).each(function (index, e) {
              if (e.id) {
                item.items.push({
                  name: e.innerHTML, // innerText decodes text while innerHTML not
                  href: "#" + e.id

                })
              }
            })
            prevSelector = selector;
          }
          hierarchy.push(item);
        }
        break;
      };
      hierarchy.reverse();
      return hierarchy;
    }
  })();

  function formList(item, classes) {
    var level = 1;
    var model = {
      items: item
    };
    var cls = [].concat(classes).join(" ");
    return getList(model, cls);

    function getList(model, cls) {
      if (!model || !model.items) return null;
      var l = model.items.length;
      if (l === 0) return null;
      var html = '<ul class="level' + level + ' ' + (cls || '') + '">';
      level++;
      for (var i = 0; i < l; i++) {
        var item = model.items[i];
        var href = item.href;
        var name = item.name;
        if (!name) continue;
        html += href ? '<li><a href="' + href + '">' + name + '</a>' : '<li>' + name;
        html += getList(item, cls) || '';
        html += '</li>';
      }
      html += '</ul>';
      return html;
    }
  }

  // For LOGO SVG
  // Replace SVG with inline SVG
  // http://stackoverflow.com/questions/11978995/how-to-change-color-of-svg-image-using-css-jquery-svg-image-replacement
  jQuery('img.svg').each(function () {
    var $img = jQuery(this);
    var imgID = $img.attr('id');
    var imgClass = $img.attr('class');
    var imgURL = $img.attr('src');

    jQuery.get(imgURL, function (data) {
      // Get the SVG tag, ignore the rest
      var $svg = jQuery(data).find('svg');

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
})