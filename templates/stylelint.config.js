/** @type {import('stylelint').Config} */
export default {
  extends: ["stylelint-config-recommended-scss"],
  ignoreFiles: [
    "**/*.ts",
    "**/*.js"
  ],
  rules: {
    "font-family-no-missing-generic-family-keyword": [ true, {
      "ignoreFontFamilies": [
        "bootstrap-icons"
      ]
    }],
  }
};
