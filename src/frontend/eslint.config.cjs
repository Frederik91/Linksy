/*
 Minimal ESLint flat config to migrate from .eslintrc.json (ESLint v9+)
 This config keeps parser/settings for TypeScript + React and registers the
 plugins used previously: @typescript-eslint, react-hooks and react-refresh.

 It's intentionally small so linting starts cleanly; we can expand it to
 include full recommended rule sets later.
*/
const tsPlugin = require("@typescript-eslint/eslint-plugin");
const reactHooks = require("eslint-plugin-react-hooks");
const reactRefresh = require("eslint-plugin-react-refresh");

module.exports = [
  // ignore patterns (kept in sync with previous .eslintrc.json)
  {
    ignores: ["dist", ".eslintrc.cjs"],
  },

  // apply to TypeScript files (with `project` for rules requiring type information)
  {
    files: ["src/**/*.{ts,tsx}"],
    languageOptions: {
      parser: require("@typescript-eslint/parser"),
      parserOptions: {
        ecmaVersion: "latest",
        sourceType: "module",
        ecmaFeatures: { jsx: true },
        // project is required for some @typescript-eslint rules that need type info
        project: "./tsconfig.json",
      },
    },
    plugins: {
      "@typescript-eslint": tsPlugin,
      "react-hooks": reactHooks,
      "react-refresh": reactRefresh,
    },
    rules: {
      // Keep the rule previously defined in .eslintrc.json
      "react-refresh/only-export-components": [
        "warn",
        { allowConstantExport: true }
      ],

      // Minimal TypeScript rules; expand as needed
      "@typescript-eslint/no-unused-vars": ["warn", { argsIgnorePattern: "^_" }],
      "@typescript-eslint/explicit-function-return-type": "off",
    },
  },

  // apply to JS/JSX files (do not set `project` to avoid parser project errors)
  {
    files: ["**/*.{js,jsx,ts,tsx}"],
    languageOptions: {
      parser: require("@typescript-eslint/parser"),
      parserOptions: {
        ecmaVersion: "latest",
        sourceType: "module",
        ecmaFeatures: { jsx: true },
      },
    },
    plugins: {
      "react-hooks": reactHooks,
      "react-refresh": reactRefresh,
    },
    rules: {
      "react-refresh/only-export-components": ["warn", { allowConstantExport: true }],
    },
  },
];
