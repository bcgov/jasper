import vue from 'eslint-plugin-vue';
import typescriptEslint from '@typescript-eslint/eslint-plugin';
import typescriptParser from '@typescript-eslint/parser';
import * as vueParser from 'vue-eslint-parser';
import prettier from 'eslint-config-prettier';
import { readFileSync } from 'fs';
import { fileURLToPath } from 'url';
import { dirname, resolve } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

// Read .gitignore patterns
const gitignorePath = resolve(__dirname, '../.gitignore');
const gitignoreContent = readFileSync(gitignorePath, 'utf-8');
const ignorePatterns = gitignoreContent
  .split('\n')
  .filter((line) => line.trim() && !line.startsWith('#'))
  .map((line) => line.trim());

export default [
  {
    ignores: [
      '**/node_modules/**',
      '**/dist/**',
      '**/build/**',
      '**/*.min.js',
      ...ignorePatterns,
    ],
  },
  {
    files: ['**/*.ts', '**/*.vue'],
    languageOptions: {
      parser: vueParser,
      parserOptions: {
        parser: typescriptParser,
        ecmaVersion: 2020,
        sourceType: 'module',
      },
      globals: {
        node: true,
        es2022: true,
      },
    },
    plugins: {
      vue,
      '@typescript-eslint': typescriptEslint,
    },
    rules: {
      'no-debugger': 'off',
      '@typescript-eslint/no-explicit-any': 'off',
      ...vue.configs.essential.rules,
      ...typescriptEslint.configs.recommended.rules,
      ...prettier.rules,
    },
  },
];
