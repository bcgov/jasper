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
            {{ uniqueJudgements > 0 ? `(${uniqueJudgements})` : '' }}
          </h5>
        </v-expansion-panel-title>
        <v-expansion-panel-text class="reserved-judgements-table">
          <v-skeleton-loader class="my-1" type="table" :loading="isLoading">
            <CaseDataTable
              :data="reservedJudgements"
              highlight-reserved-judgments
              :can-edit-due-date="canEditDueDate"
              :save-due-date="saveDueDate"
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
            {{ uniqueContinuations > 0 ? `(${uniqueContinuations})` : '' }}
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
            {{ uniqueOthers > 0 ? `(${uniqueOthers})` : '' }}
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
            {{ uniqueFutureAssigned > 0 ? `(${uniqueFutureAssigned})` : '' }}
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
  import { PERMISSIONS } from '@/constants/permissions';
  import { CaseService } from '@/services';
  import { useCommonStore, useCourtFileSearchStore } from '@/stores';
  import { AssignedCaseResponse, Case } from '@/types';
  import { ApiResponse } from '@/types/ApiResponse';
  import { KeyValueInfo } from '@/types/common';
  import { getCourtClassLabel, isCourtClassLabelCriminal } from '@/utils/utils';
  import { computed, inject, onBeforeUnmount, onMounted, ref, watch } from 'vue';
  import CaseDataTable from './CaseDataTable.vue';

  const isLoading = ref(true);
  const reservedJudgements = ref<Case[]>([]);
  const scheduledContinuations = ref<Case[]>([]);
  const others = ref<Case[]>([]);
  const futureAssigned = ref<Case[]>([]);
  const uniqueJudgements = computed(
    () => new Set(reservedJudgements.value.map((c) => c.fileNumber)).size
  );
  const uniqueContinuations = computed(
    () => new Set(scheduledContinuations.value.map((c) => c.fileNumber)).size
  );
  const uniqueOthers = computed(
    () => new Set(others.value.map((c) => c.fileNumber)).size
  );
  const uniqueFutureAssigned = computed(
    () => new Set(futureAssigned.value.map((c) => c.fileNumber)).size
  );
  const props = defineProps({
    judgeId: { type: Number, default: null },
  });
  const caseService = inject<CaseService>('caseService');
  if (!caseService) {
    throw new Error('Service(s) is undefined.');
  }
  const courtFileSearchStore = useCourtFileSearchStore();
  const commonStore = useCommonStore();
  const dueDateEditingJudgeId = ref<number | null>(null);
  const pendingGroups = new Set<string>();
  let active = true;
  let generation = 0;
  onBeforeUnmount(() => {
    active = false;
    generation++;
  });

  const canEditDueDate = (item: Case) => {
    const user = commonStore.loggedInUserInfo;
    return (
      !!user &&
      user.isActive === true &&
      !!user.permissions?.includes(PERMISSIONS.EDIT_RESERVED_JUDGMENTS_DUE_DATE) &&
      user.judgeId > 0 &&
      dueDateEditingJudgeId.value === user.judgeId &&
      item.judgeId === user.judgeId &&
      !!item.updatedDate &&
      !!item.physicalFileId?.trim() &&
      !!item.appearanceId?.trim() &&
      (!item.reason?.trim() ||
        (item.reason.toUpperCase() === 'DEC' &&
          item.restrictionCode?.toUpperCase() === 'S'))
    );
  };

  const saveDueDate = async (item: Case, date: string | null) => {
    if (
      !active || !canEditDueDate(item) ||
      !reservedJudgements.value.includes(item)
    )
      throw new Error('You cannot edit this due date. Refresh before trying again.');
    const key = JSON.stringify([
      item.judgeId, item.physicalFileId, item.appearanceId,
    ]);
    if (pendingGroups.has(key))
      throw new Error('A due date for this case is already being saved.');
    const version = generation;
    const selectedJudge = props.judgeId;
    pendingGroups.add(key);
    try {
      const response = await caseService.setDueDateOverride(item, date);
      if (!active || version !== generation || selectedJudge !== props.judgeId)
        return;
      if (!response.succeeded) throw new Error(response.errors.join(' '));
      for (const updated of response.payload) {
        const displayed = reservedJudgements.value.find(
          (row) => row.id === updated.id
        );
        if (displayed) {
          Object.assign(displayed, updated, {
            overrideDueDate: updated.overrideDueDate ?? null,
          });
        }
      }
    } finally {
      pendingGroups.delete(key);
    }
  };

  const fetchAssignedCases = async (judgeId: number | null) => {
    const version = ++generation;
    dueDateEditingJudgeId.value = null;
    isLoading.value = true;
    try {
      const response: ApiResponse<AssignedCaseResponse> =
        await caseService.getAssignedCases({
          judgeId: judgeId ?? '',
        });
      if (!active || version !== generation) return;
      if (!response.succeeded) throw new Error(response.errors.join(' '));
      dueDateEditingJudgeId.value = response.payload.dueDateEditingJudgeId ?? null;
      reservedJudgements.value = response.payload.reservedJudgments;
      scheduledContinuations.value = response.payload.scheduledContinuations;
      others.value = response.payload.others;
      futureAssigned.value = response.payload.futureAssigned;
    } catch (err) {
      if (!active || version !== generation) return;
      reservedJudgements.value = [];
      scheduledContinuations.value = [];
      others.value = [];
      futureAssigned.value = [];
      console.error('Failed to load RJs or scheduled continuations:', err);
    } finally {
      if (active && version === generation) isLoading.value = false;
    }
  };

  onMounted(() => {
    fetchAssignedCases(props.judgeId);
  });

  watch(
    () => props.judgeId,
    (newJudgeId) => {
      fetchAssignedCases(newJudgeId);
    },
    { flush: 'sync' }
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
