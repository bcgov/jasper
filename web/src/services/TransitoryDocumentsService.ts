import { FileMetadataDto } from '@/types/transitory-documents';
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
      { responseType: 'blob', skipErrorHandler: true }
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

  async fetchFileForViewer(fileMetadata: FileMetadataDto): Promise<string> {
    const blob = await this.httpService.get<Blob>(
      'api/TransitoryDocuments/download',
      {
        fileName: fileMetadata.fileName,
        extension: fileMetadata.extension,
        sizeBytes: fileMetadata.sizeBytes,
        createdUtc: fileMetadata.createdUtc,
        relativePath: fileMetadata.relativePath,
        matchedRoomFolder: fileMetadata.matchedRoomFolder,
      },
      { responseType: 'blob', skipErrorHandler: true }
    );

    // Convert blob to base64
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onloadend = () => {
        const base64String = reader.result as string;
        // Remove the data URL prefix (e.g., "data:application/pdf;base64,")
        const base64 = base64String.split(',')[1];
        resolve(base64);
      };
      reader.onerror = reject;
      reader.readAsDataURL(blob);
    });
  }

  async mergePdfs(files: FileMetadataDto[]): Promise<{
    base64Pdf: string;
    pageRanges: Array<{ start: number; end?: number }>;
  }> {
    const response = await this.httpService.post<{
      base64Pdf: string;
      pageRanges: Array<{ start: number; end?: number }>;
    }>('api/TransitoryDocuments/merge', { files }, {}, 'json');

    return response;
  }
}
