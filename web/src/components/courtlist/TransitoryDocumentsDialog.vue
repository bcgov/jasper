<template>
  <v-dialog v-model="isOpen" fullscreen>
    <v-card>
      <v-toolbar color="primary">
        <v-toolbar-title
          >Transitory Documents - {{ props.date }} {{ props.location }} Room
          {{ props.roomCd }} {{
        }}</v-toolbar-title>
        <v-spacer></v-spacer>
        <v-btn :icon="mdiClose" @click="close" title="close"></v-btn>
      </v-toolbar>

      <v-card-text>
        <v-container>
          <v-row
            v-if="error || (documents.length === 0 && !loading)"
            justify="center"
            class="my-5"
          >
            <v-col cols="12" md="6">
              <v-alert v-if="error" type="error" border="start">
                {{ error }}
              </v-alert>
              <v-alert v-else type="info" border="start">
                No documents found for this location and date.
              </v-alert>
            </v-col>
          </v-row>

          <v-skeleton-loader class="my-1" type="table" :loading="loading">
            <v-data-table
              v-if="documents.length > 0"
              v-model="selectedDocuments"
              :headers="headers"
              :items="documents"
              :sort-by="[{ key: 'matchedRoomFolder', order: 'desc' }]"
              class="elevation-1"
              fixed-header
              height="calc(100vh - 200px)"
              :show-select="canViewDocuments"
              return-object
              :item-value="(item) => item.relativePath"
              :item-selectable="(item) => canViewDocuments && isPdf(item)"
              data-testid="documents-table"
            >
              <template v-slot:item.fileName="{ item }">
                <a
                  v-if="canViewDocuments && isSupportedByNutrient(item)"
                  href="#"
                  @click.prevent="openInNutrient(item)"
                  class="text-primary"
                >
                  {{ item.fileName }}
                </a>
                <span v-else>{{ item.fileName }}</span>
              </template>
              <template v-slot:item.actions="{ item }">
                <template v-if="canDownloadDocuments">
                  <v-progress-circular
                    v-if="
                      item.relativePath && downloadingFiles[item.relativePath]
                    "
                    indeterminate
                    color="primary"
                    size="20"
                    width="2"
                  ></v-progress-circular>
                  <v-btn
                    v-else
                    :icon="mdiDownload"
                    variant="text"
                    size="small"
                    data-testid="download-file-btn"
                    @click="downloadFile(item)"
                    title="Download file"
                  ></v-btn>
                </template>
              </template>
              <template v-slot:item.sizeBytes="{ item }">
                {{ formatFileSize(item.sizeBytes) }}
              </template>
              <template v-slot:item.createdUtc="{ item }">
                {{ formatDate(item.createdUtc) }}
              </template>
            </v-data-table>
          </v-skeleton-loader>
        </v-container>
      </v-card-text>
    </v-card>

    <ActionBar
      v-if="canViewDocuments"
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
        data-testid="view-documents"
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
  import { useCommonStore } from '@/stores';
  import { mdiClose, mdiDownload, mdiFileDocumentOutline } from '@mdi/js';
  import { computed, inject, ref, watch } from 'vue';
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

  const commonStore = useCommonStore();
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
  const downloadingFiles = ref<Record<string, boolean>>({});

  const DOWNLOAD_TRANSITORY_DOCUMENTS = 'DOWNLOAD_TRANSITORY_DOCUMENTS';
  const VIEW_TRANSITORY_DOCUMENTS = 'VIEW_TRANSITORY_DOCUMENTS';

  const canViewDocuments = computed(
    () =>
      commonStore.userInfo?.permissions?.includes(VIEW_TRANSITORY_DOCUMENTS) ??
      false
  );

  const canDownloadDocuments = computed(
    () =>
      commonStore.userInfo?.permissions?.includes(
        DOWNLOAD_TRANSITORY_DOCUMENTS
      ) ?? false
  );

  const headers = [
    { title: 'Room', key: 'matchedRoomFolder', sortable: true },
    { title: 'File Name', key: 'fileName', sortable: true },
    { title: 'Extension', key: 'extension', sortable: true },
    { title: 'Created', key: 'createdUtc', sortable: true },
    { title: 'Size', key: 'sizeBytes', sortable: true },
    {
      title: 'Actions',
      key: 'actions',
      sortable: false,
      align: 'center' as const,
    },
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
    } catch (e: any) {
      console.error('Error fetching transitory documents:', e);

      // Check for response status code
      const status = e?.response?.status || e?.statusCode;

      if (status === 404) {
        error.value = 'No documents were found for this location and date.';
      } else if (status === 401 || status === 403 || status >= 500) {
        error.value =
          'Server error occurred while retrieving documents. Please try again later or contact your administrator.';
      } else {
        error.value = 'Failed to load documents. Please try again.';
      }
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

  const isSupportedByNutrient = (item: FileMetadataDto): boolean => {
    const ext = item.extension?.toLowerCase();
    return ext === '.pdf' || ext === '.doc' || ext === '.docx';
  };

  const openInNutrient = async (item: FileMetadataDto) => {
    try {
      // Store single file in session storage
      sessionStorage.setItem('transitoryDocuments', JSON.stringify([item]));

      // Open in new tab
      const route = router.resolve({
        name: 'NutrientContainer',
        query: { type: 'transitory-bundle' },
      });
      window.open(route.href, '_blank');
    } catch (e) {
      downloadError.value = true;
      downloadErrorMessage.value =
        'Failed to open document in viewer. Please try again.';
      console.error('Error opening document in viewer:', e);
    }
  };

  const downloadFile = async (item: FileMetadataDto) => {
    if (!item.relativePath || !canDownloadDocuments.value) return;

    downloadingFiles.value[item.relativePath] = true;
    try {
      await transitoryDocumentsService?.downloadFile(item);
    } catch (e) {
      downloadError.value = true;
      downloadErrorMessage.value = 'Failed to download file. Please try again.';
      console.error('Error downloading file:', e);
    } finally {
      downloadingFiles.value[item.relativePath] = false;
    }
  };

  const handleViewDocuments = async () => {
    if (!canViewDocuments.value || selectedDocuments.value.length === 0) {
      return;
    }

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
