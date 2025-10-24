export interface FileMetadataDto {
  fileName: string;
  extension: string;
  sizeBytes: number;
  createdUtc: string;
  absolutePath: string;
  matchedRoomFolder: string | null;
}
