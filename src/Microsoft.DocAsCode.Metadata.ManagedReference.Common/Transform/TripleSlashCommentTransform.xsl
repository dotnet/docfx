<?xml version="1.0"?>
<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:input="urn:input-variables"
  exclude-result-prefixes="xsl input"
  version="1.1">

  <xsl:output method="xml" indent="yes" encoding="UTF-8" />

  <xsl:param name="input:language"/>

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
    <code>
      <xsl:apply-templates />
    </code>
  </xsl:template>

  <xsl:template match="code">
    <xsl:element name="pre">
      <xsl:element name="code">
        <xsl:if test="normalize-space(@language)">
          <xsl:attribute name="class">lang-<xsl:value-of select="@language" /></xsl:attribute>
        </xsl:if>
        <xsl:copy-of select="@source|@region"/>
        <xsl:apply-templates />
      </xsl:element>
    </xsl:element>
  </xsl:template>

  <xsl:template match="value">
    <returns>
      <xsl:apply-templates />
    </returns>
  </xsl:template>

  <xsl:template match="see[@langword]">
    <xref>
      <xsl:attribute name="uid">
        <xsl:value-of select="concat('langword_', $input:language, '_', @langword)"/>
      </xsl:attribute>
      <xsl:attribute name="name">
        <xsl:value-of select="@langword"/>
      </xsl:attribute>
      <xsl:attribute name="href">
      </xsl:attribute>
    </xref>
  </xsl:template>

  <xsl:template match="see[@href and not(parent::member)]">
    <a>
      <xsl:apply-templates select="@*|node()"/>
      <xsl:if test="not(text())">
        <xsl:value-of select="@href"/>
      </xsl:if>
    </a>
  </xsl:template>

  <xsl:template match="seealso[@href and not(parent::member)]">
    <a>
      <xsl:apply-templates select="@*|node()"/>
      <xsl:if test="not(text())">
        <xsl:value-of select="@href"/>
      </xsl:if>
    </a>
  </xsl:template>

  <xsl:template match="paramref">
    <xsl:if test="normalize-space(@name)">
      <code data-dev-comment-type="paramref" class="paramref">
        <xsl:value-of select="@name" />
      </code>
    </xsl:if>
  </xsl:template>

  <xsl:template match="typeparamref">
    <xsl:if test="normalize-space(@name)">
      <code data-dev-comment-type="typeparamref" class="typeparamref">
        <xsl:value-of select="@name" />
      </code>
    </xsl:if>
  </xsl:template>

  <xsl:template match="languageKeyword">
    <code data-dev-comment-type="languageKeyword" class="languageKeyword">
      <xsl:apply-templates />
    </code>
  </xsl:template>

  <xsl:template match="note">
    <xsl:variable name="type">
      <xsl:choose>
        <xsl:when test="not(normalize-space(@type) = '')">
          <xsl:value-of select="@type"/>
        </xsl:when>
        <xsl:otherwise>note</xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <div>
      <xsl:attribute name="class">
        <xsl:choose>
          <xsl:when test="$type = 'tip'">TIP</xsl:when>
          <xsl:when test="$type = 'warning'">WARNING</xsl:when>
          <xsl:when test="$type = 'caution'">CAUTION</xsl:when>
          <xsl:when test="$type = 'important'">IMPORTANT</xsl:when>
          <xsl:otherwise>NOTE</xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
      <h5>
        <xsl:value-of select="$type"/>
      </h5>
      <p>
        <xsl:apply-templates select="node()"/>
      </p>
    </div>
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
                <th>
                  <xsl:apply-templates select="listheader/description" />
                </th>
              </tr>
            </thead>
          </xsl:if>
          <tbody>
            <xsl:for-each select="item">
              <tr>
                <td>
                  <xsl:apply-templates select="term"/>
                </td>
                <td>
                  <xsl:apply-templates select="description"/>
                </td>
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

</xsl:stylesheet>
