<template>
  <div>
    <template
      v-for="(group, courtClass) in groupedSelections"
      :key="courtClass"
    >
      <ActionBar
        :selected="group"
        :selectionPrependText="courtClass + ' file/s'"
      >
        <template #default>
          <v-btn
            :prepend-icon="mdiFileDocumentOutline"
            style="letter-spacing: 0rem"
            density="compact"
            data-testid="view-case-details"
            @click="() => onViewApprCaseDetails(group)"
          >
            View case details
          </v-btn>
          <v-btn
            v-if="isCourtClassLabelCriminal(courtClass)"
            :prepend-icon="mdiFileDocumentMultipleOutline"
            style="letter-spacing: 0rem"
            density="compact"
            data-testid="view-key-documents"
            @click="() => onViewKeyDocuments(group, [])"
          >
            View key documents
          </v-btn>
          <v-btn
            v-if="isCourtClassLabelCriminal(courtClass)"
            :prepend-icon="mdiFileDocumentMultipleOutline"
            density="compact"
            style="letter-spacing: 0rem"
            data-testid="view-informations"
            @click="() => onViewInformations(group, ['INITIATING'])"
          >
            View Informations
          </v-btn>
          <v-btn
            v-else
            :prepend-icon="mdiFolderEyeOutline"
            style="letter-spacing: 0rem"
            density="compact"
            data-testid="view-judicial-binders"
            :disabled="binderLoading || totalBinderCount === 0"
            @click="() => onViewJudicialBinders(group)"
          >
            View judicial binder(s)&nbsp;
            <v-progress-circular
              v-if="binderLoading"
              indeterminate
              size="18"
              width="2"
              color="primary"
              class="mr-2 align-middle"
            />
            <span v-else-if="totalBinderCount > 0">
              ({{ totalBinderCount }} / {{ getCivilFiles(selected).length }})
            </span>
          </v-btn>
        </template>
      </ActionBar>
    </template>
  </div>
</template>

<script setup lang="ts">
  import ActionBar from '@/components/shared/table/ActionBar.vue';
  import { CourtListAppearance } from '@/types/courtlist';
  import { getCourtClassLabel, isCourtClassLabelCriminal } from '@/utils/utils';
  import {
    mdiFileDocumentMultipleOutline,
    mdiFileDocumentOutline,
    mdiFolderEyeOutline,
  } from '@mdi/js';
  import { computed, inject, ref, watch } from 'vue';
  import { BinderService } from '@/services';
  import { useCommonStore } from '@/stores';
  import { Binder } from '@/types/Binder';
  import { BinderRequest } from '@/types/DocumentBundleRequest';

  const props = defineProps<{
    selected: CourtListAppearance[];
  }>();

  const binderService = inject<BinderService>('binderService');
  const commonStore = useCommonStore();

  if (!binderService) {
    throw new Error('Service is undefined.');
  }

  const emit = defineEmits<{
    (e: 'view-case-details', appearances: CourtListAppearance[]): void;
    (
      e: 'view-key-documents',
      appearances: CourtListAppearance[],
      categories: string[]
    ): void;
    (
      e: 'view-informations',
      appearances: CourtListAppearance[],
      categories: string[]
    ): void;
    (e: 'unique-civil-file-selected', appearance: CourtListAppearance): void;
    (e: 'view-judicial-binders', binders: BinderRequest[]): void;
  }>();

  const previousCivilFileIds = new Set<string>();
  const fileBinders = ref<Record<string, Binder[]>>({});
  const binderRequests = ref<Promise<any>[]>([]);
  const binderLoading = computed(() => binderRequests.value.length > 0);

  const getCivilFiles = (items: CourtListAppearance[]) =>
    items.filter(
      (item) =>
        !isCourtClassLabelCriminal(getCourtClassLabel(item.courtClassCd)) &&
        item.physicalFileId
    );

  const fetchBinderCountForAppearance = async (
    appearance: CourtListAppearance
  ) => {
    const labels = {
      physicalFileId: appearance.physicalFileId,
      courtClassCd: appearance.courtClassCd,
      judgeId: commonStore.userInfo?.userId,
    };

    try {
      const request = binderService
        .getBinders(labels)
        .then((response) => {
          fileBinders.value[appearance.physicalFileId] =
            response.succeeded && response.payload ? response.payload : [];
        })
        .finally(() => {
          binderRequests.value = binderRequests.value.filter(
            (r) => r !== request
          );
        });
      binderRequests.value.push(request);
    } catch (error) {
      console.error('Error fetching binders:', error);
      fileBinders.value[appearance.physicalFileId] = [];
    }
  };

  const groupedSelections = computed(() => {
    const groups: Record<string, CourtListAppearance[]> = {};

    for (const item of props.selected) {
      const group = getCourtClassLabel(item.courtClassCd);
      if (!groups[group]) {
        groups[group] = [];
      }
      groups[group].push(item);
    }

    return groups;
  });

  // Watch for changes in selected civil/family files and fetch binder counts
  watch(
    () => props.selected,
    async (newSelected, oldSelected) => {
      const civilFiles = getCivilFiles(newSelected);
      const isSelection = newSelected.length > (oldSelected?.length || 0);

      if (civilFiles.length === 0) {
        fileBinders.value = {};
        previousCivilFileIds.clear();
        return;
      }

      if (!isSelection) {
        const currentFileIds = new Set(civilFiles.map((f) => f.physicalFileId));
        fileBinders.value = Object.fromEntries(
          Object.entries(fileBinders.value).filter(([fileId]) =>
            currentFileIds.has(fileId)
          )
        );

        // Update previousCivilFileIds to match current selection
        previousCivilFileIds.clear();
        currentFileIds.forEach((id) => previousCivilFileIds.add(id));
        return;
      }

      // Find newly added items
      const oldFileIds = new Set(
        getCivilFiles(oldSelected || []).map((f) => f.physicalFileId)
      );
      const newlySelected = civilFiles.filter(
        (f) => !oldFileIds.has(f.physicalFileId)
      );

      // Fetch for all new items
      for (const appearance of newlySelected) {
        if (!previousCivilFileIds.has(appearance.physicalFileId)) {
          emit('unique-civil-file-selected', appearance);
          previousCivilFileIds.add(appearance.physicalFileId);
          await fetchBinderCountForAppearance(appearance);
        }
      }
    },
    { immediate: true }
  );

  const totalBinderCount = computed(() => {
    return Object.values(fileBinders.value).reduce(
      (sum, binders) => sum + binders.length,
      0
    );
  });

  const onViewApprCaseDetails = (appearances: CourtListAppearance[]) => {
    emit('view-case-details', appearances);
  };
  const onViewKeyDocuments = (
    appearances: CourtListAppearance[],
    categories: string[]
  ) => {
    emit('view-key-documents', appearances, categories);
  };
  const onViewInformations = (
    appearances: CourtListAppearance[],
    categories: string[]
  ) => {
    emit('view-informations', appearances, categories);
  };
  const onViewJudicialBinders = (appearances: CourtListAppearance[]) => {
    const fileIds = new Set(appearances.map((a) => a.physicalFileId));

    const binderRequests: BinderRequest[] = Object.entries(fileBinders.value)
      .filter(([fileBinderId]) => fileIds.has(fileBinderId))
      .flatMap(([fileBinderId, binders]) => {
        const matchingAppearance = appearances.find(
          (appearance) => appearance.physicalFileId === fileBinderId
        );

        return binders.map((binder) => ({
          id: binder.id,
          labels: {
            courtClassCd: binder.labels.courtClassCd,
            isCriminal: binder.labels.isCriminal,
            judgeId: binder.labels.judgeId,
            physicalFileId: binder.labels.physicalFileId,
            participantId: binder.labels.participantId,
          },
          fileNumber:
            matchingAppearance?.aslCourtFileNumber ??
            matchingAppearance?.courtFileNumber,
          documents: binder.documents,
        }));
      });

    emit('view-judicial-binders', binderRequests);
  };
</script>
