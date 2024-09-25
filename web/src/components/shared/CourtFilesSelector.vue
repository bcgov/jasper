<template>
  <div class="mt-2 p-2 bg-light">
    <h4 class="sm mb-2">Files to View ({{ files.length }})</h4>
    <b-form-select v-model="selectedFileId" class="extra-sm mb-2" @change="handleChange">
      <option v-for="option in files" :key="option.value" :value="option.key">
        {{ option.value }}
      </option>
    </b-form-select>
    <div class="d-flex mb-2">
      <b-button class="extra-sm flex-fill mr-2" variant="outline-primary" @click="handleAdd">Add File(s)</b-button>
      <b-button class="extra-sm flex-fill" variant="outline-primary" @click="handleRemove">Remove this File</b-button>
    </div>
  </div>
</template>
<script lang="ts">
import { KeyValueInfo } from '@/types/common';
import { Component, Prop, Vue } from 'vue-property-decorator';

@Component
export default class CourtFilesSelector extends Vue {
  @Prop({ type: Array, default: () => [] })
  files!: KeyValueInfo[];

  @Prop({ type: String, default: () => "" })
  passedFileId;

  selectedFileId = "";

  created() {
    this.selectedFileId = this.passedFileId;
  }

  handleChange() {
    this.$emit('file-changed', this.selectedFileId);
  }

  handleRemove() {
    this.$emit('file-removed', this.selectedFileId);
  }

  handleAdd() {
    this.$emit('add-files');
  }
}
</script>
<style scoped>
.sm {
  font-size: 0.75rem !important;
}

.extra-sm {
  font-size: 0.6rem !important;
}
</style>