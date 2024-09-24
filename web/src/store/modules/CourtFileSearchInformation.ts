import { KeyValueInfo } from '@/types/common';
import { CourtFileSearchCriteria, FileDetail } from '@/types/courtFileSearch';
import { Module, Mutation, VuexModule } from 'vuex-module-decorators';

const NAMESPACE = 'CourtFileSearchInformation';

export const ADD_FILES_FOR_VIEWING = `${NAMESPACE}/addFilesForViewing`;
export const CLEAR_SELECTED_FILES = `${NAMESPACE}/clearSelectedFiles`;
export const UPDATE_CURRENT_VIEWED_FILE_ID = `${NAMESPACE}/updateCurrentViewedFileId`;
export const REMOVE_CURRENT_VIEWED_FILE_ID = `${NAMESPACE}/removeCurrentViewedFileId`;
export const GET_SELECTED_FILES = `${NAMESPACE}/selectedFiles`;
export const GET_CURRENT_SEARCH_CRITERIA = `${NAMESPACE}/currentSearchCriteria`;
export const GET_CURRENT_SEARCH_RESULTS = `${NAMESPACE}/currentSearchResults`;

@Module({
  namespaced: true,
  name: NAMESPACE
})
export default class CourtFileSearchInformation extends VuexModule {
  public filesForViewing: KeyValueInfo[] = [];
  public currentViewedFileId = "";
  public searchCriteria?: CourtFileSearchCriteria;
  public searchResults?: FileDetail[];

  get selectedFiles(): KeyValueInfo[] {
    return this.filesForViewing;
  }

  get currentFileId(): string {
    return this.currentViewedFileId;
  }

  get currentSearchCriteria(): CourtFileSearchCriteria | undefined {
    return this.searchCriteria;
  }

  get currentSearchResults(): FileDetail[] | undefined {
    return this.searchResults;
  }

  @Mutation
  public updateCurrentViewedFileId(fileId: string): void {
    this.currentViewedFileId = fileId;
  }

  @Mutation
  public addFilesForViewing({ searchCriteria, searchResults, files }) {
    this.searchCriteria = searchCriteria;
    this.searchResults = [...searchResults];
    this.filesForViewing = [...files];
    this.currentViewedFileId = this.filesForViewing[0].key;
  }

  @Mutation
  public clearSelectedFiles() {
    this.filesForViewing.length = 0;
    this.searchCriteria = undefined;
    this.searchResults = undefined;
    this.currentViewedFileId = "";
  }

  @Mutation
  public removeCurrentViewedFileId(fileId: string): void {
    this.filesForViewing = this.filesForViewing.filter(c => c.key !== fileId);
    this.currentViewedFileId = this.filesForViewing.length > 0 ? this.filesForViewing[this.filesForViewing.length - 1].key : "";
  }
}