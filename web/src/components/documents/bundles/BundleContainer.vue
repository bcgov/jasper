<template>
  <v-progress-linear v-if="loading" indeterminate />
  <v-skeleton-loader v-if="loading" :loading="loading" type="ossein" />
  <v-row class="py-12" v-if="emptyStore">
    <v-col>
      <p class="text-center mx-auto">No documents available to display.</p>
    </v-col>
  </v-row>

  <div v-show="!loading" ref="pdf-container" class="pdf-container" />
</template>

<script setup lang="ts">
  import { CourtListService } from '@/services';
import { useBundleStore } from '@/stores';
import { appearanceRequest } from '@/stores/BundleStore';
import { ApiResponse } from '@/types/ApiResponse';
import { BinderDocument } from '@/types/BinderDocument';
import {
  CourtListDocumentBundleRequest,
  CourtListDocumentBundleResponse,
} from '@/types/courtlist/jsonTypes';
import { inject, onMounted, onUnmounted, ref } from 'vue';

  const loading = ref(false);
  const emptyStore = ref(false);
  const bundleStore = useBundleStore();
  const configuration = { container: '.pdf-container' };
  const courtListService = inject<CourtListService>('courtListService');
  let count = 0;
  let bundleResponse: ApiResponse<CourtListDocumentBundleResponse> | null = null;

  if (!courtListService) {
    throw new Error('HttpService is not available!');
  }

  const loadNutrient = async () => {
    if (!bundleStore.getAppearanceRequests) {
      return;
    }
    loading.value = true;
    emptyStore.value = false;


    loadMultiple();
  };

  const loadMultiple = async () => {
    const appearanceRequests = bundleStore.getAppearanceRequests;
    const groupedRequests: Record<
      string,
      Record<string, (typeof appearanceRequests)[0][]>
    > = {};

    appearanceRequests.forEach((req) => {
      const fileNumber = req.fileNumber;
      const fullName = req.fullName;

      if (!groupedRequests[fileNumber]) {
        groupedRequests[fileNumber] = {};
      }
      if (!groupedRequests[fileNumber][fullName]) {
        groupedRequests[fileNumber][fullName] = [];
      }
      groupedRequests[fileNumber][fullName].push(req);
    });
    const groupedAppearances = Object.values(groupedRequests).flatMap(
      (fileGroup) =>
        Object.values(fileGroup).flatMap((appearances) =>
          appearances.map((a) => a.appearance)
        )
    );
    const bundleRequest = {
      appearances: groupedAppearances,
    } as unknown as CourtListDocumentBundleRequest;
    bundleResponse = await courtListService.generateCourtListPdf(bundleRequest);

    loading.value = false;

    const outline = configureOutline(groupedRequests);
    let instance = await NutrientViewer.load({
      ...configuration,
      document: `data:application/pdf;base64,${bundleResponse.payload.pdfResponse.base64Pdf}`,
    });

    instance.setDocumentOutline(outline);
    instance.setViewState((viewState) =>
      viewState.set('sidebarMode', NutrientViewer.SidebarMode.DOCUMENT_OUTLINE)
    );
  };

  const configureOutline = (
    groupedRequests: Record<string, Record<string, appearanceRequest[]>>
  ): any => {
    const outline = NutrientViewer.Immutable.List(
      Object.entries(groupedRequests).map(([groupKey, userGroup]) =>
        makeFirstGroup(groupKey, userGroup)
      )
    );
    return outline;
  };

  const makeFirstGroup = (
    groupKey: string,
    userGroup: Record<string, appearanceRequest[]>
  ) => {
    const childrenArray: any[] = [];
    Object.entries(userGroup).forEach(([name, docs]) => {
      childrenArray.push(makeSecondGroup(name, docs));
    });
    const childrenList = NutrientViewer.Immutable.List(childrenArray);
    return new NutrientViewer.OutlineElement({
      title: groupKey,
      isExpanded: true,
      children: childrenList,
    });
  };

  const makeSecondGroup = (memberName: string, docs: appearanceRequest[]) => {
    const fileIds = docs.map((d) => d.appearance.fileId);
    const partIds = docs.map((d) => d.appearance.participantId);
    let binders = bundleResponse?.payload.binders.filter(
      (b) =>
        b.labels &&
        fileIds.some((id) => b.labels.physicalFileId === id) &&
        partIds.some((pid) => b.labels.participantId === pid)
    );
    if (!binders) {
      return;
    }
    return new NutrientViewer.OutlineElement({
      title: memberName,
      isExpanded: true,
      children: NutrientViewer.Immutable.List(
        binders.flatMap((binder) =>
          binder.documents
            .filter((doc) => !isNaN(parseFloat(doc.documentId)))
            .map((doc) => makeDocElement(doc))
        )
      ),
    });
  };

  const makeDocElement = (doc: BinderDocument) => {
    console.log(doc);
    return new NutrientViewer.OutlineElement({
      title: doc.fileName ?? doc.documentType,
      action: new NutrientViewer.Actions.GoToAction({
        pageIndex:
          bundleResponse.payload.pdfResponse.pageRanges?.[count++]?.start,
      }),
    });
  };

  onMounted(() => {
    loadNutrient();
  });

  onUnmounted(() => {
    if (NutrientViewer) {
      NutrientViewer.unload('.pdf-container');
    }
  });
</script>

<style scoped>
  .pdf-container {
    height: 90vh;
  }
  .v-skeleton-loader {
    height: 100%;
  }
</style>
