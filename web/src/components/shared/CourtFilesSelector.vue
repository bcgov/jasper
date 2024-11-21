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
import { KeyValueInfo } from '@/types/common';
import { defineComponent } from 'vue';
import { useCourtFileSearchStore } from "@stores";

export default defineComponent('CourtFilesSelector', {
  data() {
    const store = useCourtFileSearchStore();

    return {
      store
    }
  },
  computed: {
    fileId: {
      get(): string {
        return this.store.currentFileId;
      },
      set(newFileId: string) {
        this.store.updateCurrentViewedFileId(newFileId);
      },
    },
    currentFileId(): string {
      return this.store.currentFileId;
    },
  },
  methods: {
    handleChange() {
      this.$router.replace({
        name: this.targetCaseDetails,
        params: { fileNumber: this.currentFileId },
      });
      this.$emit('reload-case-details');
    },
    handleRemove() {
      this.store.removeCurrentViewedFileId(this.currentFileId);
      if (this.currentFileId) {
        this.$router.replace({
          name: this.targetCaseDetails,
          params: { fileNumber: this.currentFileId },
        });
        this.$emit('reload-case-details');
      } else {
        this.$router.push({ name: 'CourtFileSearchView' });
      }
    },
    handleAdd() {
      this.$router.push({ name: 'CourtFileSearchView' });
    },
  },
  props: {
    files: { type: Array, default: () => [] },
    targetCaseDetails: { type: String, default: () => '' },
  },
});
</script>
