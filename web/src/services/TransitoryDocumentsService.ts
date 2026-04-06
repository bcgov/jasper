import {
  FileMetadataDto,
  TransitoryMergeContext,
} from '@/types/transitory-documents';
import { HttpService } from './HttpService';

export class TransitoryDocumentsService {
  constructor(private readonly httpService: HttpService) {}

  async searchDocuments(
    locationId: string,
    roomCd: string,
    date: string
  ): Promise<FileMetadataDto[]> {
    return this.httpService.get<FileMetadataDto[]>('api/TransitoryDocuments', {
      locationId,
      roomCd,
      date,
    });
  }

  async downloadFile(fileMetadata: FileMetadataDto): Promise<void> {
    const response = await this.httpService.get<Blob>(
      'api/TransitoryDocuments/download',
      {
        fileName: fileMetadata.fileName,
        extension: fileMetadata.extension,
        sizeBytes: fileMetadata.sizeBytes,
        createdUtc: fileMetadata.createdUtc,
        relativePath: fileMetadata.relativePath,
        matchedRoomFolder: fileMetadata.matchedRoomFolder,
      },
      { responseType: 'blob' }
    );

    // Extract filename from fileMetadata
    const fileName = fileMetadata.fileName;

    // Create a download link and trigger it
    const url = globalThis.URL.createObjectURL(response);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    globalThis.URL.revokeObjectURL(url);
  }

  async mergePdfs(
    files: FileMetadataDto[],
    context?: TransitoryMergeContext
  ): Promise<{
    base64Pdf: string;
    pageRanges: Array<{ start: number; end?: number }>;
  }> {
    const response = await this.httpService.post<{
      base64Pdf: string;
      pageRanges: Array<{ start: number; end?: number }>;
    }>('api/TransitoryDocuments/merge', { files, ...context }, {}, 'json');

    return response;
  }
}
