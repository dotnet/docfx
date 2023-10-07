// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * This file is used to generate the API page JSON schema. To generate the schema:
 *   1. run `npx typescript-json-schema ApiPage.schema.d.ts ApiPage --required --strictNullChecks --out ApiPage.schema.json`
 *   2. manually mark properties as markdown
 */

/** Define the markdown content type */
type markdown = string;

/** Represents an inline composed of text or links */
type Inline = string | (string | { text: string; url?: string })[];

/** Represents a markdown block */
type Markdown = {
  /** Markdown content */
  markdown: markdown;
};

/** Represents a heading */
type Heading =
  | { /** Heading title */ h1: string; /** URL fragment */ id?: string }
  | { /** Heading title */ h2: string; /** URL fragment */ id?: string }
  | { /** Heading title */ h3: string; /** URL fragment */ id?: string }
  | { /** Heading title */ h4: string; /** URL fragment */ id?: string }
  | { /** Heading title */ h5: string; /** URL fragment */ id?: string }
  | { /** Heading title */ h6: string; /** URL fragment */ id?: string };

/** Represents an API heading */
type Api = (
  | { /** API name */ api1: string }
  | { /** API name */ api2: string }
  | { /** API name */ api3: string }
  | { /** API name */ api4: string }
) & {
  /** API URL fragment */
  id?: string;

  /** Is this API deprecated, or the deprecation reason */
  deprecated?: boolean | string;

  /** API source URL */
  src?: string;

  /** Opaque metadata about the API as HTML data-* attributes  */
  metadata?: { [key: string]: string };
};

/** Represents a sheet of facts */
type Facts = {
  facts: {
    name: string;
    value: Inline;
  }[];
};

/** Represents a list of content */
type List = {
  list: Inline[];
};

/** Represents a single inheritance chain from base type to derived type */
type Inheritance = {
  inheritance: Inline[];
};

/** Represents a code block */
type Code = {
  /** Code text */
  code: string;

  /** Code [langauge identifier](https://code.visualstudio.com/docs/languages/identifiers#_known-language-identifiers) */
  languageId?: string;
};

/** Represents a set of parameters */
type Params = {
  parameters: {
    /** Parameter name */
    name?: string;

    /** Parameter type */
    type?: Inline;

    /** Parameter default value */
    default?: string;

    /** Parameter description in markdown format */
    description?: markdown;

    /** Is this parameter deprecated, or the deprecation reason */
    deprecated?: boolean | string;

    /** Is this parameter optional? */
    optional?: boolean;
  }[];
};

/** Represents block level elements */
type Block =
  | Heading
  | Api
  | Markdown
  | Facts
  | Params
  | List
  | Inheritance
  | Code;

/** Represents a general API page */
type ApiPage = {
  /** Page title */
  title: string;

  /** Opaque metadata about the page as HTML <meta> tags  */
  metadata?: { [key: string]: string | string[] };

  /** Default code [language identifier](https://code.visualstudio.com/docs/languages/identifiers#_known-language-identifiers) */
  languageId?: string;

  /** Page body */
  body: Block[];
};
