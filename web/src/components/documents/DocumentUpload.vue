<template>
  <div
    class="px-4 py-3 rounded-lg"
    :class="{ 'upload-disabled': props.disabled }"
    style="background-color: rgba(66, 133, 244, 0.04); border: 1px solid rgba(66, 133, 244, 0.2)"
  >
    <label style="display: flex; align-items: center; gap: 8px; cursor: pointer">
      <v-checkbox
        v-model="showDocumentUpload"
        density="compact"
        hide-details
        style="margin: 0; padding: 0; min-width: 24px"
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
          filter-by-type=".pdf,.doc,.docx,.png,.jpg,.jpeg"
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
        <p v-if="selectedUpload" class="text-caption text-success mt-2">
          ✓ {{ selectedUpload.name }}
        </p>
      </div>
    </v-expand-transition>
  </div>
</template>

<script setup lang="ts">
  import { ref, watch } from 'vue';

  const show = defineModel<boolean>('show', { type: Boolean, required: true });
  const props = defineProps<{
    disabled: boolean;
  }>();
  const selectedFile = defineModel<File | null>('selectedFile', {
    default: null,
  });

  const showDocumentUpload = ref<boolean>(false);
  const selectedUpload = ref<File | null>(null);
  const rejectedUploadMessage = ref<string>('');

  // Sync internal state with model
  watch(selectedUpload, (newVal) => {
    selectedFile.value = newVal;
  });

  watch(
    () => show.value,
    (newVal) => {
      if (!newVal) {
        showDocumentUpload.value = false;
        selectedUpload.value = null;
        rejectedUploadMessage.value = '';
      }
    }
  );

  watch(showDocumentUpload, (newVal) => {
    if (!newVal) {
      rejectedUploadMessage.value = '';
    }
  });

  watch(
    () => props.disabled,
    (newVal) => {
      if (newVal) {
        showDocumentUpload.value = false;
      }
    }
  );

  const onDocumentSelected = (files: File[] | File | null | undefined) => {
    if (props.disabled) {
      return;
    }

    rejectedUploadMessage.value = '';

    if (!files) {
      selectedUpload.value = null;
      return;
    }

    selectedUpload.value = Array.isArray(files) ? (files[0] ?? null) : files;
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
  .upload-disabled {
    opacity: 0.65;
    pointer-events: none;
  }
</style>
