// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

function buildTree(containerName, treeData) {
  var nodeCount = 0;
  var durationTimeInMilliSecond = 750;
  var maxLabelLengthOfEachLevel = [0];
  var port = document.body.childNodes[1].data;

  var width = $(document).width();
  var height = $(document).height();

  var tree = d3.layout.tree()
    .separation(function (a, b) { return a.parent = b.parent ? 1 : 2; });

  var diagonal = d3.svg.diagonal()
    .projection(function (d) { return [d.y, d.x]; });

  // Call visit function to establish maxLabelLength of every level
  visit(treeData, 0, function (d, level) {
    var tmp = level;
    // If the node has children , the text(node's name) will be displayed on the head of node , or on the right
    if (!(d.children && d.children.length > 0))
      tmp++;
    // 'while' but not 'if' because it may skip the next level
    while (maxLabelLengthOfEachLevel.length <= tmp)
      maxLabelLengthOfEachLevel.push(minHorizontalSpace);
    maxLabelLengthOfEachLevel[tmp] = Math.min(Math.max(maxLabelLengthOfEachLevel[tmp], getTokenName(d.name).length), maxHorizontalSpace);
  }, function (d) {
    return d.children && d.children.length > 0 ? d.children : null;
  })

  var zoomListener = d3.behavior.zoom().scaleExtent([0.1, 3])
    .on("zoom", function () {
      vis.attr("transform", "translate(" + d3.event.translate + ")scale(" + d3.event.scale + ")");
    });

  // Define the vis, attaching a class for styling and the zoomListener
  var vis = d3.select(containerName).append("svg:svg")
    .attr("width", width)
    .attr("height", height)
    .call(zoomListener)
    .append("svg:g");

  // Define the root
  var root = treeData;
  root.x0 = height / 2;
  root.y0 = width / 4;

  // Layout the tree initially and center on the root node.
  update(root);
  centerNode(root);

  // Refresh the viewSize
  var waiting = false;
  $(window).resize(function () {
    if (!waiting) {
      waiting = true;
      setTimeout(function () {
        waiting = false;
        d3.select("svg")
          .attr("width", $(document).width())
          .attr("height", $(document).height());
      }, 500);
    }
  })

  // If selection range intersect with token range
  function isIntersect(tokenStart, tokenEnd, selectionSatrt, selectionEnd) {
    return Math.max(tokenStart, selectionSatrt) <= Math.min(tokenEnd, selectionEnd);
  }

  var currrentCenterNode = root;
  var targetNode = root;

  // Communication with extension to get the selection startLineNumber and endLinerNumber
  setInterval(function () {
    $.get("http://localhost:" + port.toString() + "/MatchFromLeftToRight")
      .done(function (data) {
        // Hightlight the circle of chosen node
        var linenumber = data.split(" ");
        vis.selectAll("g.node")
          .select("circle")
          .style("fill", function (d) {
            if (isIntersect(parseInt(getStartLineNumber(d.name)), parseInt(getEndLineNumber(d.name)), parseInt(linenumber[0]), parseInt(linenumber[1]))) {    //centerNode(d);
              targetNode = d;
              return selectedGold
            } else {
              return d._children ? "lightsteelblue" : "white";
            }
          });

        // Hight the text of chosen node
        vis.selectAll("g.node")
          .select("text")
          .style("fill", function (d) {
            if (isIntersect(parseInt(getStartLineNumber(d.name)), parseInt(getEndLineNumber(d.name)), parseInt(linenumber[0]), parseInt(linenumber[1]))) {    //centerNode(d);
              targetNode = d;
              return selectedGold
            } else {
              return unSelectedGray;
            }
          });
      })
    if (targetNode !== currrentCenterNode) {
      centerNode(targetNode);
      currrentCenterNode = targetNode;
    }
  }, 500);

  function update(source) {
    var levelWidth = [1];
    // Count the total number of nodes in every level to compute the height of tree
    var childCount = function (level, n) {
      if (n.children && n.children.length > 0) {
        if (levelWidth.length <= level + 1)
          levelWidth.push(0);
        levelWidth[level + 1] += n.children.length;
        n.children.forEach(function (d) {
          childCount(level + 1, d);
        });
      }
    };
    childCount(0, root);
    var newHeight = d3.max(levelWidth) * 40; // 40 pixels per line  
    tree = tree.size([newHeight, width]);

    // Compute the new tree layout.
    var nodes = tree.nodes(root).reverse();

    // Set widths between levels based on maxLabelLength.
    nodes.forEach(function (d) {
      d.y = getoffset(d.depth);
    });

    // Compute the offet of node on the node.depth and maxLabelLengthOfeachLevel
    function getoffset(level) {
      var count = 0;
      for (var i = 1; i <= level; i++) {
        count += maxLabelLengthOfEachLevel[i] * (22 - i * 2);
      }
      return count;
    }

    // Update the nodes...
    node = vis.selectAll("g.node")
      .data(nodes, function (d) { return d.id || (d.id = ++nodeCount); });

    // Enter any new nodes at the parent's previous position.
    var nodeEnter = node.enter().append("svg:g")
      .attr("class", "node")
      .attr("transform", function (d) { return "translate(" + source.y0 + "," + source.x0 + ")"; });

    nodeEnter.append("svg:circle")
      .attr("r", 1e-6)
      .style("fill", function (d) { return d._children ? "steelblue" : "white"; })
      .on('click', circleClick);;

    nodeEnter.append("svg:text")
      .attr("x", function (d) { return d.children || d._children ? -10 : 10; })
      .attr("dy", ".35em")
      .attr("y", function (d) {
        if (d.depth === 0)
          return 0;
        else
          return d.children || d._children ? -15 : 0;
      })
      .attr("text-anchor", function (d) {
        if (d.depth === 0)
          return "end";
        else
          return d.children || d._children ? "middle" : "start";
      })
      .text(function (d) { return getTokenName(d.name); })
      .attr("font-size", function (d) { return (20 - d.depth * 4) < 11 ? 11 : (25 - d.depth * 4); })
      .on('click', textClick);

    nodeEnter.append("title")
      .text(function (d) { return getAltContent(d.name); });

    // Transition nodes to their new position.
    var nodeUpdate = node.transition()
      .duration(durationTimeInMilliSecond)
      .attr("transform", function (d) { return "translate(" + d.y + "," + d.x + ")"; });

    nodeUpdate.select("circle")
      .attr("r", 4.5)
      .style("fill", function (d) { return d._children ? "lightsteelblue" : "white"; });

    // Fade the text in
    nodeUpdate.select("text")
      .style("fill-opacity", 1);

    // Transition exiting nodes to the parent's new position.
    var nodeExit = node.exit().transition()
      .duration(durationTimeInMilliSecond)
      .attr("transform", function (d) { return "translate(" + source.y + "," + source.x + ")"; })
      .remove();

    nodeExit.select("circle")
      .attr("r", 1e-6);

    nodeExit.select("text")
      .style("fill-opacity", 1e-6);

    // Update the links
    var link = vis.selectAll("path.link")
      .data(tree.links(nodes), function (d) {
        return d.target.id;
      });

    // Enter any new links at the parent's previous position.
    link.enter().insert("path", "g")
      .attr("class", "link")
      .attr("d", function (d) {
        var o = { x: source.x0, y: source.y0 };
        return diagonal({ source: o, target: o });
      });

    // Transition links to their new position.
    link.transition()
      .duration(durationTimeInMilliSecond)
      .attr("d", diagonal);

    // Transition exiting nodes to the parent's new position.
    link.exit().transition()
      .duration(durationTimeInMilliSecond)
      .attr("d", function (d) {
        var o = { x: source.x, y: source.y };
        return diagonal({ source: o, target: o });
      })
      .remove();

    // Stash the old positions for transition.
    nodes.forEach(function (d) {
      d.x0 = d.x;
      d.y0 = d.y;
    });
  }

  // Function to center node when clicked so node will be tranlated to the center(depend on the depth)
  function centerNode(source) {
    scale = zoomListener.scale();
    x = -source.y0;
    y = -source.x0;
    x = x * scale + width / 3 + source.depth * 100;   // Center of different level will be different
    y = y * scale + height / 2;
    d3.select('g').transition()
      .duration(durationTimeInMilliSecond)
      .attr("transform", "translate(" + x + "," + y + ")scale(" + scale + ")");
    zoomListener.scale(scale)
      .translate([x, y]);
  }

  // Toggle children on click.
  function circleClick(d) {
    if (d.children) {
      d._children = d.children;
      d.children = null;
    } else {
      d.children = d._children;
      d._children = null;
    }
    update(d);
    centerNode(d);
  }

  // Map to left editor when click the text
  function textClick(d) {
    $.get("http://localhost:" + [port.toString(), "MatchFromRightToLeft", getStartLineNumber(d.name), getEndLineNumber(d.name)].join("/"));
  }
}

// Get the token type from name
function getTokenName(name) {
  var info = name.split(">");
  if (info.length >= 3)
    return info[2];
  return "";
}

// Get linenum from name
function getStartLineNumber(name) {
  var info = name.split(">");
  if (info.length >= 1)
    return info[0];
  return "";
}

function getEndLineNumber(name) {
  var info = name.split(">");
  if (info.length >= 2)
    return info[1];
  return "";
}

// Get the content from name
function getAltContent(name) {
  var info = name.split(">");
  if (info.length >= 4)
    return info[3].replace(/&quot;/g, "\"").replace(/&#39/g, "\'");
  return "";
}

function visit(parent, level, visitFn, childrenFn) {
  if (!parent)
    return;
  visitFn(parent, level);
  var children = childrenFn(parent);
  if (children) {
    children.forEach(function (child) {
      visit(child, level + 1, visitFn, childrenFn);
    }, this);
  }
}
