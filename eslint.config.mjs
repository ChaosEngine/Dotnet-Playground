import globals from "globals";
import path from "node:path";
import jsdoc from "eslint-plugin-jsdoc";
import { fileURLToPath } from "node:url";
import js from "@eslint/js";
import { FlatCompat } from "@eslint/eslintrc";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.basename(path.dirname(__filename));

const compat = new FlatCompat({
	baseDirectory: __dirname,
	recommendedConfig: js.configs.recommended,
	allConfig: js.configs.all
});

export default [
	// Ignore specific directories and files
	{
		ignores: ["**/bin", "**/obj", "**/wwwroot/lib", "**/*.min.js", "**/*Bundle.js"]
	},
	// Extend recommended configurations
	...compat.extends("eslint:recommended"), 
	jsdoc.configs['flat/recommended'],
	{
		languageOptions: {
			globals: {
				...globals.browser,
				...globals.jquery,
				...globals.worker
			},
			ecmaVersion: 2022,
			sourceType: "module"
		},
		plugins: {
			jsdoc
		},


		rules: {
			indent: ["off", "tab"],
			"linebreak-style": ["off", "unix"],
			quotes: ["off", "double"],
			semi: ["error", "always"],
			eqeqeq: "error",
			"comma-dangle": "warn",
			"no-console": "warn",
			"no-debugger": "warn",
			"no-extra-semi": "warn",
			"no-extra-parens": "off",
			"no-irregular-whitespace": "warn",
			"no-undef": "error",
			"no-unused-vars": "warn",
			"semi-spacing": "warn",

			"jsdoc/require-returns": "warn",
			"jsdoc/require-jsdoc": ["error", {
				checkConstructors: false,
				publicOnly: true,
				require: {
					'MethodDefinition': true
				}
			}]
		}
	}
];