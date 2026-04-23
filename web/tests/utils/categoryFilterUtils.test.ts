import {
  DEFAULT_SECTION_TITLE,
  MULTI_SECTION_TITLE,
  getSectionTitle,
  getUncategorizedCount,
  matchesCategorySelection,
  pruneInvalidSelections,
} from '@/utils/categoryFilterUtils';
import { describe, expect, it } from 'vitest';

describe('categoryFilterUtils', () => {
  describe('pruneInvalidSelections', () => {
    it('keeps only selections present in valid values', () => {
      const result = pruneInvalidSelections(
        ['Scheduled', 'Other', 'Missing'],
        ['Scheduled', 'CSR', 'Other']
      );

      expect(result).toEqual(['Scheduled', 'Other']);
    });

    it('returns empty when no selections are valid', () => {
      const result = pruneInvalidSelections(['A', 'B'], ['X', 'Y']);
      expect(result).toEqual([]);
    });
  });

  describe('getSectionTitle', () => {
    it('returns default title when there are no active values', () => {
      expect(getSectionTitle([])).toBe(DEFAULT_SECTION_TITLE);
    });

    it('returns mapped display title for a single selection when mapper is provided', () => {
      const mapDisplayTitle = (value: string) =>
        value === 'CSR' ? 'Court Summary' : value;

      expect(getSectionTitle(['CSR'], mapDisplayTitle)).toBe('Court Summary');
    });

    it('returns single raw value when mapper is not provided', () => {
      expect(getSectionTitle(['Transcript'])).toBe('Transcript');
    });

    it('returns multi-selection title when multiple values are active', () => {
      expect(getSectionTitle(['CSR', 'Transcript'])).toBe(MULTI_SECTION_TITLE);
    });
  });

  describe('getUncategorizedCount', () => {
    it('counts null, undefined, empty and whitespace-only categories as uncategorized', () => {
      const items = [
        { category: null },
        { category: undefined },
        { category: '' },
        { category: '   ' },
        { category: 'CSR' },
      ];

      const count = getUncategorizedCount(items, (item) => item.category);
      expect(count).toBe(4);
    });
  });

  describe('matchesCategorySelection', () => {
    const getCategory = (item: { category?: string | null }) => item.category;

    it('returns true when no active filters are selected', () => {
      expect(
        matchesCategorySelection({ category: 'CSR' }, [], getCategory)
      ).toBe(true);
    });

    it('matches selected category case-insensitively and ignores whitespace', () => {
      expect(
        matchesCategorySelection({ category: '  csr  ' }, ['CSR'], getCategory)
      ).toBe(true);
    });

    it('matches Other against uncategorized items', () => {
      expect(
        matchesCategorySelection({ category: '   ' }, ['Other'], getCategory)
      ).toBe(true);
    });

    it('does not match Other when item has a real category', () => {
      expect(
        matchesCategorySelection({ category: 'CSR' }, ['Other'], getCategory)
      ).toBe(false);
    });

    it('supports custom other label', () => {
      expect(
        matchesCategorySelection({ category: '' }, ['Misc'], getCategory, {
          otherLabel: 'Misc',
        })
      ).toBe(true);
    });

    it('uses special predicates when provided', () => {
      const item = { category: 'CSR', nextAppearanceDt: '2025-01-01' };
      const result = matchesCategorySelection(
        item,
        ['Scheduled'],
        (doc) => doc.category,
        {
          specialPredicates: {
            scheduled: (doc) => !!doc.nextAppearanceDt,
          },
        }
      );

      expect(result).toBe(true);
    });

    it('falls back to category matching when special predicate is not available', () => {
      const item = { category: 'Transcript', nextAppearanceDt: '' };
      const result = matchesCategorySelection(
        item,
        ['Transcript', 'Scheduled'],
        (doc) => doc.category,
        {
          specialPredicates: {
            scheduled: (doc) => !!doc.nextAppearanceDt,
          },
        }
      );

      expect(result).toBe(true);
    });
  });
});
