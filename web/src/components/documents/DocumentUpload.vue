<template>
  <div
    class="px-4 py-3 rounded-lg upload-container"
    :class="{ 'upload-disabled': props.disabled }"
  >
    <label class="upload-label">
      <v-checkbox
        v-model="showDocumentUpload"
        density="compact"
        hide-details
        class="upload-checkbox"
        :disabled="props.disabled"
      />
      <span>Attach supporting document (optional)</span>
    </label>
    <v-expand-transition>
      <div v-if="showDocumentUpload" class="mt-2">
        <v-file-upload
          data-testid="review-document-upload"
          title="Upload document"
          density="comfortable"
          clearable
          :multiple="false"
          filter-by-type=".pdf,.doc,.docx"
          scrim="primary"
          :disabled="props.disabled"
          @update:model-value="onDocumentSelected"
          @rejected="onDocumentRejected"
        />
        <v-alert
          v-if="rejectedUploadMessage"
          type="error"
          variant="tonal"
          density="comfortable"
          class="mt-2"
        >
          {{ rejectedUploadMessage }}
        </v-alert>
        <p v-if="selectedFile" class="text-caption text-success mt-2">
          ✓ {{ selectedFile.name }}
        </p>
      </div>
    </v-expand-transition>
  </div>
</template>

<script setup lang="ts">
  import { computed, ref, watch } from 'vue';

  const show = defineModel<boolean>('show', { type: Boolean, required: true });
  const props = defineProps<{
    disabled: boolean;
  }>();
  const selectedFile = defineModel<File | null>('selectedFile', {
    default: null,
  });

  const selectedUpload = computed<File | null>({
    get: () => selectedFile.value,
    set: (file) => {
      selectedFile.value = file;
    },
  });

  const showDocumentUpload = ref<boolean>(false);
  const rejectedUploadMessage = ref<string>('');

  const clearUploadState = () => {
    selectedUpload.value = null;
    rejectedUploadMessage.value = '';
  };

  watch([() => show.value, () => props.disabled], ([newShow, newDisabled]) => {
    if (!newShow || newDisabled) {
      showDocumentUpload.value = false;
    }
  });

  watch(showDocumentUpload, (newVal) => {
    if (!newVal) {
      clearUploadState();
    }
  });

  const onDocumentSelected = (files: File[] | File | null | undefined) => {
    if (props.disabled) {
      return;
    }

    rejectedUploadMessage.value = '';

    if (!files) {
      selectedFile.value = null;
      return;
    }

    selectedFile.value = Array.isArray(files) ? (files[0] ?? null) : files;
  };

  const onDocumentRejected = (files: File[]) => {
    if (props.disabled) {
      return;
    }

    if (!files.length) {
      rejectedUploadMessage.value = '';
      return;
    }

    const allowedTypes = 'PDF, DOC, DOCX, PNG, JPG, JPEG';
    rejectedUploadMessage.value =
      files.length === 1
        ? `${files[0].name} is not a supported file type. Allowed types: ${allowedTypes}.`
        : `${files.length} files were not supported. Allowed types: ${allowedTypes}.`;
  };
</script>

<style scoped>
  .upload-container {
    background-color: rgba(66, 133, 244, 0.04);
    border: 1px solid rgba(66, 133, 244, 0.2);
  }

  .upload-label {
    display: flex;
    align-items: center;
    gap: 8px;
    cursor: pointer;
  }

  .upload-checkbox {
    margin: 0;
    padding: 0;
    min-width: 24px;
  }

  .upload-disabled {
    opacity: 0.65;
    pointer-events: none;
  }
</style>
