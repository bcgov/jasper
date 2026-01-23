import shared from '@/components/shared';
import { civilDocumentType } from '@/types/civil/jsonTypes';
import { CourtDocumentType } from '@/types/shared';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/components/documents/DocumentUtils', () => ({
  prepareCivilDocumentData: vi.fn(),
  getCivilDocumentType: vi.fn(),
}));

describe('shared.openCivilDocument', () => {
  const mockDocument: any = {
    appearanceId: '1',
    fileSeqNo: 1,
    documentTypeDescription: 'Affidavit',
    imageId: 'img1',
    documentSupport: [{ actCd: 'ACT1' }],
    filedDt: '2024-06-01',
    filedByName: 'John Doe',
    runtime: 'Completed',
    issue: [{ issueDsc: 'Issue 1' }],
    category: 'civil',
    DateGranted: '2024-06-01',
    civilDocumentId: 'doc1',
    partId: 'part1',
  };

  const mockLocations: any = [
    { agencyIdentifierCd: 'AG1', name: 'Courtroom 1' },
    { agencyIdentifierCd: 'AG2', name: 'Courtroom 2' },
  ];

  let prepareCivilDocumentDataMock: any;
  let getCivilDocumentTypeMock: any;
  let openDocumentsPdfSpy: any;

  beforeEach(async () => {
    vi.clearAllMocks();

    const documentUtils = await import('@/components/documents/DocumentUtils');
    prepareCivilDocumentDataMock = vi.mocked(
      documentUtils.prepareCivilDocumentData
    );
    getCivilDocumentTypeMock = vi.mocked(documentUtils.getCivilDocumentType);

    openDocumentsPdfSpy = vi
      .spyOn(shared, 'openDocumentsPdf')
      .mockImplementation(() => {});

    // Default mock return values
    prepareCivilDocumentDataMock.mockReturnValue({
      fileId: undefined,
      fileNumberText: undefined,
      courtLevel: undefined,
      location: undefined,
    });

    getCivilDocumentTypeMock.mockReturnValue(CourtDocumentType.Civil);
  });

  it('should call prepareCivilDocumentData with the document', () => {
    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG1',
      mockLocations
    );

    expect(prepareCivilDocumentDataMock).toHaveBeenCalledWith(mockDocument);
  });

  it('should always set fileId from parameter', () => {
    prepareCivilDocumentDataMock.mockReturnValue({
      fileId: 'existing-file-id',
      fileNumberText: undefined,
      courtLevel: undefined,
      location: undefined,
    });

    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG1',
      mockLocations
    );

    expect(openDocumentsPdfSpy).toHaveBeenCalledWith(
      CourtDocumentType.Civil,
      expect.objectContaining({
        fileId: 'file123',
      })
    );
  });

  it('should set fileNumberText from parameter when not already set', () => {
    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG1',
      mockLocations
    );

    expect(openDocumentsPdfSpy).toHaveBeenCalledWith(
      CourtDocumentType.Civil,
      expect.objectContaining({
        fileNumberText: 'FN123',
      })
    );
  });

  it('should not overwrite fileNumberText if already set', () => {
    prepareCivilDocumentDataMock.mockReturnValue({
      fileId: undefined,
      fileNumberText: 'existing-file-number',
      courtLevel: undefined,
      location: undefined,
    });

    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG1',
      mockLocations
    );

    expect(openDocumentsPdfSpy).toHaveBeenCalledWith(
      CourtDocumentType.Civil,
      expect.objectContaining({
        fileNumberText: 'existing-file-number',
      })
    );
  });

  it('should set courtLevel from parameter when not already set', () => {
    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG1',
      mockLocations
    );

    expect(openDocumentsPdfSpy).toHaveBeenCalledWith(
      CourtDocumentType.Civil,
      expect.objectContaining({
        courtLevel: 'Provincial',
      })
    );
  });

  it('should not overwrite courtLevel if already set', () => {
    prepareCivilDocumentDataMock.mockReturnValue({
      fileId: undefined,
      fileNumberText: undefined,
      courtLevel: 'Supreme',
      location: undefined,
    });

    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG1',
      mockLocations
    );

    expect(openDocumentsPdfSpy).toHaveBeenCalledWith(
      CourtDocumentType.Civil,
      expect.objectContaining({
        courtLevel: 'Supreme',
      })
    );
  });

  it('should set location by finding matching agencyId when not already set', () => {
    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG1',
      mockLocations
    );

    expect(openDocumentsPdfSpy).toHaveBeenCalledWith(
      CourtDocumentType.Civil,
      expect.objectContaining({
        location: 'Courtroom 1',
      })
    );
  });

  it('should set location from second location when agencyId matches', () => {
    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG2',
      mockLocations
    );

    expect(openDocumentsPdfSpy).toHaveBeenCalledWith(
      CourtDocumentType.Civil,
      expect.objectContaining({
        location: 'Courtroom 2',
      })
    );
  });

  it('should not set location if agencyId is not found', () => {
    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG999',
      mockLocations
    );

    expect(openDocumentsPdfSpy).toHaveBeenCalledWith(
      CourtDocumentType.Civil,
      expect.objectContaining({
        location: undefined,
      })
    );
  });

  it('should not overwrite location if already set', () => {
    prepareCivilDocumentDataMock.mockReturnValue({
      fileId: undefined,
      fileNumberText: undefined,
      courtLevel: undefined,
      location: 'Existing Location',
    });

    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG1',
      mockLocations
    );

    expect(openDocumentsPdfSpy).toHaveBeenCalledWith(
      CourtDocumentType.Civil,
      expect.objectContaining({
        location: 'Existing Location',
      })
    );
  });

  it('should call getCivilDocumentType with the document', () => {
    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG1',
      mockLocations
    );

    expect(getCivilDocumentTypeMock).toHaveBeenCalledWith(mockDocument);
  });

  it('should call openDocumentsPdf with documentType and enhanced documentData', () => {
    getCivilDocumentTypeMock.mockReturnValue(CourtDocumentType.CSR);

    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG1',
      mockLocations
    );

    expect(openDocumentsPdfSpy).toHaveBeenCalledWith(
      CourtDocumentType.CSR,
      expect.objectContaining({
        fileId: 'file123',
        fileNumberText: 'FN123',
        courtLevel: 'Provincial',
        location: 'Courtroom 1',
      })
    );
  });

  it('should handle all parameters being provided together', () => {
    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG2',
      mockLocations
    );

    expect(openDocumentsPdfSpy).toHaveBeenCalledTimes(1);
    expect(openDocumentsPdfSpy).toHaveBeenCalledWith(CourtDocumentType.Civil, {
      fileId: 'file123',
      fileNumberText: 'FN123',
      courtLevel: 'Provincial',
      location: 'Courtroom 2',
    });
  });

  it('should handle empty locations array', () => {
    shared.openCivilDocument(
      mockDocument,
      'file123',
      'FN123',
      'Provincial',
      'AG1',
      []
    );

    expect(openDocumentsPdfSpy).toHaveBeenCalledWith(
      CourtDocumentType.Civil,
      expect.objectContaining({
        location: undefined,
      })
    );
  });
});

describe('shared.getBaseCivilDocumentTableHeaders', () => {
  describe('when isScheduledCategory is false (default)', () => {
    it('should return 7 headers', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders();
      expect(headers).toHaveLength(7);
    });

    it('should have correct SEQ header', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders();
      expect(headers[0]).toEqual({
        title: 'SEQ',
        key: 'fileSeqNo',
        width: '4rem',
        maxWidth: '4rem',
      });
    });

    it('should have correct DOCUMENT TYPE header', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders();
      expect(headers[1]).toEqual({
        title: 'DOCUMENT TYPE',
        key: 'documentTypeDescription',
      });
    });

    it('should have correct ACT header', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders();
      expect(headers[2]).toEqual({
        title: 'ACT',
        key: 'activity',
      });
    });

    it('should have DATE FILED header as the 4th header', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders(false);
      const dateHeader = headers[3];
      expect(dateHeader.title).toBe('DATE FILED');
      expect(dateHeader.key).toBe('filedDt');
      expect(dateHeader.width).toBe('8.5rem');
      expect(dateHeader.maxWidth).toBe('8.5rem');
    });

    it('should have value formatter for DATE FILED header', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders(false);
      const dateHeader = headers[3];
      expect(dateHeader.value).toBeDefined();
      expect(typeof dateHeader.value).toBe('function');
    });

    it('should have sortRaw function for DATE FILED header', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders(false);
      const dateHeader = headers[3];
      expect(dateHeader.sortRaw).toBeDefined();
      expect(typeof dateHeader.sortRaw).toBe('function');
    });

    it('should sort DATE FILED by date correctly', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders(false);
      const dateHeader = headers[3];
      
      const item1 = { filedDt: '2024-01-15' } as civilDocumentType;
      const item2 = { filedDt: '2024-06-20' } as civilDocumentType;
      
      const result = dateHeader.sortRaw!(item1, item2);
      expect(result).toBeLessThan(0); // item1 is before item2
      
      const reverseResult = dateHeader.sortRaw!(item2, item1);
      expect(reverseResult).toBeGreaterThan(0); // item2 is after item1
    });

    it('should have correct ORDER MADE header', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders();
      const orderMadeHeader = headers[4];
      expect(orderMadeHeader.title).toBe('ORDER MADE');
      expect(orderMadeHeader.key).toBe('orderMadeDt');
      expect(orderMadeHeader.width).toBe('9.5rem');
      expect(orderMadeHeader.maxWidth).toBe('9.5rem');
      expect(orderMadeHeader.value).toBeDefined();
      expect(orderMadeHeader.sortRaw).toBeDefined();
    });

    it('should sort ORDER MADE by date correctly', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders();
      const orderMadeHeader = headers[4];
      
      const item1 = { orderMadeDt: '2024-03-10' } as civilDocumentType;
      const item2 = { orderMadeDt: '2024-08-25' } as civilDocumentType;
      
      const result = orderMadeHeader.sortRaw!(item1, item2);
      expect(result).toBeLessThan(0);
    });

    it('should have correct FILED / SWORN BY header', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders();
      expect(headers[5]).toEqual({
        title: 'FILED / SWORN BY',
        key: 'filedBy',
      });
    });

    it('should have correct ISSUES header', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders();
      expect(headers[6]).toEqual({
        title: 'ISSUES',
        key: 'issue',
      });
    });
  });

  describe('when isScheduledCategory is true', () => {
    it('should return 7 headers', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders(true);
      expect(headers).toHaveLength(7);
    });

    it('should have DATE SCHEDULED header as the 4th header', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders(true);
      const dateHeader = headers[3];
      expect(dateHeader.title).toBe('DATE SCHEDULED');
      expect(dateHeader.key).toBe('nextAppearanceDt');
      expect(dateHeader.width).toBe('8.5rem');
      expect(dateHeader.maxWidth).toBe('8.5rem');
    });

    it('should have value formatter for DATE SCHEDULED header', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders(true);
      const dateHeader = headers[3];
      expect(dateHeader.value).toBeDefined();
      expect(typeof dateHeader.value).toBe('function');
    });

    it('should have sortRaw function for DATE SCHEDULED header', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders(true);
      const dateHeader = headers[3];
      expect(dateHeader.sortRaw).toBeDefined();
      expect(typeof dateHeader.sortRaw).toBe('function');
    });

    it('should sort DATE SCHEDULED by date correctly', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders(true);
      const dateHeader = headers[3];
      
      const item1 = { nextAppearanceDt: '2024-02-10' } as civilDocumentType;
      const item2 = { nextAppearanceDt: '2024-09-15' } as civilDocumentType;
      
      const result = dateHeader.sortRaw!(item1, item2);
      expect(result).toBeLessThan(0); // item1 is before item2
      
      const reverseResult = dateHeader.sortRaw!(item2, item1);
      expect(reverseResult).toBeGreaterThan(0); // item2 is after item1
    });

    it('should have same first 3 headers as when isScheduledCategory is false', () => {
      const headersScheduled = shared.getBaseCivilDocumentTableHeaders(true);
      const headersNotScheduled = shared.getBaseCivilDocumentTableHeaders(false);
      
      expect(headersScheduled[0]).toEqual(headersNotScheduled[0]);
      expect(headersScheduled[1]).toEqual(headersNotScheduled[1]);
      expect(headersScheduled[2]).toEqual(headersNotScheduled[2]);
    });

    it('should have different 4th header than when isScheduledCategory is false', () => {
      const headersScheduled = shared.getBaseCivilDocumentTableHeaders(true);
      const headersNotScheduled = shared.getBaseCivilDocumentTableHeaders(false);
      
      expect(headersScheduled[3].title).not.toEqual(headersNotScheduled[3].title);
      expect(headersScheduled[3].key).not.toEqual(headersNotScheduled[3].key);
    });
  });

  describe('sortRaw edge cases', () => {
    it('should handle equal dates in DATE FILED sortRaw', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders(false);
      const dateHeader = headers[3];
      
      const item1 = { filedDt: '2024-05-15' } as civilDocumentType;
      const item2 = { filedDt: '2024-05-15' } as civilDocumentType;
      
      const result = dateHeader.sortRaw!(item1, item2);
      expect(result).toBe(0);
    });

    it('should handle equal dates in DATE SCHEDULED sortRaw', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders(true);
      const dateHeader = headers[3];
      
      const item1 = { nextAppearanceDt: '2024-07-20' } as civilDocumentType;
      const item2 = { nextAppearanceDt: '2024-07-20' } as civilDocumentType;
      
      const result = dateHeader.sortRaw!(item1, item2);
      expect(result).toBe(0);
    });

    it('should handle equal dates in ORDER MADE sortRaw', () => {
      const headers = shared.getBaseCivilDocumentTableHeaders();
      const orderMadeHeader = headers[4];
      
      const item1 = { orderMadeDt: '2024-04-10' } as civilDocumentType;
      const item2 = { orderMadeDt: '2024-04-10' } as civilDocumentType;
      
      const result = orderMadeHeader.sortRaw!(item1, item2);
      expect(result).toBe(0);
    });
  });
});
