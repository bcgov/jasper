import shared from '@/components/shared';
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
