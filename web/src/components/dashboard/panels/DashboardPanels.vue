<template>
  <v-skeleton-loader
    v-if="isLoading"
    :loading="isLoading"
    type="article"
  ></v-skeleton-loader>
  <div class="my-4 mx-2" v-else>
    <v-expansion-panels
      class="mb-3"
      bg-color="var(--bg-gray-500)"
      :flat="true"
      multiple
    >
      <!-- RJs and DECs -->
      <v-expansion-panel>
        <v-expansion-panel-title>
          <h5 class="m-0">
            Reserved judgments & decisions
            {{
              reservedJudgementCount > 0 ? `(${reservedJudgementCount})` : ''
            }}
          </h5>
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
              ]"
            />
          </v-skeleton-loader>
        </v-expansion-panel-text>
      </v-expansion-panel>
      <!-- Scheduled continuations -->
      <v-expansion-panel>
        <v-expansion-panel-title>
          <h5 class="m-0">
            Scheduled continuations
            {{
              scheduledContinuationsCount > 0
                ? `(${scheduledContinuationsCount})`
                : ''
            }}
          </h5>
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
      <!-- Other seized cases -->
      <v-expansion-panel>
        <v-expansion-panel-title>
          <h5 class="m-0">
            Other seized cases
            {{ othersCount > 0 ? `(${othersCount})` : '' }}
          </h5>
        </v-expansion-panel-title>
        <v-expansion-panel-text class="others-table">
          <v-skeleton-loader class="my-1" type="table" :loading="isLoading">
            <CaseDataTable
              :data="others"
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
      <!-- Future assigned -->
      <v-expansion-panel>
        <v-expansion-panel-title>
          <h5 class="m-0">
            Future assigned
            {{ futureAssignedCount > 0 ? `(${futureAssignedCount})` : '' }}
          </h5>
        </v-expansion-panel-title>
        <v-expansion-panel-text class="future-assigned-table">
          <v-skeleton-loader class="my-1" type="table" :loading="isLoading">
            <CaseDataTable
              :data="futureAssigned"
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
  const others = ref<Case[]>([]);
  const futureAssigned = ref<Case[]>([]);
  const reservedJudgementCount = computed(
    () => reservedJudgements.value.length
  );
  const scheduledContinuationsCount = computed(
    () => scheduledContinuations.value.length
  );
  const othersCount = computed(() => others.value.length);
  const futureAssignedCount = computed(() => futureAssigned.value.length);
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
      others.value = response.payload.others;
      futureAssigned.value = response.payload.futureAssigned;
    } catch (err) {
      reservedJudgements.value = [];
      scheduledContinuations.value = [];
      others.value = [];
      futureAssigned.value = [];
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
  :deep(.v-expansion-panel) {
    margin-top: 0;
    margin-bottom: 1rem;
  }

  .reserved-judgements-table,
  .scheduled-continuations-table,
  .others-table,
  .future-assigned-table {
    background-color: var(--bg-white-500) !important;
    max-height: 400px;
    overflow-y: auto;
  }
</style>
