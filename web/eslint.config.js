// web/eslint.config.js
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import pluginVue from 'eslint-plugin-vue';
import {
  defineConfigWithVueTs,
  vueTsConfigs,
} from '@vue/eslint-config-typescript';
import eslintConfigPrettier from 'eslint-config-prettier/flat';
import globals from 'globals';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Reuse patterns from the repo root .gitignore
const gitignorePath = path.resolve(__dirname, '../.gitignore');
const gitignorePatterns = fs.existsSync(gitignorePath)
  ? fs
      .readFileSync(gitignorePath, 'utf8')
      .split('\n')
      .map((line) => line.trim())
      .filter((line) => line && !line.startsWith('#'))
  : [];

export default defineConfigWithVueTs(
  {
    ignores: [
      '**/node_modules/**',
      '**/dist/**',
      '**/coverage/**',
      ...gitignorePatterns,
    ],
  },

  pluginVue.configs['flat/essential'],
  vueTsConfigs.recommended,

  {
    files: ['src/**/*.{ts,tsx,vue}'],
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',
      globals: {
        ...globals.browser,
      },
    },
    rules: {
      'no-debugger': 'off',
      '@typescript-eslint/no-explicit-any': 'off',
    },
  },

  eslintConfigPrettier
);
