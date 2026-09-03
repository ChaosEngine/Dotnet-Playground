import js from "@eslint/js";
import jsdoc from "eslint-plugin-jsdoc";
import globals from "globals";

export default [
	// Ignore specific directories and files
	{
		ignores: [
			"**/bin",
			"**/obj",
			"**/wwwroot/lib",
			"**/*.min.js",
			"**/*Bundle.js",
			"**/dist",
			"**/coverage"
		]
	},
	// Extend recommended configurations
	js.configs.recommended,
	jsdoc.configs["flat/recommended"],
	{
		files: ["**/*.{js,mjs,cjs}"],
		languageOptions: {
			ecmaVersion: "latest",
			sourceType: "module",
			parserOptions: {
				ecmaFeatures: {
					jsx: false
				}
			},
			globals: {
				...globals.browser,
				...globals.jquery,
				...globals.worker
			}
		},
		plugins: {
			jsdoc
		},
		linterOptions: {
			reportUnusedDisableDirectives: true
		},
		rules: {
			indent: ["off", "tab"],
			"linebreak-style": "off",
			quotes: "off",
			semi: ["error", "always"],
			eqeqeq: ["error", "always"],
			"comma-dangle": ["warn", "never"],
			"no-console": "warn",
			"no-debugger": "warn",
			"no-extra-semi": "warn",
			"no-extra-parens": "off",
			"no-irregular-whitespace": "warn",
			"no-undef": "error",
			"no-unused-vars": ["warn", {
				argsIgnorePattern: "^_",
				varsIgnorePattern: "^_",
				ignoreRestSiblings: true
			}],
			"semi-spacing": "warn",
			
			"jsdoc/require-returns": "warn",
			"jsdoc/require-jsdoc": ["warn", {
				checkConstructors: false,
				publicOnly: true,
				require: {
					MethodDefinition: true
				}
			}]
		}
	}
];