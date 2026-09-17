import {
  DEFAULT_SECTION_TITLE,
  MULTI_SECTION_TITLE,
  SCHEDULED_CATEGORY_FILTER,
  UNCATEGORIZED_CATEGORY_FILTER,
  getSectionTitle,
  getUncategorizedCount,
  isAllOptionsSelected,
  matchesCategorySelection,
  normalizeCategory,
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

    it('canonicalizes and deduplicates valid selections', () => {
      expect(pruneInvalidSelections([' CSR ', 'csr'], ['csr'])).toEqual([
        'csr',
      ]);
    });
  });

  describe('isAllOptionsSelected', () => {
    it('requires every current option to be selected', () => {
      expect(isAllOptionsSelected(['stale', 'duplicate'], ['a', 'b'])).toBe(
        false
      );
      expect(isAllOptionsSelected(['b', 'a', 'stale'], ['a', 'b'])).toBe(true);
    });
  });

  it('normalizes category identity once', () => {
    expect(normalizeCategory(' CSR ')).toBe('csr');
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

    it('matches the uncategorized sentinel against uncategorized items', () => {
      expect(
        matchesCategorySelection(
          { category: '   ' },
          [UNCATEGORIZED_CATEGORY_FILTER],
          getCategory
        )
      ).toBe(true);
    });

    it('keeps a genuine Other category distinct from uncategorized', () => {
      expect(
        matchesCategorySelection({ category: 'Other' }, ['other'], getCategory)
      ).toBe(true);
      expect(
        matchesCategorySelection(
          { category: 'Other' },
          [UNCATEGORIZED_CATEGORY_FILTER],
          getCategory
        )
      ).toBe(false);
    });

    it('uses special predicates when provided', () => {
      const item = { category: 'CSR', nextAppearanceDt: '2025-01-01' };
      const result = matchesCategorySelection(
        item,
        [SCHEDULED_CATEGORY_FILTER],
        (doc) => doc.category,
        {
          specialPredicates: {
            [SCHEDULED_CATEGORY_FILTER]: (doc) => !!doc.nextAppearanceDt,
          },
        }
      );

      expect(result).toBe(true);
    });

    it('falls back to category matching when special predicate is not available', () => {
      const item = { category: 'Transcript', nextAppearanceDt: '' };
      const result = matchesCategorySelection(
        item,
        ['transcript', SCHEDULED_CATEGORY_FILTER],
        (doc) => doc.category,
        {
          specialPredicates: {
            [SCHEDULED_CATEGORY_FILTER]: (doc) => !!doc.nextAppearanceDt,
          },
        }
      );

      expect(result).toBe(true);
    });
  });
});
