<?xml version="1.0"?>
<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:input="urn:input-variables"
  exclude-result-prefixes="xsl input"
  version="1.1">

  <xsl:output method="xml" indent="yes" encoding="UTF-8" />

  <xsl:template match="para">
    <p>
      <xsl:apply-templates />
    </p>
  </xsl:template>

  <xsl:template match="c">
    <code data-inline="true">
      <xsl:apply-templates />
    </code>
  </xsl:template>

  <xsl:template match="value">
    <returns>
      <xsl:apply-templates />
    </returns>
  </xsl:template>

  <xsl:template match="see[@href and not(parent::member)]">
    <a>
      <xsl:apply-templates select="@*|node()"/>
      <xsl:if test="not(node())">
        <xsl:value-of select="@href"/>
      </xsl:if>
    </a>
  </xsl:template>

  <xsl:template match="seealso[@href and not(parent::member)]">
    <a>
      <xsl:apply-templates select="@*|node()"/>
      <xsl:if test="not(node())">
        <xsl:value-of select="@href"/>
      </xsl:if>
    </a>
  </xsl:template>

  <xsl:template match="paramref">
    <xsl:if test="normalize-space(@name)">
      <code data-inline="true" class="paramref">
        <xsl:value-of select="@name" />
      </code>
    </xsl:if>
  </xsl:template>

  <xsl:template match="typeparamref">
    <xsl:if test="normalize-space(@name)">
      <code data-inline="true" class="typeparamref">
        <xsl:value-of select="@name" />
      </code>
    </xsl:if>
  </xsl:template>

  <xsl:template match="languageKeyword">
    <code data-inline="true" class="languageKeyword">
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
                <xsl:for-each select="listheader/term">
                  <th class="term">
                    <xsl:apply-templates />
                  </th>
                </xsl:for-each>
                <xsl:for-each select="listheader/description">
                  <th class="description">
                    <xsl:apply-templates />
                  </th>
                </xsl:for-each>
              </tr>
            </thead>
          </xsl:if>
          <tbody>
            <xsl:for-each select="item">
              <tr>
                <xsl:for-each select="term">
                  <td class="term">
                    <xsl:apply-templates />
                  </td>
                </xsl:for-each>
                <xsl:for-each select="description">
                  <td class="description">
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
                  <xsl:choose>
                    <xsl:when test="self::node()[description|term]">
                      <xsl:apply-templates select="term" />
                      <xsl:apply-templates select="description" />
                    </xsl:when>
                    <xsl:otherwise>
                      <xsl:apply-templates />
                    </xsl:otherwise>
                  </xsl:choose>
                </li>
              </xsl:for-each>
            </ul>
          </xsl:when>
          <xsl:when test="$listtype = 'number'">
            <ol>
              <xsl:for-each select="item">
                <li>
                  <xsl:choose>
                    <xsl:when test="self::node()[description|term]">
                      <xsl:apply-templates select="term" />
                      <xsl:apply-templates select="description" />
                    </xsl:when>
                    <xsl:otherwise>
                      <xsl:apply-templates />
                    </xsl:otherwise>
                  </xsl:choose>
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
    <span class="term">
      <xsl:apply-templates />
    </span>
  </xsl:template>

  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

</xsl:stylesheet>
