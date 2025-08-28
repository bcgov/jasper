import { describe, it, expect, vi } from 'vitest';
import * as DocumentUtils from 'CMP/documents/DocumentUtils';
import { CourtDocumentType } from '@/types/shared';

vi.mock('@/filters', () => ({
  beautifyDate: vi.fn((date) => `beautified-${date}`),
}));

const mockCriminalFileStore = {
  criminalFileInformation: {
    detailsData: {
      courtClassCd: 'CR',
      courtLevelCd: 'P',
      fileNumberTxt: '12345',
      homeLocationAgencyName: 'Vancouver',
    },
    fileNumber: 'FN-001',
  },
};

vi.mock('@/stores', () => ({
  useCriminalFileStore: () => mockCriminalFileStore,
}));

describe('DocumentUtils', () => {
  describe('prepareDocumentData', () => {
    it('should prepare criminal document data for ROP category', () => {
      const data = {
        date: '2024-01-01',
        imageId: 'img-1',
        category: 'rop',
        documentTypeDescription: 'Some Type',
        partId: 10,
        profSeqNo: 20,
      };
      const result = DocumentUtils.prepareCriminalDocumentData(data);
      expect(result).toEqual({
        courtClass: 'CR',
        courtLevel: 'P',
        dateFiled: 'beautified-2024-01-01',
        documentId: 'img-1',
        documentDescription: 'Record of Proceedings',
        fileId: 'FN-001',
        fileNumberText: '12345',
        isCriminal: true,
        partId: 10,
        profSeqNo: 20,
        location: 'Vancouver',
      });
    });

    it('should prepare criminal document data for non-ROP category', () => {
      const data = {
        date: '2024-02-02',
        imageId: 'img-2',
        category: 'other',
        documentTypeDescription: 'Other Type',
        partId: 11,
        profSeqNo: 21,
      };
      const result = DocumentUtils.prepareCriminalDocumentData(data);
      expect(result.documentDescription).toBe('Other Type');
    });
  });

  describe('getCriminalDocumentType', () => {
    it('should return ROP for category rop', () => {
      const data = { category: 'rop' } as any;
      expect(DocumentUtils.getCriminalDocumentType(data)).toBe(CourtDocumentType.ROP);
    });

    it('should return Criminal for other categories', () => {
      const data = { category: 'other' } as any;
      expect(DocumentUtils.getCriminalDocumentType(data)).toBe(CourtDocumentType.Criminal);
    });

    it('should handle undefined category', () => {
      const data = {} as any;
      expect(DocumentUtils.getCriminalDocumentType(data)).toBe(CourtDocumentType.Criminal);
    });
  });

  describe('getCivilDocumentType', () => {
    it('should return CSR for documentTypeCd CSR', () => {
      const data = { documentTypeCd: 'CSR' } as any;
      expect(DocumentUtils.getCivilDocumentType(data)).toBe(CourtDocumentType.CSR);
    });

    it('should return Civil for other documentTypeCd', () => {
      const data = { documentTypeCd: 'OTH' } as any;
      expect(DocumentUtils.getCivilDocumentType(data)).toBe(CourtDocumentType.Civil);
    });

    it('should handle undefined documentTypeCd', () => {
      const data = {} as any;
      expect(DocumentUtils.getCivilDocumentType(data)).toBe(CourtDocumentType.Civil);
    });
  });
});