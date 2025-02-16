import globals from "globals";
import path from "node:path";
import jsdoc from "eslint-plugin-jsdoc";
import { fileURLToPath } from "node:url";
import js from "@eslint/js";
import { FlatCompat } from "@eslint/eslintrc";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.basename(path.dirname(__filename));
// console.log("ch__dirname = ", __dirname);

const compat = new FlatCompat({
    baseDirectory: __dirname,
    recommendedConfig: js.configs.recommended,
    allConfig: js.configs.all
});


export default [...compat.extends("eslint:recommended"), jsdoc.configs['flat/recommended'],
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
        eqeqeq: 2,
        "comma-dangle": 1,
        "no-console": 1,
        "no-debugger": 1,
        "no-extra-semi": 1,
        "no-extra-parens": 0,
        "no-irregular-whitespace": 1,
        "no-undef": 2,
        "no-unused-vars": 1,
        "semi-spacing": 1,

        "jsdoc/require-returns": "warn",
        "jsdoc/require-jsdoc": ["error", {
            checkConstructors: false,
            publicOnly: true,
            require: {
                'MethodDefinition': true
            }
        }]
    }
}];