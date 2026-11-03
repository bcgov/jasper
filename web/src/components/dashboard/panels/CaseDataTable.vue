<template>
  <v-data-table-virtual
    :headers="headers"
    :items="data"
    :sort-by="sortBy"
    fixed-header
  >
    <template #[`item.courtClass`]="{ item }">
      <span :class="[getCourtClassStyle(item.courtClass)]">
        {{ getCourtClassLabel(item.courtClass) }}
      </span>
    </template>
    <template #[`item.styleOfCause`]="{ item }">
      <a href="#" @click.prevent="viewCaseDetails(item)">
        {{ item.styleOfCause }}
      </a>
    </template>
    <template #[`item.dueDate`]="{ item }">
      <div
        v-if="saveDueDate && canEditDueDate?.(item)"
        class="d-flex align-start ga-2"
      >
        <v-text-field
          :model-value="
            drafts[item.id] ?? inputDate(item.overrideDueDate ?? item.dueDate)
          "
          type="date"
          aria-label="Due date override"
          density="compact"
          min-width="180"
          clearable
          :disabled="saving[item.id]"
          :error-messages="errors[item.id]"
          hide-details="auto"
          @update:model-value="drafts[item.id] = $event ?? ''"
        />
        <v-btn
          size="small"
          :loading="saving[item.id]"
          :disabled="drafts[item.id] === undefined"
          @click="saveOverride(item)"
          >Save</v-btn
        >
      </div>
      <span v-else>{{
        displayDate(
          columns?.includes('dueDate')
            ? (item.overrideDueDate ?? item.dueDate)
            : item.dueDate
        )
      }}</span>
    </template>
  </v-data-table-virtual>
</template>

<script setup lang="ts">
  import { Case } from '@/types';
  import { DataTableHeader } from '@/types/shared';
  import {
    formatDateInstanceToDDMMMYYYY,
    formatDateInstanceToYYYYMMDD,
    formatDateToDDMMMYYYY,
    parseDDMMMYYYYToDate,
  } from '@/utils/dateUtils';
  import { getCourtClassLabel, getCourtClassStyle } from '@/utils/utils';
  import { computed, ref } from 'vue';

  const props = defineProps<{
    data: Case[];
    viewCaseDetails: (item: Case) => void;
    canEditDueDate?: (item: Case) => boolean;
    saveDueDate?: (item: Case, date: string | null) => Promise<void>;
    columns?: (
      | 'fileNumber'
      | 'styleOfCause'
      | 'division'
      | 'decisionDate'
      | 'nextAppearance'
      | 'reason'
      | 'lastAppearance'
      | 'dueDate'
      | 'caseAge'
    )[];
    sortBy?: { key: string; order: 'asc' | 'desc' }[];
  }>();

  const sortBy = ref(props.sortBy || [{ key: 'dueDate', order: 'asc' }]);
  const drafts = ref<Record<string, string>>({});
  const errors = ref<Record<string, string>>({});
  const saving = ref<Record<string, boolean>>({});

  const inputDate = (value: string | null | undefined) => {
    if (!value) return '';
    if (/^\d{4}-\d{2}-\d{2}/.test(value)) return value.slice(0, 10);
    return formatDateInstanceToYYYYMMDD(parseDDMMMYYYYToDate(value));
  };
  const displayDate = (value: string | null | undefined) =>
    formatDateToDDMMMYYYY(inputDate(value));

  const saveOverride = async (item: Case) => {
    if (
      !props.saveDueDate ||
      !props.canEditDueDate?.(item) ||
      saving.value[item.id]
    )
      return;
    saving.value[item.id] = true;
    errors.value[item.id] = '';
    try {
      await props.saveDueDate(item, drafts.value[item.id] || null);
      delete drafts.value[item.id];
    } catch (error: any) {
      errors.value[item.id] =
        error?.originalError?.response?.data?.errors?.join(' ') ||
        error?.message ||
        'Unable to save the due date. Please try again.';
    } finally {
      saving.value[item.id] = false;
    }
  };

  const allColumns: Record<string, DataTableHeader> = {
    fileNumber: {
      title: 'FILE #',
      key: 'courtFileNumber',
    },
    styleOfCause: {
      title: 'ACCUSED / PARTIES',
      key: 'styleOfCause',
    },
    division: {
      title: 'DIVISION',
      key: 'courtClass',
    },
    decisionDate: {
      title: 'DECISION DATE',
      key: 'decisionDate',
      value: (item: Case) => displayDate(item.dueDate),
      sort: (a: string, b: string) =>
        (parseDDMMMYYYYToDate(a)?.getTime() ?? 0) -
        (parseDDMMMYYYYToDate(b)?.getTime() ?? 0),
    },
    nextAppearance: {
      title: 'NEXT APPEARANCE',
      key: 'dueDate',
      value: (item: Case) =>
        formatDateInstanceToDDMMMYYYY(new Date(item.dueDate ?? '')),
      sort: (a: string, b: string) =>
        new Date(a).getTime() - new Date(b).getTime(),
    },
    reason: {
      title: 'REASON',
      key: 'reason',
    },
    lastAppearance: {
      title: 'LAST APPEARANCE',
      key: 'appearanceDate',
      value: (item: Case) =>
        formatDateInstanceToDDMMMYYYY(new Date(item.appearanceDate)),
      sort: (a: string, b: string) =>
        new Date(a).getTime() - new Date(b).getTime(),
    },
    dueDate: {
      title: 'DUE DATE',
      key: 'dueDate',
      value: (item: Case) => displayDate(item.overrideDueDate ?? item.dueDate),
      sort: (a: string, b: string) =>
        (parseDDMMMYYYYToDate(a)?.getTime() ?? 0) -
        (parseDDMMMYYYYToDate(b)?.getTime() ?? 0),
    },
    caseAge: {
      title: 'CASE AGE (days)',
      key: 'ageInDays',
      value: (item: Case) =>
        item.ageInDays === 0 ? '' : item.ageInDays.toString(),
    },
  };

  const headers = computed<DataTableHeader[]>(() => {
    const columnKeys = props.columns || [
      'fileNumber',
      'styleOfCause',
      'division',
      'nextAppearance',
      'reason',
      'lastAppearance',
      'caseAge',
    ];
    return columnKeys.map((key) => allColumns[key]);
  });
</script>
