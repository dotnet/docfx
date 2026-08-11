/** @type {import('stylelint').Config} */
export default {
  extends: ["stylelint-config-standard-scss"],
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
