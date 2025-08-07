<template>
  <div class="d-flex justify-center align-center my-3">
    <div class="calendar-toggle text-left">
      <a
        href="#"
        data-testid="calendar-toggle-link"
        class="text-decoration-underline cursor-pointer"
        @click="isCourtCalendar = !isCourtCalendar"
      >
        <v-icon :icon="isCourtCalendar ? mdiChevronLeft : mdiChevronRight" />
        {{ isCourtCalendar ? 'View My Calendar' : 'View Other Calendars' }}
      </a>
    </div>
    <div class="d-flex align-center centered">
      <h3 data-testid="title" class="m-0">
        Schedule for {{ formatDateInstanceToMMMYYYY(selectedDate!) }}
      </h3>
      <div class="d-flex align-center centered">
        <!-- Month picker for My Calendar -->
        <v-menu
          v-if="!isCourtCalendar"
          data-testid="month-picker"
          location="bottom end"
          class="mr-3"
        >
          <template v-slot:activator="{ props }">
            <v-icon :icon="mdiChevronDown" v-bind="props" class="mr-3" />
          </template>
          <div class="d-flex flex-column month-picker">
            <div class="d-flex flex-row justify-center mt-3">
              <v-icon
                :icon="mdiChevronLeft"
                data-testid="previous-button"
                @click.stop="previousYear"
              />
              <span class="mx-3">{{ selectedYear }}</span>
              <v-icon
                :icon="mdiChevronRight"
                @click.stop="nextYear"
                data-testid="next-button"
              />
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
        <!-- Date Picker for Court Calendar -->
        <v-date-input
          v-else
          data-testid="date-picker"
          class="cc-date-picker mx-3"
          hide-details
          label=""
          density="compact"
          v-model="selectedDate"
        >
          <template v-slot:default="">
            <v-icon :icon="mdiCalendarMonthOutline" />
          </template>
        </v-date-input>
        <v-btn-secondary
          text="Today"
          size="large"
          class="mr-3"
          data-testid="today-button"
          @click="today"
          density="comfortable"
        ></v-btn-secondary>
        <!-- Calendar View Picker for Court Calendar -->
        <v-menu
          v-if="isCourtCalendar"
          data-testid="calendar-view-picker"
          v-model="menuOpen"
          location="bottom end"
        >
          <template #activator="{ props }">
            <v-btn v-bind="props" variant="outlined" density="default">
              <component
                class="mr-2"
                :is="currentOption?.icon"
                width="24"
                height="24"
              />
              <span>{{ currentOption?.label }}</span>
              <v-icon :icon="mdiChevronDown" v-bind="props" />
            </v-btn>
          </template>

          <v-list>
            <v-list-item
              v-for="(option, i) in options"
              :key="i"
              @click="selectOption(option)"
            >
              <v-list-item-title class="d-flex align-center">
                <component
                  :is="option.icon"
                  class="mr-2"
                  width="24"
                  height="24"
                />
                {{ option.label }}
              </v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>
      </div>
    </div>
    <div class="more text-right">
      <a class="text-decoration-underline cursor-pointer"
        ><v-icon :icon="mdiDotsHorizontal" />More</a
      >
    </div>
  </div>
</template>
<script setup lang="ts">
  import MonthIcon from '@/assets/month.svg';
  import OneWeekIcon from '@/assets/one-week.svg';
  import TwoWeekIcon from '@/assets/two-week.svg';
  import { CalendarViewEnum } from '@/types/common';
  import { formatDateInstanceToMMMYYYY } from '@/utils/dateUtils';
  import {
    mdiCalendarMonthOutline,
    mdiChevronDown,
    mdiChevronLeft,
    mdiChevronRight,
    mdiDotsHorizontal,
  } from '@mdi/js';
  import { computed, ref } from 'vue';

  const selectedDate = defineModel<Date>('selectedDate');
  const isCourtCalendar = defineModel<boolean>('isCourtCalendar');
  const calendarView = defineModel<string>('calendarView');

  const viewMode = ref<'months' | 'year' | 'month' | undefined>('months');
  const selectedYear = ref(
    selectedDate.value
      ? selectedDate.value.getFullYear()
      : new Date().getFullYear()
  );
  const selectedMonth = ref(
    selectedDate.value ? selectedDate.value.getMonth() : new Date().getMonth()
  );

  const options = [
    { label: 'Month', value: CalendarViewEnum.MonthView, icon: MonthIcon },
    { label: '2 Week', value: CalendarViewEnum.TwoWeekView, icon: TwoWeekIcon },
    { label: 'Week', value: CalendarViewEnum.WeekView, icon: OneWeekIcon },
  ];

  const currentOption = computed(() =>
    options.find((o) => o.value === calendarView.value)
  );

  const menuOpen = ref(false);

  const selectOption = (option) => {
    calendarView.value = option.value;
    menuOpen.value = false;
  };

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
</script>
<style scoped>
  /* Common Styles*/
  .calendar-toggle,
  .more {
    flex: 1;
    color: var(--text-blue-500);
  }

  .calendar-toggle:hover,
  .more:hover {
    color: var(--text-blue-800);
  }

  /* My Schedule Controls Styles */
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

  /* Court Calendar Controls Styles */
  :deep(.cc-date-picker .v-field__field) {
    width: 175px !important;
  }

  :deep(.cc-date-picker .v-input__prepend) {
    display: none;
  }

  .two-week-svg {
    width: 22px;
    height: 22px;
  }
</style>
