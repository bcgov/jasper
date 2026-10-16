import { normalizeExtension } from '@/utils/transitoryDocumentFormats';
import { describe, expect, it } from 'vitest';

describe('transitory document extension normalization', () => {
  it.each([
    [' pdf ', '.pdf'],
    ['.PDF', '.pdf'],
    ['docx', '.docx'],
    ['..pdf', null],
    ['.doc/x', null],
    ['', null],
  ])('normalizes %s consistently', (input, expected) => {
    expect(normalizeExtension(input)).toBe(expected);
  });
});
