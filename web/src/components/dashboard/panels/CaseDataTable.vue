<template>
  <v-data-table-virtual
    :headers="headers"
    :items="data"
    :sort-by="sortBy"
    :row-props="getRowProps"
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
        v-if="columns?.includes('dueDate') && saveDueDate && canEditDueDate?.(item)"
        class="d-flex align-start ga-2"
        @focusout="saveOnFocusOut(item, $event)"
      >
        <v-text-field
          :model-value="
            drafts[item.id] ?? displayDate(item.overrideDueDate ?? item.dueDate)
          "
          aria-label="Due date override"
          placeholder="DD-MMM-YYYY"
          density="compact"
          min-width="180"
          clearable
          :disabled="saving[item.id]"
          :loading="saving[item.id]"
          :error-messages="errors[item.id]"
          hide-details="auto"
          @update:model-value="drafts[item.id] = $event ?? ''"
          @update:focused="focused[item.id] = $event"
          @keydown.enter.prevent="saveOverride(item)"
          @keydown.esc.prevent="cancelOverride(item)"
        >
          <template #append-inner>
            <v-menu
              v-model="menus[item.id]"
              :close-on-content-click="false"
              @update:model-value="saveOnPickerClose(item, $event)"
            >
              <template #activator="{ props: activatorProps }">
                <v-btn
                  v-bind="activatorProps"
                  :icon="mdiCalendarMonthOutline"
                  aria-label="Choose due date"
                  variant="text"
                  size="x-small"
                  :disabled="saving[item.id]"
                  @mousedown.prevent
                  @keydown.enter.stop
                />
              </template>
              <v-date-picker
                :model-value="pickerDate(item)"
                hide-header
                @update:model-value="selectDate(item, $event)"
              />
            </v-menu>
          </template>
        </v-text-field>
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
  import { useSnackbarStore } from '@/stores/SnackbarStore';
  import { Case } from '@/types';
  import { DataTableHeader } from '@/types/shared';
  import {
    formatDateInstanceToDDMMMYYYY,
    formatDateInstanceToYYYYMMDD,
    formatDateToDDMMMYYYY,
    parseDDMMMYYYYToDate,
  } from '@/utils/dateUtils';
  import { getCourtClassLabel, getCourtClassStyle } from '@/utils/utils';
  import { mdiCalendarMonthOutline } from '@mdi/js';
  import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

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
    highlightReservedJudgments?: boolean;
  }>();

  const sortBy = ref(props.sortBy || [{ key: 'dueDate', order: 'asc' }]);
  const snackbarStore = useSnackbarStore();
  const drafts = ref<Record<string, string>>({});
  const errors = ref<Record<string, string>>({});
  const saving = ref<Record<string, boolean>>({});
  const menus = ref<Record<string, boolean>>({});
  const focused = ref<Record<string, boolean>>({});
  const today = ref(formatDateInstanceToYYYYMMDD(new Date()));
  let dateRefreshTimer: ReturnType<typeof setInterval> | undefined;
  let active = true;
  let generation = 0;

  onMounted(() => {
    if (props.highlightReservedJudgments) {
      dateRefreshTimer = setInterval(() => {
        today.value = formatDateInstanceToYYYYMMDD(new Date());
      }, 60_000);
    }
  });

  onBeforeUnmount(() => {
    active = false;
    clearInterval(dateRefreshTimer);
  });
  watch(
    () => props.data,
    () => {
      generation++;
      drafts.value = {};
      errors.value = {};
      saving.value = {};
      menus.value = {};
      focused.value = {};
    },
    { flush: 'sync' }
  );

  // A duplicate row may receive a new revision from another row's save.
  watch(
    () => props.data.map((item) => ({
      id: item.id,
      updatedDate: item.updatedDate,
      overrideDueDate: item.overrideDueDate,
      dueDate: item.dueDate,
      editable: !!props.canEditDueDate?.(item),
    })),
    (rows, previous) => {
      const previousRows = new Map(previous.map((row) => [row.id, row]));
      for (const row of rows) {
        const old = previousRows.get(row.id);
        if (
          old && (
            row.updatedDate !== old.updatedDate ||
            row.overrideDueDate !== old.overrideDueDate ||
            row.dueDate !== old.dueDate ||
            row.editable !== old.editable
          )
        ) {
          delete drafts.value[row.id];
          delete errors.value[row.id];
          delete menus.value[row.id];
        }
      }
    },
    { flush: 'sync' }
  );

  const inputDate = (value: string | null | undefined) => {
    if (!value) return '';
    if (/^\d{4}-\d{2}-\d{2}(?:$|[T ])/.test(value)) {
      const date = value.slice(0, 10);
      const parsed = parseDDMMMYYYYToDate(formatDateToDDMMMYYYY(date));
      return formatDateInstanceToYYYYMMDD(parsed) === date ? date : '';
    }
    return formatDateInstanceToYYYYMMDD(parseDDMMMYYYYToDate(value));
  };
  const displayDate = (value: string | null | undefined) =>
    formatDateToDDMMMYYYY(inputDate(value));

  const businessDaysUntil = (value: string | null | undefined) => {
    const date = inputDate(value);
    if (!date || date < today.value) return undefined;

    const dueDate = parseDDMMMYYYYToDate(formatDateToDDMMMYYYY(date));
    const cursor = parseDDMMMYYYYToDate(formatDateToDDMMMYYYY(today.value));
    if (!dueDate || !cursor) return undefined;

    let days = 0;
    // Count weekdays after today, stopping once the highlighting window is exceeded.
    while (cursor < dueDate && days <= 7) {
      cursor.setDate(cursor.getDate() + 1);
      if (cursor.getDay() !== 0 && cursor.getDay() !== 6) days++;
    }
    return days;
  };

  const getRowProps = ({ item }: { item: Case }) => {
    // The reserved judgments panel also contains scheduled decisions with a reason code.
    if (!props.highlightReservedJudgments || item.reason?.trim()) return {};

    const days = businessDaysUntil(item.overrideDueDate ?? item.dueDate);
    if (days === undefined || days > 7) return {};

    return { class: days <= 3 ? 'due-date-urgent' : 'due-date-warning' };
  };

  const parseDraft = (value: string): string | null | undefined => {
    const text = value.trim();
    if (!text) return null;
    if (!/^\d{2}-[a-z]{3}-\d{4}$/i.test(text)) return undefined;
    const normalized =
      text.slice(0, 3) + text[3].toUpperCase() +
      text.slice(4, 6).toLowerCase() + text.slice(6);
    const date = parseDDMMMYYYYToDate(normalized);
    return date ? formatDateInstanceToYYYYMMDD(date) : undefined;
  };

  const pickerDate = (item: Case) => {
    const value = drafts.value[item.id];
    const date = value === undefined
      ? displayDate(item.overrideDueDate ?? item.dueDate) : value;
    const parsed = parseDraft(date);
    return parsed ? parseDDMMMYYYYToDate(formatDateToDDMMMYYYY(parsed)) : null;
  };

  const selectDate = async (item: Case, date: unknown) => {
    if (saving.value[item.id]) return;
    const isoDate = date instanceof Date ? formatDateInstanceToYYYYMMDD(date) : '';
    if (!isoDate || inputDate(isoDate) !== isoDate) {
      errors.value[item.id] = 'Enter a valid date as DD-MMM-YYYY.';
      return;
    }
    drafts.value[item.id] = formatDateToDDMMMYYYY(isoDate);
    errors.value[item.id] = '';
    menus.value[item.id] = false;
    await saveOverride(item);
  };

  const saveOnFocusOut = (item: Case, event: FocusEvent) => {
    const editor = event.currentTarget as HTMLElement;
    const target = event.relatedTarget;
    if ((target instanceof Node && editor.contains(target)) || menus.value[item.id])
      return;
    return saveOverride(item);
  };

  const saveOnPickerClose = (item: Case, open: boolean) => {
    if (!open && !focused.value[item.id]) return saveOverride(item);
  };

  const cancelOverride = (item: Case) => {
    if (saving.value[item.id]) return;
    delete drafts.value[item.id];
    delete errors.value[item.id];
    menus.value[item.id] = false;
  };

  const saveOverride = async (item: Case) => {
    if (
      !props.saveDueDate ||
      !props.canEditDueDate?.(item) ||
      saving.value[item.id] ||
      drafts.value[item.id] === undefined
    )
      return;
    const date = parseDraft(drafts.value[item.id]);
    if (date === undefined) {
      errors.value[item.id] = 'Enter a valid date as DD-MMM-YYYY.';
      return;
    }
    if (date === (item.overrideDueDate ?? null)) {
      cancelOverride(item);
      return;
    }
    const version = generation;
    saving.value[item.id] = true;
    errors.value[item.id] = '';
    menus.value[item.id] = false;
    try {
      await props.saveDueDate(item, date);
      if (active && version === generation) delete drafts.value[item.id];
    } catch (error: any) {
      if (!active || version !== generation) return;
      // Restore the last persisted value and report the failure through the shared API error toast.
      delete drafts.value[item.id];
      const response = error?.originalError?.response ?? error?.response;
      const responseErrors = response?.data?.errors;
      const validationMessage = Array.isArray(responseErrors)
        ? responseErrors.join(' ')
        : responseErrors && typeof responseErrors === 'object'
          ? Object.values(responseErrors).flat().join(' ') : '';
      const messages: Record<number, string> = {
        400: 'The due date was rejected. Enter a valid date as DD-MMM-YYYY.',
        403: 'You no longer have permission to edit this due date.',
        404: 'This case is no longer available. Refresh before editing.',
        409: 'This case has changed. Refresh before editing its due date.',
        500: 'Unable to save the due date. Please try again.',
      };
      const message =
        validationMessage ||
        messages[response?.status] ||
        error?.message ||
        'Unable to save the due date. Please try again.';
      snackbarStore.showSnackbar(message, '#b84157', 'Error');
    } finally {
      if (active && version === generation) saving.value[item.id] = false;
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

<style scoped>
  :deep(tr.due-date-warning),
  :deep(tr.due-date-warning > td),
  :deep(tr.due-date-warning:hover),
  :deep(tr.due-date-warning:hover > td) {
    background-color: var(--bg-yellow-500);
  }

  :deep(tr.due-date-urgent),
  :deep(tr.due-date-urgent > td),
  :deep(tr.due-date-urgent:hover),
  :deep(tr.due-date-urgent:hover > td) {
    background-color: color-mix(in srgb, var(--bg-red-500) 15%, white);
  }
</style>
