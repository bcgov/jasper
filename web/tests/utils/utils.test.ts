import { isPositiveInteger, parseQueryStringToString } from '@/utils/utils';
import { describe, expect, it } from 'vitest';

describe('utils', () => {
  describe('parseQueryStringToString', () => {
    it('returns the string when value is a string', () => {
      expect(parseQueryStringToString('test')).toBe('test');
    });

    it('returns the first element when value is an array', () => {
      expect(parseQueryStringToString(['first', 'second'])).toBe('first');
    });

    it('returns fallback when value is an empty array', () => {
      expect(parseQueryStringToString([], 'default')).toBe('default');
    });

    it('returns fallback when value is null', () => {
      expect(parseQueryStringToString(null, 'fallback')).toBe('fallback');
    });

    it('returns fallback when value is undefined', () => {
      expect(parseQueryStringToString(undefined, 'fallback')).toBe('fallback');
    });

    it('returns empty string fallback if not provided', () => {
      expect(parseQueryStringToString(undefined)).toBe('');
    });

    it('returns empty string if value is null and no fallback is provided', () => {
      expect(parseQueryStringToString(null)).toBe('');
    });

    describe('isPositiveInteger', () => {
      it('returns true for positive integers', () => {
        expect(isPositiveInteger(1)).toBe(true);
        expect(isPositiveInteger(100)).toBe(true);
      });

      it('returns false for zero', () => {
        expect(isPositiveInteger(0)).toBe(false);
      });

      it('returns false for negative numbers', () => {
        expect(isPositiveInteger(-1)).toBe(false);
        expect(isPositiveInteger(-100)).toBe(false);
      });

      it('returns false for non-number types', () => {
        expect(isPositiveInteger('5')).toBe(false);
        expect(isPositiveInteger(null)).toBe(false);
        expect(isPositiveInteger(undefined)).toBe(false);
        expect(isPositiveInteger({})).toBe(false);
        expect(isPositiveInteger([])).toBe(false);
      });

      it('returns false for NaN', () => {
        expect(isPositiveInteger(NaN)).toBe(false);
      });
    });
  });
});
