import { CourtDocumentType } from '@/types/shared';
import * as DocumentUtils from 'CMP/documents/DocumentUtils';
import { describe, expect, it, vi } from 'vitest';

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

const mockCivilFileStore = {
  civilFileInformation: {
    detailsData: {},
  },
};

vi.mock('@/stores', () => ({
  useCriminalFileStore: () => mockCriminalFileStore,
  useCivilFileStore: () => mockCivilFileStore,
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

    it('should prepare use civilDocumentId when no civil appearanceId', () => {
      const data = {
        civilDocumentId: 'civ-1',
      };
      const result = DocumentUtils.prepareCivilDocumentData(data);
      expect(result.appearanceId).toBe('civ-1');
    });

    it('should use imageId as documentId when Reference category', () => {
      const data = {
        documentId: 'doc-id-1',
        imageId: 'img-1',
        category: 'Reference',
      };
      const result = DocumentUtils.prepareCivilDocumentData(data);
      expect(result.documentId).toBe('img-1');
    });

    it('should use imageId as documentId when LITIGANT category', () => {
      const data = {
        documentId: 'doc-id-1',
        imageId: 'img-2',
        category: 'LITIGANT',
      };
      const result = DocumentUtils.prepareCivilDocumentData(data);
      expect(result.documentId).toBe('img-2');
    });

    it('should use imageId as documentId when litigant category (lowercase)', () => {
      const data = {
        documentId: 'doc-id-1',
        imageId: 'img-3',
        category: 'litigant',
      };
      const result = DocumentUtils.prepareCivilDocumentData(data);
      expect(result.documentId).toBe('img-3');
    });
  });

  describe('getCriminalDocumentType', () => {
    it('should return ROP for category rop', () => {
      const data = { category: 'rop' } as any;
      expect(DocumentUtils.getCriminalDocumentType(data)).toBe(
        CourtDocumentType.ROP
      );
    });

    it('should return Criminal for other categories', () => {
      const data = { category: 'other' } as any;
      expect(DocumentUtils.getCriminalDocumentType(data)).toBe(
        CourtDocumentType.Criminal
      );
    });

    it('should handle undefined category', () => {
      const data = {} as any;
      expect(DocumentUtils.getCriminalDocumentType(data)).toBe(
        CourtDocumentType.Criminal
      );
    });
  });

  describe('getCivilDocumentType', () => {
    it('should return CSR for documentTypeCd CSR', () => {
      const data = { documentTypeCd: 'CSR' } as any;
      expect(DocumentUtils.getCivilDocumentType(data)).toBe(
        CourtDocumentType.CSR
      );
    });

    it('should return Civil for other documentTypeCd', () => {
      const data = { documentTypeCd: 'OTH' } as any;
      expect(DocumentUtils.getCivilDocumentType(data)).toBe(
        CourtDocumentType.Civil
      );
    });

    it('should handle undefined documentTypeCd', () => {
      const data = {} as any;
      expect(DocumentUtils.getCivilDocumentType(data)).toBe(
        CourtDocumentType.Civil
      );
    });
  });

  describe('formatDocumentCategory', () => {
    it('should return "Report" for PSR category', () => {
      const document = { category: 'PSR' } as any;
      expect(DocumentUtils.formatDocumentCategory(document)).toBe('Report');
    });

    it('should return "Report" for PSR string category', () => {
      expect(DocumentUtils.formatDocumentCategory('PSR')).toBe('Report');
    });

    it('should return "ROP" for rop category', () => {
      const document = { category: 'rop' } as any;
      expect(DocumentUtils.formatDocumentCategory(document)).toBe('ROP');
    });

    it('should return "ROP" for rop category', () => {
      expect(DocumentUtils.formatDocumentCategory('rop')).toBe('ROP');
    });

    it('should return original category for other categories', () => {
      const document = { category: 'INITIATING' } as any;
      expect(DocumentUtils.formatDocumentCategory(document)).toBe('INITIATING');
    });

    it('should return original category for other (string) categories', () => {
      expect(DocumentUtils.formatDocumentCategory('INITIATING')).toBe(
        'INITIATING'
      );
    });

    it('should handle undefined category', () => {
      const document = {} as any;
      expect(DocumentUtils.formatDocumentCategory(document)).toBeUndefined();
    });

    it('should handle null category', () => {
      const document = { category: null, docmClassification: null } as any;
      expect(DocumentUtils.formatDocumentCategory(document)).toBeNull();
    });
  });

  describe('formatDocumentType', () => {
    it('should return "Record of Proceedings" for rop category', () => {
      const document = {
        category: 'rop',
        documentTypeDescription: 'Some other description',
      } as any;
      expect(DocumentUtils.formatDocumentType(document)).toBe(
        'Record of Proceedings'
      );
    });

    it('should return documentTypeDescription for non-rop categories', () => {
      const document = {
        category: 'PSR',
        documentTypeDescription: 'Pre-sentence Report',
      } as any;
      expect(DocumentUtils.formatDocumentType(document)).toBe(
        'Pre-sentence Report'
      );
    });

    it('should return documentTypeDescription for undefined category', () => {
      const document = {
        documentTypeDescription: 'Some Document Type',
      } as any;
      expect(DocumentUtils.formatDocumentType(document)).toBe(
        'Some Document Type'
      );
    });

    it('should handle undefined documentTypeDescription', () => {
      const document = {
        category: 'OTHER',
      } as any;
      expect(DocumentUtils.formatDocumentType(document)).toBeUndefined();
    });
  });
});
