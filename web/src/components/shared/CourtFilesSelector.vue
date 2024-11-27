<template>
  <div class="mt-2 p-2 bg-light">
    <h4 class="mb-2">Files to View ({{ files.length }})</h4>
    <b-form-select
      v-model="fileId"
      class="extra-sm mb-2"
      @change="handleChange"
    >
      <option
        v-for="option in selectedFiles"
        :key="option.value"
        :value="option.key"
      >
        {{ option.value }}
      </option>
    </b-form-select>
    <div class="d-flex mb-2">
      <b-button
        class="flex-fill mr-2"
        variant="outline-primary"
        @click="handleAdd"
        >Add File(s)</b-button
      >
      <b-button
        class="flex-fill"
        variant="outline-primary"
        @click="handleRemove"
        >Remove this File</b-button
      >
    </div>
  </div>
</template>
<script lang="ts">
  import { useCourtFileSearchStore } from '@/stores';
  import { KeyValueInfo } from '@/types/common';
  import { computed, defineComponent, PropType } from 'vue';
  import { useRouter } from 'vue-router';

  export default defineComponent({
    props: {
      files: { type: Array as PropType<KeyValueInfo[]>, default: () => [] },
      targetCaseDetails: { type: String, default: '' },
    },
    setup(props, { emit }) {
      const store = useCourtFileSearchStore();
      const router = useRouter();

      const fileId = computed({
        get: () => store.currentFileId,
        set: (newFileId: string) => {
          store.updateCurrentViewedFileId(newFileId);
        },
      });

      const currentFileId = computed(() => store.currentFileId);

      const handleChange = () => {
        router.replace({
          name: props.targetCaseDetails,
          params: { fileNumber: currentFileId.value },
        });
        emit('reload-case-details');
      };

      const handleRemove = () => {
        store.removeCurrentViewedFileId(currentFileId.value);
        if (currentFileId.value) {
          router.replace({
            name: props.targetCaseDetails,
            params: { fileNumber: currentFileId.value },
          });
          emit('reload-case-details');
        } else {
          router.push({ name: 'CourtFileSearchView' });
        }
      };

      const handleAdd = () => {
        router.push({ name: 'CourtFileSearchView' });
      };

      return {
        fileId,
        currentFileId,
        selectedFiles: props.files,
        handleChange,
        handleRemove,
        handleAdd,
      };
    },
  });
</script>
