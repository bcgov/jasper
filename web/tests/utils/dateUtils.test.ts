import {
  extractTime,
  formatDateInstanceToDDMMMYYYY,
  formatDateInstanceToMMMYYYY,
  formatDateToDDMMMYYYY,
  hoursMinsFormatter,
  parseDDMMMYYYYToDate,
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

  describe('formatDateInstanceToDDMMMYYYY', () => {
    it('formats a valid Date correctly', () => {
      const date = new Date(2025, 5, 17);
      expect(formatDateInstanceToDDMMMYYYY(date)).toBe('17-Jun-2025');
    });

    it('formats a date with single-digit day/month', () => {
      const date = new Date(2025, 0, 5);
      expect(formatDateInstanceToDDMMMYYYY(date)).toBe('05-Jan-2025');
    });

    it('returns empty string for invalid Date', () => {
      const date = new Date('invalid');
      expect(formatDateInstanceToDDMMMYYYY(date)).toBe('');
    });
  });

  describe('formatDateInstanceToMMMYYYY', () => {
    it('formats a valid date to "MMM YYYY"', () => {
      const date = new Date('2025-06-15');
      const result = formatDateInstanceToMMMYYYY(date);
      expect(result).toBe('Jun 2025');
    });

    it('returns empty string for invalid Date object', () => {
      const date = new Date('invalid-date-string');
      const result = formatDateInstanceToMMMYYYY(date);
      expect(result).toBe('');
    });

    it('returns empty string if passed null', () => {
      const result = formatDateInstanceToMMMYYYY(null as unknown as Date);
      expect(result).toBe('');
    });

    it('returns empty string if passed undefined', () => {
      const result = formatDateInstanceToMMMYYYY(undefined as unknown as Date);
      expect(result).toBe('');
    });

    it('formats edge case: January 1, 2000', () => {
      const result = formatDateInstanceToMMMYYYY(new Date(2000, 0, 1));
      expect(result).toBe('Jan 2000');
    });

    it('formats leap year date correctly', () => {
      const result = formatDateInstanceToMMMYYYY(new Date(2024, 1, 29));
      expect(result).toBe('Feb 2024');
    });
  });

  describe('parseDDMMMYYYYToDate', () => {
    it('parses a valid date string correctly', () => {
      const result = parseDDMMMYYYYToDate('07-Jul-2025');
      expect(result).toEqual(new Date(2025, 6, 7));
    });

    it('trims and parses a valid date with extra spaces', () => {
      const result = parseDDMMMYYYYToDate(' 01-Jan-2000 ');
      expect(result).toEqual(new Date(2000, 0, 1));
    });

    it('returns null for invalid month abbreviation', () => {
      const result = parseDDMMMYYYYToDate('15-XYZ-2021');
      expect(result).toBeNull();
    });

    it('returns null if dateStr is empty', () => {
      const result = parseDDMMMYYYYToDate('');
      expect(result).toBeNull();
    });

    it('returns null if dateStr is not a string', () => {
      // @ts-expect-error - intentionally passing wrong type
      expect(parseDDMMMYYYYToDate(12345)).toBeNull();
    });

    it('returns null for wrong format (missing parts)', () => {
      expect(parseDDMMMYYYYToDate('07-Jul')).toBeNull();
      expect(parseDDMMMYYYYToDate('2025')).toBeNull();
    });

    it('returns null for invalid numbers', () => {
      expect(parseDDMMMYYYYToDate('xx-Jul-2025')).toBeNull();
      expect(parseDDMMMYYYYToDate('07-Jul-xx')).toBeNull();
    });

    it.only('returns null for NaN Date (e.g. 32nd of a month)', () => {
      expect(parseDDMMMYYYYToDate('32-Jan-2022')).toBeNull();
    });

    it('returns null for completely invalid format', () => {
      expect(parseDDMMMYYYYToDate('not-a-date')).toBeNull();
    });
  });
});
