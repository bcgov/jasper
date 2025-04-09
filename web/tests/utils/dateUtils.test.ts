import {
  extractTime,
  formatDateToDDMMMYYYY,
  hoursMinsFormatter,
} from '@/utils/dateUtils';
import { describe, expect, it } from 'vitest';

describe('dateUtils', () => {
  describe('formatDateToDDMMMYYYY', () => {
    it('formats a valid date string to "DD-MMM-YYYY"', () => {
      const result = formatDateToDDMMMYYYY('2023-01-01');
      expect(result).toBe('01-Jan-2023');
    });

    it('returns an empty string for an invalid date string', () => {
      const result = formatDateToDDMMMYYYY('');
      expect(result).toBe('');
    });

    it('returns an empty string for a null date string', () => {
      const result = formatDateToDDMMMYYYY(null as unknown as string);
      expect(result).toBe('');
    });
  });

  describe('hoursMinsFormatter', () => {
    it('formats hours and minutes correctly', () => {
      const result = hoursMinsFormatter('2', '30');
      expect(result).toBe('2 Hr(s) 30 Min(s)');
    });

    it('formats hours only when minutes are zero', () => {
      const result = hoursMinsFormatter('5', '0');
      expect(result).toBe('5 Hr(s)');
    });

    it('formats minutes only when hours are zero', () => {
      const result = hoursMinsFormatter('0', '45');
      expect(result).toBe('45 Min(s)');
    });

    it('returns "0 Mins" when both hours and minutes are zero', () => {
      const result = hoursMinsFormatter('0', '0');
      expect(result).toBe('0 Mins');
    });

    it('handles invalid inputs gracefully', () => {
      const result = hoursMinsFormatter('', '');
      expect(result).toBe('0 Mins');
    });
  });

  describe('extractTime', () => {
    it('extracts and formats time in 12-hour format with AM/PM', () => {
      const result = extractTime('2023-01-01 14:30:00');
      expect(result).toBe('2:30 PM');
    });

    it('handles midnight correctly', () => {
      const result = extractTime('2023-01-01 00:15:00');
      expect(result).toBe('12:15 AM');
    });

    it('handles noon correctly', () => {
      const result = extractTime('2023-01-01 12:00:00');
      expect(result).toBe('12:00 PM');
    });

    it('handles single-digit hours correctly', () => {
      const result = extractTime('2023-01-01 09:05:00');
      expect(result).toBe('9:05 AM');
    });

    it('throws an error for invalid date format', () => {
      expect(() => extractTime('invalid-date')).toThrowError();
    });
  });
});
