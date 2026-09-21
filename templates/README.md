This folder contains the source code for website themes used by docfx.exe.

Use Node.js 24 (the version used in CI) to work on the templates:

```sh
npm ci
npm run build
npm run lint
npm test
```

The Stylelint configuration extends `stylelint-config-recommended-scss`, which
is declared directly rather than relying on the standard SCSS preset to make it
available transitively. Upgrade Stylelint and its SCSS presets together to keep
their peer dependencies compatible.
