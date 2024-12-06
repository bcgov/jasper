import { KeyValueInfo } from '@/types/common';
import { CourtFileSearchCriteria, FileDetail } from '@/types/courtFileSearch';
import { defineStore } from 'pinia';

export const useCourtFileSearchStore = defineStore('CourtFileSearchStore', {
  state: () => ({
    filesForViewing: [] as KeyValueInfo[],
    currentViewedFileId: '',
    searchCriteria: {} as CourtFileSearchCriteria,
    searchResults: [] as FileDetail[],
  }),
  getters: {
    selectedFiles: (state) => state.filesForViewing,
    currentFileId: (state) => state.currentViewedFileId,
    currentSearchCriteria: (state) => state.searchCriteria,
    currentSearchResults: (state) => state.searchResults,
  },
  actions: {
    updateCurrentViewedFileId(fileId: string): void {
      this.currentViewedFileId = fileId;
    },
    addFilesForViewing({ searchCriteria, searchResults, files }): void {
      this.searchCriteria = searchCriteria;
      this.searchResults = [...searchResults];
      this.filesForViewing = [...files];
      this.currentViewedFileId = this.filesForViewing[0].key;
    },
    clearSelectedFiles(): void {
      this.filesForViewing.length = 0;
      this.searchCriteria = {} as CourtFileSearchCriteria;
      this.searchResults = [];
      this.currentViewedFileId = '';
    },
    removeCurrentViewedFileId(fileId: string): void {
      this.filesForViewing = this.filesForViewing.filter(
        (c) => c.key !== fileId
      );
      this.currentViewedFileId =
        this.filesForViewing.length > 0
          ? this.filesForViewing[this.filesForViewing.length - 1].key
          : '';
    },
  },
});
