import { KeyValueInfo } from '@/types/common';
import { Module, Mutation, VuexModule } from 'vuex-module-decorators';

export const ADD_FILES = "SelectedCourtFiles/addFiles";
export const CLEAR_FILES = "SelectedCourtFiles/clearFiles";
export const SET_FILE_ID = "SelectedCourtFiles/setFileId";
export const REMOVE_FILE_ID = "SelectedCourtFiles/removeFileId";

@Module({
  namespaced: true,
  name: 'SelectedCourtFiles'
})
export default class SelectedCourtFiles extends VuexModule {
  public files: KeyValueInfo[] = [];
  public fileId = "";

  get selectedFiles(): KeyValueInfo[] {
    return this.files;
  }

  get currentFileId(): string {
    return this.fileId;
  }

  @Mutation
  public setFileId(fileId: string): void {
    this.fileId = fileId;
  }

  @Mutation
  public addFiles(files: KeyValueInfo[]) {
    this.files = [...files];
    this.fileId = this.files[0].key;
  }

  @Mutation
  public clearFiles() {
    this.files.length = 0;
  }

  @Mutation
  public removeFileId(fileId: string): void {
    this.files = this.files.filter(c => c.key !== fileId);
    this.fileId = this.files.length > 0?  this.files[this.files.length - 1].key : "";
  }
}