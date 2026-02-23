import { isPositiveInteger, parseQueryStringToString, isCourtClassLabelCriminal, arrayBufferToBase64 } from '@/utils/utils';
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
    
    describe('isCourtClassLabelCriminal', () => {
      it('returns true for "Criminal - Adult"', () => {
        expect(isCourtClassLabelCriminal('Criminal - Adult')).toBe(true);
      });

      it('returns true for "Youth"', () => {
        expect(isCourtClassLabelCriminal('Youth')).toBe(true);
      });

      it('returns true for "Tickets"', () => {
        expect(isCourtClassLabelCriminal('Tickets')).toBe(true);
      });

      it('returns false for "Small Claims"', () => {
        expect(isCourtClassLabelCriminal('Small Claims')).toBe(false);
      });

      it('returns false for "Family"', () => {
        expect(isCourtClassLabelCriminal('Family')).toBe(false);
      });

      it('returns false for "Unknown"', () => {
        expect(isCourtClassLabelCriminal('Unknown')).toBe(false);
      });

      it('returns false for empty string', () => {
        expect(isCourtClassLabelCriminal('')).toBe(false);
      });

      it('returns false for unrelated label', () => {
        expect(isCourtClassLabelCriminal('Civil')).toBe(false);
      });
    });

    describe('arrayBufferToBase64', () => {
      it('converts empty buffer to empty base64 string', () => {
        const buffer = new ArrayBuffer(0);
        const result = arrayBufferToBase64(buffer);
        expect(result).toBe('');
      });

      it('converts simple ASCII text buffer to base64', () => {
        const text = 'Hello';
        const buffer = new TextEncoder().encode(text).buffer;
        const result = arrayBufferToBase64(buffer);
        expect(result).toBe('SGVsbG8=');
      });

      it('converts buffer with various byte values to base64', () => {
        const bytes = new Uint8Array([0, 1, 2, 127, 128, 255]);
        const buffer = bytes.buffer;
        const result = arrayBufferToBase64(buffer);
        expect(result).toBeTruthy();
        expect(typeof result).toBe('string');
        // Verify it's valid base64 (only contains valid base64 characters)
        expect(/^[A-Za-z0-9+/]*={0,2}$/.test(result)).toBe(true);
      });

      it('handles buffer with binary data', () => {
        const bytes = new Uint8Array([0xFF, 0xFE, 0xFD, 0xFC]);
        const buffer = bytes.buffer;
        const result = arrayBufferToBase64(buffer);
        expect(result).toBeTruthy();
        expect(typeof result).toBe('string');
        expect(result.length).toBeGreaterThan(0);
      });
    });
  });
});
