<template>
  <div class="d-flex justify-center my-3">
    <div class="my-calendar text-left">
      <a
        href="#"
        data-testid="my-calendar-link"
        class="text-decoration-underline cursor-pointer"
        @click="isCourtCalendar = !isCourtCalendar"
        ><v-icon :icon="mdiChevronLeft" />View My Calendar</a
      >
    </div>
    <div class="d-flex align-center centered">
      <h3 data-testid="title" class="m-0">
        Schedule for {{ formatDateInstanceToMMMYYYY(selectedDate!) }}
      </h3>
      <v-date-input
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
        @click="today"
        density="comfortable"
      ></v-btn-secondary>
      <v-menu v-model="menuOpen" location="bottom end">
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
  import { formatDateInstanceToMMMYYYY } from '@/utils/dateUtils';
  import {
    mdiCalendarMonthOutline,
    mdiChevronDown,
    mdiChevronLeft,
    mdiDotsHorizontal,
  } from '@mdi/js';
  import { computed, ref } from 'vue';

  const selectedDate = defineModel<Date>('selectedDate');
  const isCourtCalendar = defineModel<boolean>('isCourtCalendar');
  const calendarView = defineModel<string>('calendarView');
  const options = [
    { label: 'Month', value: 'dayGridMonth', icon: MonthIcon },
    { label: '2 Week', value: 'dayGridTwoWeek', icon: TwoWeekIcon },
    { label: 'Week', value: 'dayGridWeek', icon: OneWeekIcon },
  ];

  const currentOption = computed(() =>
    options.find((o) => o.value === calendarView.value)
  );

  const menuOpen = ref(false);

  const selectOption = (option) => {
    calendarView.value = option.value;
    menuOpen.value = false;
  };

  const today = () => {
    const currentDate = new Date();
    selectedDate.value = currentDate;
  };
</script>
<style scoped>
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

  /* Toolbar Styles*/
  .my-calendar,
  .more {
    flex: 1;
    color: var(--text-blue-500);
  }

  .my-calendar:hover,
  .more:hover {
    color: var(--text-blue-800);
  }
</style>
