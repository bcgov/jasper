import { KeyValueInfo } from '@/types/common';
import { CourtFileSearchCriteria, FileDetail } from '@/types/courtFileSearch';

type FilesForViewingPayload = {
  searchCriteria: CourtFileSearchCriteria | Record<string, never>;
  searchResults: FileDetail[];
  files: KeyValueInfo[];
};

type CourtFileSearchSyncStore = {
  selectedFiles: KeyValueInfo[];
  currentSearchCriteria: CourtFileSearchCriteria;
  currentSearchResults: FileDetail[];
  addFilesForViewing(payload: FilesForViewingPayload): void;
};

export const syncSelectedFilesForCurrentCase = (
  courtFileSearchStore: CourtFileSearchSyncStore,
  currentFileNumber: string,
  currentFileNumberText: string
) => {
  const currentFile: KeyValueInfo = {
    key: currentFileNumber,
    value: currentFileNumberText,
  };

  const existingFiles = [...courtFileSearchStore.selectedFiles];
  const currentIndex = existingFiles.findIndex(
    (f) => f.key === currentFileNumber
  );

  if (currentIndex < 0) {
    courtFileSearchStore.addFilesForViewing({
      searchCriteria: {},
      searchResults: [],
      files: [currentFile],
    });
    return;
  }

  if (existingFiles[currentIndex].value !== currentFileNumberText) {
    existingFiles[currentIndex] = currentFile;
    courtFileSearchStore.addFilesForViewing({
      searchCriteria: courtFileSearchStore.currentSearchCriteria,
      searchResults: courtFileSearchStore.currentSearchResults,
      files: existingFiles,
    });
  }
};
