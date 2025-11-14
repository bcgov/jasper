<template>
  <v-skeleton-loader
    v-if="isLoading"
    :loading="isLoading"
    type="article"
  ></v-skeleton-loader>
  <div class="my-4 mx-2" v-else>
    <v-expansion-panels class="mb-3" bg-color="var(--bg-gray-500)" :flat="true">
      <v-expansion-panel :value="RESERVED_JUDGEMENT">
        <v-expansion-panel-title>
          <h4 class="m-0">
            Reserved judgments & decisions
            {{
              reservedJudgementCount > 0 ? `(${reservedJudgementCount})` : ''
            }}
          </h4>
        </v-expansion-panel-title>
        <v-expansion-panel-text class="reserved-judgements-table">
          <v-skeleton-loader class="my-1" type="table" :loading="isLoading">
            <CaseDataTable
              :data="reservedJudgements"
              :view-case-details="viewCaseDetails"
              :columns="[
                'fileNumber',
                'styleOfCause',
                'division',
                'decisionDate',
                'reason',
                'lastAppearance',
                'dueDate',
                'caseAge',
              ]"
              :sort-by="[{ key: 'cc', order: 'desc' }]"
            />
          </v-skeleton-loader>
        </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>
    <v-expansion-panels bg-color="var(--bg-gray-500)" :flat="true">
      <v-expansion-panel :value="SCHEDULED_CONTINUATIONS">
        <v-expansion-panel-title>
          <h4 class="m-0">
            Scheduled continuations
            {{
              scheduledContinuationsCount > 0
                ? `(${scheduledContinuationsCount})`
                : ''
            }}
          </h4>
        </v-expansion-panel-title>
        <v-expansion-panel-text class="scheduled-continuations-table">
          <v-skeleton-loader class="my-1" type="table" :loading="isLoading">
            <CaseDataTable
              :data="scheduledContinuations"
              :view-case-details="viewCaseDetails"
              :columns="[
                'fileNumber',
                'styleOfCause',
                'division',
                'nextAppearance',
                'reason',
                'lastAppearance',
                'caseAge',
              ]"
            />
          </v-skeleton-loader>
        </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>
  </div>
</template>

<script setup lang="ts">
  import { CaseService } from '@/services';
  import { useCourtFileSearchStore } from '@/stores';
  import { AssignedCaseResponse, Case } from '@/types';
  import { ApiResponse } from '@/types/ApiResponse';
  import { KeyValueInfo } from '@/types/common';
  import { getCourtClassLabel, isCourtClassLabelCriminal } from '@/utils/utils';
  import { computed, inject, onMounted, ref, watch } from 'vue';
  import CaseDataTable from './CaseDataTable.vue';

  const isLoading = ref(true);
  const reservedJudgements = ref<Case[]>([]);
  const scheduledContinuations = ref<Case[]>([]);
  const RESERVED_JUDGEMENT = 'reserved-judgement';
  const SCHEDULED_CONTINUATIONS = 'scheduled-continuations';
  const reservedJudgementCount = computed(
    () => reservedJudgements.value.length
  );
  const scheduledContinuationsCount = computed(
    () => scheduledContinuations.value.length
  );
  const props = defineProps({
    judgeId: { type: Number, default: null },
  });
  const caseService = inject<CaseService>('caseService');
  if (!caseService) {
    throw new Error('Service(s) is undefined.');
  }
  const courtFileSearchStore = useCourtFileSearchStore();

  const fetchAssignedCases = async (judgeId: number | null) => {
    isLoading.value = true;
    try {
      const response: ApiResponse<AssignedCaseResponse> =
        await caseService.getAssignedCases({
          judgeId: judgeId ?? '',
        });
      reservedJudgements.value = response.payload.reservedJudgments;
      scheduledContinuations.value = response.payload.scheduledContinuations;
    } catch (err) {
      reservedJudgements.value = [];
      scheduledContinuations.value = [];
      console.error('Failed to load RJs or scheduled continuations:', err);
    } finally {
      isLoading.value = false;
    }
  };

  onMounted(() => {
    fetchAssignedCases(props.judgeId);
  });

  watch(
    () => props.judgeId,
    (newJudgeId) => {
      fetchAssignedCases(newJudgeId);
    }
  );

  const viewCaseDetails = (item: Case) => {
    const courtClassLabel = getCourtClassLabel(item.courtClass);
    const isCriminal = isCourtClassLabelCriminal(courtClassLabel);

    const caseDetailUrl = `/${isCriminal ? 'criminal-file' : 'civil-file'}/${item.physicalFileId}`;

    const files: KeyValueInfo[] = [
      {
        key: item.physicalFileId,
        value: item.courtFileNumber,
      },
    ];
    courtFileSearchStore.addFilesForViewing({
      searchCriteria: {},
      searchResults: [],
      files,
    });

    window.open(caseDetailUrl, '_blank');
  };
</script>

<style scoped>
  .reserved-judgements-table,
  .scheduled-continuations-table {
    background-color: var(--bg-white-500) !important;
    max-height: 400px;
    overflow-y: auto;
  }
</style>
