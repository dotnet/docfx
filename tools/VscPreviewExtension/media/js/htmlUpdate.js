// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
$(function () {
  var port = $("meta[name='port']")[0].content;
  var pageRefreshFunctionName = "refresh";
  var filePath = $("meta[name='filePath']")[0].content;
  var lastLocation = 0;
  var filePathEscape = filePath.replace(/\\/g, "\\\\");
  var rightClick = false;

  setInterval(function () {
    // VSCode bug: https://github.com/Microsoft/vscode/issues/23020
    // TODO: Remove when bug fixed
    $("link").each(function (index) {
      var path = $(this).attr("href");
      if (path.startsWith("https"))
        $(this).attr("href", path.replace("https", "http"));
    });

    $.get("http://localhost:" + port.toString() + "/previewContent")
      .done(function (data) {
        if (data.isMarkdownFileChanged) {
          window[pageRefreshFunctionName](data.markupResult);

          (function () {
            $("[sourcefile]").click(function () {
              if ($(this).attr('sourcefile') === filePath) {
                rightClick = true;
                $.get("http://localhost:" + [port.toString(), "MatchFromRightToLeft", $(this).attr('sourcestartlinenumber'), $(this).attr('sourceendlinenumber')].join("/"));
              }
              else {
                // TODO: add the lineNumber information of file include in Html
              }
            });
          })();
        }
      })

    // TODO: Merge this with previewMatch.js
    // Communication with extension to get the selection range of activeEditor
    $.get("http://localhost:" + port.toString() + "/MatchFromLeftToRight")
      .done(function (data) {
        var editorSelectionRange = data.split(" ");
        var currentLocation = parseInt(editorSelectionRange[0]);
        // Focus on the corresponding line only when the editor selection range changed
        if (lastLocation !== currentLocation) {
          if (rightClick) {
            lastLocation = currentLocation;
            rightClick = false;
            return;
          }
          var centerLocation = currentLocation;
          var selectItem = $("[sourcefile='" + filePathEscape + "']").filter(function (index) { return $(this).attr('sourcestartlinenumber') <= centerLocation && $(this).attr('sourceendlinenumber') >= centerLocation }).last();
          // If result of selection is empty selection, focus on the end of last node
          while (selectItem.length === 0) {
            centerLocation--;
            selectItem = $("[sourcefile='" + filePathEscape + "']").filter(function (index) { return $(this).attr('sourcestartlinenumber') <= centerLocation && $(this).attr('sourceendlinenumber') >= centerLocation }).last();
          }
          $("body,html").animate({
            scrollTop: selectItem.offset().top
          }, 0);
          lastLocation = currentLocation;
        }
      })
  }, 500);
})