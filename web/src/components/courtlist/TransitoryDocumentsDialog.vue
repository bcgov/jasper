<template>
  <v-dialog v-model="isOpen" fullscreen>
    <v-card>
      <v-toolbar color="primary">
        <v-toolbar-title
          >Transitory Documents - {{ props.date }} {{ props.location }} Room
          {{ props.roomCd }} {{
        }}</v-toolbar-title>
        <v-spacer></v-spacer>
        <v-btn :icon="mdiClose" @click="close"></v-btn>
      </v-toolbar>

      <v-card-text>
        <v-container>
          <v-row v-if="loading" justify="center" class="my-5">
            <v-progress-circular
              indeterminate
              color="primary"
              size="64"
            ></v-progress-circular>
          </v-row>

          <v-row v-else-if="error" justify="center" class="my-5">
            <v-col cols="12" md="6">
              <v-alert type="error" border="start">
                {{ error }}
              </v-alert>
            </v-col>
          </v-row>

          <v-row
            v-else-if="documents.length === 0"
            justify="center"
            class="my-5"
          >
            <v-col cols="12" md="6">
              <v-alert type="info" border="start">
                No documents found for this location and date.
              </v-alert>
            </v-col>
          </v-row>

          <v-data-table
            v-else
            v-model="selectedDocuments"
            :headers="headers"
            :items="documents"
            :sort-by="[{ key: 'matchedRoomFolder', order: 'desc' }]"
            class="elevation-1"
            fixed-header
            height="calc(100vh - 200px)"
            show-select
            return-object
            :item-value="(item) => item.absolutePath"
            :item-selectable="(item) => isPdf(item)"
          >
            <template v-slot:item.fileName="{ item }">
              <a
                href="#"
                @click.prevent="downloadFile(item)"
                class="text-primary"
              >
                {{ item.fileName }}
              </a>
            </template>
            <template v-slot:item.sizeBytes="{ item }">
              {{ formatFileSize(item.sizeBytes) }}
            </template>
            <template v-slot:item.createdUtc="{ item }">
              {{ formatDate(item.createdUtc) }}
            </template>
          </v-data-table>
        </v-container>
      </v-card-text>
    </v-card>

    <ActionBar
      :selected="selectedDocuments"
      selectionPrependText="Documents"
      @clicked="handleViewDocuments"
    >
      <v-btn
        size="large"
        class="mx-2"
        :prepend-icon="mdiFileDocumentOutline"
        style="letter-spacing: 0.001rem"
        @click="handleViewDocuments"
      >
        View documents
      </v-btn>
    </ActionBar>

    <v-snackbar v-model="downloadError" color="error" :timeout="5000">
      {{ downloadErrorMessage }}
      <template v-slot:actions>
        <v-btn variant="text" @click="downloadError = false">Close</v-btn>
      </template>
    </v-snackbar>
  </v-dialog>
</template>

<script setup lang="ts">
  import ActionBar from '@/components/shared/table/ActionBar.vue';
  import { TransitoryDocumentsService } from '@/services/TransitoryDocumentsService';
  import { FileMetadataDto } from '@/types/transitory-documents';
  import { mdiClose, mdiFileDocumentOutline } from '@mdi/js';
  import { inject, ref, watch } from 'vue';
  import { useRouter } from 'vue-router';

  const props = defineProps<{
    modelValue: boolean;
    locationId: string;
    roomCd: string;
    date: string;
    location: string;
  }>();

  const emit = defineEmits<{
    'update:modelValue': [value: boolean];
  }>();

  const transitoryDocumentsService = inject<TransitoryDocumentsService>(
    'transitoryDocumentsService'
  );
  const router = useRouter();

  const isOpen = ref(props.modelValue);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const documents = ref<FileMetadataDto[]>([]);
  const selectedDocuments = ref<FileMetadataDto[]>([]);
  const downloadError = ref(false);
  const downloadErrorMessage = ref('');

  const headers = [
    { title: 'Room', key: 'matchedRoomFolder', sortable: true },
    { title: 'File Name', key: 'fileName', sortable: true },
    { title: 'Extension', key: 'extension', sortable: true },
    { title: 'Created', key: 'createdUtc', sortable: true },
    { title: 'Size', key: 'sizeBytes', sortable: true },
  ];

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  };

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleString();
  };

  const fetchDocuments = async () => {
    if (!props.locationId || !props.roomCd || !props.date) return;

    loading.value = true;
    error.value = null;
    documents.value = [];

    try {
      const result = await transitoryDocumentsService?.searchDocuments(
        props.locationId,
        props.roomCd,
        props.date
      );
      documents.value = result || [];
    } catch (e) {
      error.value = 'Failed to load documents. Please try again.';
      console.error('Error fetching transitory documents:', e);
    } finally {
      loading.value = false;
    }
  };

  const close = () => {
    emit('update:modelValue', false);
    selectedDocuments.value = [];
  };

  const isPdf = (item: FileMetadataDto): boolean => {
    return item.extension?.toLowerCase() === '.pdf';
  };

  const downloadFile = async (item: FileMetadataDto) => {
    try {
      await transitoryDocumentsService?.downloadFile(item);
    } catch (e) {
      downloadError.value = true;
      downloadErrorMessage.value = 'Failed to download file. Please try again.';
      console.error('Error downloading file:', e);
    }
  };

  const handleViewDocuments = async () => {
    if (selectedDocuments.value.length === 0) return;

    const pdfDocuments = selectedDocuments.value.filter((doc) => isPdf(doc));

    if (pdfDocuments.length === 0) {
      downloadError.value = true;
      downloadErrorMessage.value =
        'Please select PDF files to view in the document viewer.';
      return;
    }

    if (pdfDocuments.length !== selectedDocuments.value.length) {
      downloadError.value = true;
      downloadErrorMessage.value =
        'Only PDF files can be viewed. Non-PDF files have been excluded.';
    }

    try {
      sessionStorage.setItem(
        'transitoryDocuments',
        JSON.stringify(pdfDocuments)
      );

      // Open in new tab
      const route = router.resolve({
        name: 'NutrientContainer',
        query: { type: 'transitory-bundle' },
      });
      window.open(route.href, '_blank');
    } catch (e) {
      downloadError.value = true;
      downloadErrorMessage.value =
        'Failed to open documents. Please try again.';
      console.error('Error opening documents in viewer:', e);
    }
  };

  watch(
    () => props.modelValue,
    (newValue) => {
      isOpen.value = newValue;
      if (newValue) {
        fetchDocuments();
      }
    }
  );

  watch(isOpen, (newValue) => {
    if (!newValue) {
      close();
    }
  });
</script>

<style scoped>
  :deep(.action-bar) {
    max-width: fit-content !important;
    width: auto !important;
  }
</style>
