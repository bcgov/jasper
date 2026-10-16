import { IHttpService } from '@/services/HttpService';
import { TransitoryDocumentsService } from '@/services/TransitoryDocumentsService';
import { FileMetadataDto } from '@/types/transitory-documents';
import { beforeEach, describe, expect, it, Mock, vi } from 'vitest';

const mockHttpService = {
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  patch: vi.fn(),
  delete: vi.fn(),
  getUri: vi.fn(),
} as unknown as IHttpService;

describe('TransitoryDocumentsService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('suppresses the generic snackbar for merge errors', async () => {
    const service = new TransitoryDocumentsService(mockHttpService as never);
    const files: FileMetadataDto[] = [
      {
        fileName: 'document.docx',
        extension: '.docx',
        sizeBytes: 123,
        createdUtc: '2026-01-01T00:00:00Z',
        relativePath: 'room/document.docx',
        matchedRoomFolder: '101',
      },
    ];
    const response = { base64Pdf: 'pdf', pageRanges: [{ start: 0, end: 1 }] };
    (mockHttpService.post as Mock).mockResolvedValue(response);

    await expect(
      service.mergePdfs(files, {
        locationId: '1',
        roomCd: '101',
        date: '2026-01-01',
      })
    ).resolves.toEqual(response);

    expect(mockHttpService.post).toHaveBeenCalledWith(
      'api/TransitoryDocuments/merge',
      {
        files,
        locationId: '1',
        roomCd: '101',
        date: '2026-01-01',
      },
      {},
      'json',
      { suppressErrorNotification: true, signal: undefined }
    );
  });
});
