<template>
  <div class="d-flex align-center centered">
    <!-- Month Picker -->
    <h3 data-testid="title" class="m-0">Schedule for {{ currentDate }}</h3>
    <v-menu location="bottom end">
      <template v-slot:activator="{ props }">
        <v-icon :icon="mdiChevronDown" v-bind="props" />
      </template>
      <div class="d-flex flex-column month-picker">
        <div class="d-flex flex-row justify-center mt-3">
          <v-icon :icon="mdiChevronLeft" @click.stop="previousYear" />
          <span class="mx-3">{{ selectedYear }}</span>
          <v-icon :icon="mdiChevronRight" @click.stop="nextYear" />
        </div>
        <v-date-picker
          :view-mode="viewMode"
          type="month"
          v-model="selectedDate"
          @update:view-mode="updateViewMode"
          @update:month="updateMonth"
          hide-header
        />
      </div>
    </v-menu>
    <v-btn-secondary
      text="Today"
      size="large"
      class="ml-3"
      @click="today"
    ></v-btn-secondary>
  </div>
</template>
<script setup lang="ts">
  import { formatDateInstanceToMMMYYYY } from '@/utils/dateUtils';
  import { computed, ref } from 'vue';

  import { mdiChevronDown, mdiChevronLeft, mdiChevronRight } from '@mdi/js';

  const selectedDate = defineModel<Date>();

  const viewMode = ref<'months' | 'year' | 'month' | undefined>('months');
  const selectedYear = ref(
    selectedDate.value
      ? selectedDate.value.getFullYear()
      : new Date().getFullYear()
  );
  const selectedMonth = ref(
    selectedDate.value ? selectedDate.value.getMonth() : new Date().getMonth()
  );

  const updateViewMode = (mode) => {
    viewMode.value = mode === 'year' ? 'year' : 'months';
  };

  const previousYear = () => {
    selectedYear.value--;
    selectedDate.value = new Date(selectedYear.value, selectedMonth.value, 1);
  };

  const nextYear = () => {
    selectedYear.value++;
    selectedDate.value = new Date(selectedYear.value, selectedMonth.value, 1);
  };

  const updateMonth = (month) => {
    selectedMonth.value = month;
    selectedDate.value = new Date(selectedYear.value, month, 1);
  };

  const today = () => {
    const currentDate = new Date();
    selectedYear.value = currentDate.getFullYear();
    selectedMonth.value = currentDate.getMonth();
    selectedDate.value = currentDate;
  };

  const currentDate = computed(() => {
    return selectedYear.value !== undefined && selectedMonth.value !== undefined
      ? formatDateInstanceToMMMYYYY(
          new Date(selectedYear.value, selectedMonth.value)
        )
      : null;
  });
</script>
<style scoped>
  :deep(.v-picker__body .v-date-picker-controls) {
    display: none;
  }

  :deep(.v-date-picker-months) {
    height: 10rem;
  }

  :deep(.v-date-picker-months__content) {
    border-bottom-left-radius: 1.25rem;
    border-bottom-right-radius: 1.25rem;
    box-shadow: 0px 0px 5px 0px rgba(0, 0, 0, 0.2);
    grid-template-columns: repeat(4, 1fr);
    grid-gap: 0;
    padding-inline-start: 0;
    padding-inline-end: 0;
  }

  :deep(.v-date-picker-months__content .v-btn) {
    border-radius: 0px;
    background-color: var(--bg-gray-200);
    margin: 0.5rem;
  }

  :deep(.v-date-picker-months__content .v-btn:hover) {
    border-radius: 0px;
    font-weight: bold;
    margin: 0.5rem;
  }

  :deep(.v-date-picker-months__content .v-btn--active .v-btn__overlay) {
    background-color: var(--bg-white-500) !important;
  }

  :deep(.v-date-picker-months__content .v-btn--active) {
    border: 1px solid var(--border-blue-500);
    border-radius: 0px;
    font-weight: bold;
  }

  :deep(.v-date-picker-months__content .v-btn__content) {
    text-transform: uppercase;
  }

  .month-picker {
    box-shadow: 0px 0px 5px 0px rgba(0, 0, 0, 0.2);
    border-radius: 1.25rem;
    background-color: var(--bg-white-500);
  }
</style>
