import { useCommonStore } from '@/stores';
import {
  arrayBufferToBase64,
  hasRole,
  isCourtClassLabelCriminal,
  isPositiveInteger,
  parseQueryStringToString,
} from '@/utils/utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/stores');

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
        const bytes = new Uint8Array([0xff, 0xfe, 0xfd, 0xfc]);
        const buffer = bytes.buffer;
        const result = arrayBufferToBase64(buffer);
        expect(result).toBeTruthy();
        expect(typeof result).toBe('string');
        expect(result.length).toBeGreaterThan(0);
      });
    });
  });

  describe('hasRole', () => {
    beforeEach(() => {
      vi.clearAllMocks();
    });

    it('returns true when userInfo contains the role', () => {
      (useCommonStore as any).mockReturnValue({
        userInfo: { roles: ['judge', 'admin'] },
      });
      expect(hasRole('judge')).toBe(true);
    });

    it('returns false when userInfo does not contain the role', () => {
      (useCommonStore as any).mockReturnValue({
        userInfo: { roles: ['admin'] },
      });
      expect(hasRole('judge')).toBe(false);
    });

    it('returns true when any role in an array matches', () => {
      (useCommonStore as any).mockReturnValue({
        userInfo: { roles: ['judge'] },
      });
      expect(hasRole(['admin', 'judge'])).toBe(true);
    });

    it('returns false when no role in an array matches', () => {
      (useCommonStore as any).mockReturnValue({
        userInfo: { roles: ['clerk'] },
      });
      expect(hasRole(['admin', 'judge'])).toBe(false);
    });

    it('returns false when roles array is empty', () => {
      (useCommonStore as any).mockReturnValue({ userInfo: { roles: [] } });
      expect(hasRole('judge')).toBe(false);
    });

    it('returns false when userInfo is null', () => {
      (useCommonStore as any).mockReturnValue({ userInfo: null });
      expect(hasRole('judge')).toBe(false);
    });

    it('returns false when userInfo has no roles property', () => {
      (useCommonStore as any).mockReturnValue({ userInfo: {} });
      expect(hasRole('judge')).toBe(false);
    });

    it('returns true when passing an array with a single matching role', () => {
      (useCommonStore as any).mockReturnValue({
        userInfo: { roles: ['judge'] },
      });
      expect(hasRole(['judge'])).toBe(true);
    });
  });
});
