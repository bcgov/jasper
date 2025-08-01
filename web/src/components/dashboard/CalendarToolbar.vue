<template>
  <div class="d-flex justify-center my-3">
    <div class="other-calendars text-left">
      <a
        href="#"
        class="text-decoration-underline cursor-pointer"
        @click="isCourtCalendar = !isCourtCalendar"
        ><v-icon :icon="mdiChevronRight" />View other calendars</a
      >
    </div>
    <div class="d-flex align-center centered">
      <h3 data-testid="title" class="m-0">
        Schedule for {{ formatDateInstanceToMMMYYYY(selectedDate!) }}
      </h3>
      <MyCalendarActions
        v-if="!isCourtCalendar"
        v-model="selectedDate"
        :isCourtCalendar="isCourtCalendar!"
      />
      <CourtCalendarActions v-else="isCourtCalendar" v-model="selectedDate" />
    </div>
    <div class="more text-right">
      <a class="text-decoration-underline cursor-pointer"
        ><v-icon :icon="mdiDotsHorizontal" />More</a
      >
    </div>
  </div>
</template>
<script setup lang="ts">
  import { formatDateInstanceToMMMYYYY } from '@/utils/dateUtils';
  import { mdiChevronRight, mdiDotsHorizontal } from '@mdi/js';
  import CourtCalendarActions from './court-calendar/CourtCalendarActions.vue';
  import MyCalendarActions from './my-calendar/MyCalendarActions.vue';

  const selectedDate = defineModel<Date>('selectedDate');
  const isCourtCalendar = defineModel<boolean>('isCourtCalendar');
</script>
<style scoped>
  .other-calendars,
  .more {
    flex: 1;
    color: var(--text-blue-500);
  }

  .other-calendars:hover,
  .more:hover {
    color: var(--text-blue-800);
  }
</style>
