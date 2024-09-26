<template>
  <div class="mt-2 p-2 bg-light">
    <h4 class="mb-2">Files to View ({{ files.length }})</h4>
    <b-form-select v-model="fileId" class="extra-sm mb-2" @change="handleChange">
      <option v-for="option in files" :key="option.value" :value="option.key">
        {{ option.value }}
      </option>
    </b-form-select>
    <div class="d-flex mb-2">
      <b-button class="flex-fill mr-2" variant="outline-primary" @click="handleAdd">Add File(s)</b-button>
      <b-button class="flex-fill" variant="outline-primary" @click="handleRemove">Remove this File</b-button>
    </div>
  </div>
</template>
<script lang="ts">
import { REMOVE_CURRENT_VIEWED_FILE_ID, UPDATE_CURRENT_VIEWED_FILE_ID } from '@/store/modules/CourtFileSearchInformation';
import { KeyValueInfo } from '@/types/common';
import { Component, Prop, Vue } from 'vue-property-decorator';
import { namespace } from "vuex-class";

const courtFileSearchState = namespace('CourtFileSearchInformation');

@Component
export default class CourtFilesSelector extends Vue {
  @Prop({ type: Array, default: () => [] })
  files!: KeyValueInfo[];

  @Prop({ type: String, default: () => "" })
  targetCaseDetails;

  @courtFileSearchState.Getter('currentFileId')
  public currentFileId!: string;

  get fileId(): string {
    return this.currentFileId;
  }

  set fileId(newFileId: string) {
    this.$store.commit(UPDATE_CURRENT_VIEWED_FILE_ID, newFileId);
  }

  handleChange() {
    this.$router.replace({ name: this.targetCaseDetails, params: { fileNumber: this.currentFileId } });
    this.$emit('reload-case-details');
  }

  handleRemove() {
    this.$store.commit(REMOVE_CURRENT_VIEWED_FILE_ID, this.currentFileId);
    if (this.currentFileId) {
      this.$router.replace({ name: this.targetCaseDetails, params: { fileNumber: this.currentFileId } });
      this.$emit('reload-case-details');
    } else {
      this.$router.push({ name: "CourtFileSearchView" });
    }
  }

  handleAdd() {
    this.$router.push({ name: "CourtFileSearchView" });
  }
}
</script>