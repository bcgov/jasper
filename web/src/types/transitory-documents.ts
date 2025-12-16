export interface FileMetadataDto {
  fileName: string;
  extension: string;
  sizeBytes: number;
  createdUtc: string;
  relativePath: string;
  matchedRoomFolder: string | null;
}
