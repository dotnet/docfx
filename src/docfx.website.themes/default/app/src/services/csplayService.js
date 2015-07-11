/*
 * Define doc related functions used in docsApp
 * Wrap Angular components in an Immediately Invoked Function Expression (IIFE)
 * to avoid variable collisions
 */

(function() {
  'use strict';
  /*jshint validthis:true */
  function provider() {
    var player;
    function csplay(player, compileServiceBaseUrl) {
      if (typeof compileServiceBaseUrl === "undefined") {
        compileServiceBaseUrl = "";
      }
      if (compileServiceBaseUrl.substr(-1, 1) !== "/") {
        compileServiceBaseUrl += "/";
      }
      if (typeof player === "string") {
        player = document.getElementById(player);
      }
      // Create editor, split bar and output
      var top = document.createElement("div");
      var splitbar = document.createElement("div");
      var bottom = document.createElement("div");
      top.innerHTML = player.innerHTML;
      top.className = "csplay_editor";
      splitbar.className = "csplay_splitbar";
      bottom.className = "csplay_output";
      var cloned = player.cloneNode(false);
      player.parentNode.replaceChild(cloned, player);
      player = cloned;
      player.appendChild(top);
      player.appendChild(splitbar);
      player.appendChild(bottom);
      // Create ace editor
      var editor = ace.edit(top);
      editor.getSession().setMode("ace/mode/csharp");
      // Use jquery from here
      player = $(player);
      top = $(top);
      splitbar = $(splitbar);
      bottom = $(bottom);
      // Make splitbar draggable
      var dragging = false;
      splitbar.mousedown(function(e) {
        dragging = true;
      });
      player.mouseup(function() {
        dragging = false;
      }).mousemove(function(e) {
        if (dragging) {
          var topHeight = e.pageY - top.offset().top;
          var bottomHeight = top.outerHeight(true) + bottom.outerHeight(true) - topHeight;
          var splitbarHeight = splitbar.outerHeight(true);
          if (topHeight > 0 && bottomHeight > 0) {
            top.css("height", topHeight);
            bottom.css("height", "calc(100% - " + (topHeight + splitbarHeight) + "px)");
            editor.resize();
          }
          e.preventDefault();
        }
      });
      return {
        run: function(callback) {
          $.ajax({
            url: compileServiceBaseUrl + "run",
            type: "POST",
            data: editor.getValue(),
            contentType: "text/plain",
            success: function(data, status, xhr) {
              bottom.html(data).removeClass("error");
              if (typeof callback.success === "function") {
                callback.success(data, status, xhr);
              }
            },
            error: function(xhr, status, message) {
              if (typeof xhr.responseJSON.error_message === "string") {
                bottom.text(xhr.responseJSON.error_message).addClass("error");
              } else {
                bottom.text(xhr.responseText).addClass("error");
              }
              if (typeof callback.error === "function") {
                callback.error(xhr, status, message);
              }
            },
            complete: function(xhr, status) {
              if (typeof callback.complete === "function") {
                callback.complete(xhr, status);
              }
            }
          });
        },
        clearOutput: function() {
          bottom.html("");
        },
        editor: editor
      };
    }

    function createPlayer() {
      var player = csplay("player", "http://dotnetsandbox.azurewebsites.net" /* hardcode for now */ );
      player.editor.setTheme("ace/theme/ambiance");
      player.editor.setFontSize(16);
      $("#run").click(function() {
        var that = $(this);
        that.html('<i class="fa fa-refresh fa-fw fa-spin"></i>Run');
        that.addClass("disabled");
        player.run({
          complete: function() {
            that.text("Run");
            that.removeClass("disabled");
          }
        });
      });
      $("#close").click(function() {
        angular.element("#console").css("margin-left", "100%");
        if (player) player.editor.setReadOnly(true);
      });
      return player;
    }

    this.tryCode = function(enable, code) {
      if (enable) {
        if (typeof player === "undefined") {
          player = createPlayer();
        }
        player.editor.setValue(code, -1);
        player.editor.clearSelection();
        player.clearOutput();
        angular.element("#console").css("margin-left", "50%");
      } else {
        angular.element("#console").css("margin-left", "100%");
      }
      if (typeof player !== "undefined") {
        player.editor.setReadOnly(!enable);
      }
    };
  }


  angular.module('docascode.csplayService', ['docascode.constants', 'docascode.util'])
    .service('csplayService', ['$q', '$http', 'docConstants', 'docUtility', provider]);

})();