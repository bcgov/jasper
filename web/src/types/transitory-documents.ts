export interface FileMetadataDto {
  fileName: string;
  extension: string;
  sizeBytes: number;
  createdUtc: string;
  relativePath: string;
  matchedRoomFolder: string | null;
}

export interface TransitoryMergeContext {
  locationId: string;
  roomCd: string;
  date: string;
}

export interface TransitoryViewerPayload {
  files: FileMetadataDto[];
  context: TransitoryMergeContext;
}
