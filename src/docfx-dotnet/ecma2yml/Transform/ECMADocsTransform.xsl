<?xml version="1.0"?>
<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:input="urn:input-variables"
  exclude-result-prefixes="xsl input"
  version="1.1">

  <xsl:output method="xml" indent="yes" encoding="UTF-8" />

  <xsl:variable name="smallcase" select="'abcdefghijklmnopqrstuvwxyz'" />
  <xsl:variable name="uppercase" select="'ABCDEFGHIJKLMNOPQRSTUVWXYZ'" />

  <xsl:template match="para">
    <p>
      <xsl:apply-templates />
    </p>
  </xsl:template>

  <xsl:template match="b">
    <strong>
      <xsl:apply-templates />
    </strong>
  </xsl:template>

  <xsl:template match="i">
    <em>
      <xsl:apply-templates />
    </em>
  </xsl:template>

  <xsl:template match="ui">
    <strong>
      <xsl:apply-templates />
    </strong>
  </xsl:template>

  <xsl:template match="c">
    <code data-dev-comment-type="c">
      <xsl:apply-templates />
    </code>
  </xsl:template>

  <xsl:template match="code">
    <xsl:choose>
      <xsl:when test="contains(., '&#10;')">
        <xsl:text>&#10;</xsl:text>
        <xsl:choose>
          <xsl:when test="@language = 'C#' or @language = 'c#' or @lang = 'C#' or @lang = 'c#'">
            <xsl:text>```csharp</xsl:text>
          </xsl:when>
          <xsl:when test="normalize-space(@language)">
            <xsl:text>```</xsl:text><xsl:value-of select="@language" />
          </xsl:when>
          <xsl:when test="normalize-space(@lang)">
            <xsl:text>```</xsl:text><xsl:value-of select="@lang" />
          </xsl:when>
          <xsl:otherwise>
            <xsl:text>```</xsl:text>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:text>&#10;</xsl:text>
        <xsl:apply-templates />
        <xsl:text>&#10;```</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <code><xsl:apply-templates /></code>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="value">
    <returns>
      <xsl:apply-templates />
    </returns>
  </xsl:template>

  <xsl:template match="img[@href]">
    <img>
      <xsl:attribute name="src">
        <xsl:value-of select="@href"/>
      </xsl:attribute>
    <xsl:apply-templates />
    </img>
  </xsl:template>

  <xsl:template match="see[@cref and not(parent::member)]">
    <xsl:choose>
      <xsl:when test="contains(normalize-space(@cref), 'Overload:')">
        <xref uid="@cref" data-throw-if-not-resolved="true">
          <xsl:attribute name="uid">
            <xsl:value-of select="concat(substring-after(@cref, ':'), '*')"/>
          </xsl:attribute>
        </xref>
      </xsl:when>
      <xsl:when test="contains(normalize-space(@cref), ':')">
        <xref uid="@cref" data-throw-if-not-resolved="true">
          <xsl:attribute name="uid">
            <xsl:value-of select="substring-after(@cref, ':')"/>
          </xsl:attribute>
        </xref>
      </xsl:when>
      <xsl:otherwise>
        <xref uid="@cref" data-throw-if-not-resolved="true">
          <xsl:attribute name="uid">
            <xsl:value-of select="normalize-space(@cref)"/>
          </xsl:attribute>
        </xref>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="see[@href and not(parent::member)]">
    <xsl:choose>
      <xsl:when test="text()!=''">
        <a>
          <xsl:apply-templates select="@*|node()"/>
        </a>
      </xsl:when>
      <xsl:otherwise>
        <a>
          <xsl:apply-templates select="@*|node()"/>
          <xsl:value-of select="normalize-space(@href)"/>
        </a>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="seealso[@href and not(parent::member) and not(parent::Docs)]">
     <xsl:choose>
      <xsl:when test="text()!=''">
        <a>
          <xsl:apply-templates select="@*|node()"/>
        </a>
      </xsl:when>
      <xsl:otherwise>
        <a>
          <xsl:apply-templates select="@*|node()"/>
          <xsl:value-of select="normalize-space(@href)"/>
        </a>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="paramref">
    <xsl:if test="normalize-space(@name)">
      <code data-dev-comment-type="paramref">
        <xsl:value-of select="@name" />
      </code>
    </xsl:if>
  </xsl:template>

  <xsl:template match="typeparamref">
    <xsl:if test="normalize-space(@name)">
      <code data-dev-comment-type="typeparamref">
        <xsl:value-of select="@name" />
      </code>
    </xsl:if>
  </xsl:template>

  <xsl:template match="languageKeyword">
    <code data-dev-comment-type="languageKeyword">
      <xsl:apply-templates />
    </code>
  </xsl:template>

  <xsl:template match="see[@langword]">
    <code data-dev-comment-type="langword">
      <xsl:value-of select="@langword"/>
    </code>
  </xsl:template>

  <xsl:template match="list">
    <xsl:variable name="listtype">
      <xsl:value-of select="normalize-space(@type)"/>
    </xsl:variable>
    <xsl:choose>
      <xsl:when test="$listtype = 'table'">
        <table>
          <xsl:if test="listheader">
            <thead>
              <tr>
                <th>
                  <xsl:apply-templates select="listheader/term" />
                </th>
                <xsl:for-each select="listheader/description">
                  <th>
                    <xsl:apply-templates />
                  </th>
                </xsl:for-each>
              </tr>
            </thead>
          </xsl:if>
          <tbody>
            <xsl:for-each select="item">
              <tr>
                <td>
                  <xsl:apply-templates select="term"/>
                </td>
                <xsl:for-each select="description">
                  <td>
                    <xsl:apply-templates />
                  </td>
                </xsl:for-each>
              </tr>
            </xsl:for-each>
          </tbody>
        </table>
      </xsl:when>
      <xsl:otherwise>
        <xsl:if test="listheader">
          <p>
            <strong>
              <xsl:if test="listheader/term">
                <xsl:value-of select="concat(string(listheader/term),'-')"/>
              </xsl:if>
              <xsl:value-of select="string(listheader/description)" />
            </strong>
          </p>
        </xsl:if>
        <xsl:choose>
          <xsl:when test="$listtype = 'bullet'">
            <ul>
              <xsl:for-each select="item">
                <li>
                  <xsl:apply-templates select="term" />
                  <xsl:apply-templates select="description" />
                </li>
              </xsl:for-each>
            </ul>
          </xsl:when>
          <xsl:when test="$listtype = 'number'">
            <ol>
              <xsl:for-each select="item">
                <li>
                  <xsl:apply-templates select="term" />
                  <xsl:apply-templates select="description" />
                </li>
              </xsl:for-each>
            </ol>
          </xsl:when>
        </xsl:choose>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="example[parent::remarks]">
    <div>
       <xsl:apply-templates />
    </div>
  </xsl:template>

  <xsl:template match="description">
    <xsl:apply-templates />
  </xsl:template>

  <xsl:template match="term">
    <xsl:apply-templates />
  </xsl:template>

  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="format[@type='text/html']">
    <xsl:apply-templates />
  </xsl:template>

</xsl:stylesheet>