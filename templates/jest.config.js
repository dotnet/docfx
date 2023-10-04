/** @type {import('ts-jest/dist/types').InitialOptionsTsJest} */
module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  transform: {
    '^.+\\.[tj]s$': ['ts-jest', {
      tsconfig: {
        allowJs: true
      }
    }]
  },
  transformIgnorePatterns: [
    '<rootDir>/node_modules/(?!lit-html)'
  ]
}
