<template>
  <CourtToday :judgeId="judgeId" v-if="!isCourtCalendar" />
  <CalendarToolbar
    v-model:selectedDate="selectedDate"
    v-model:isCourtCalendar="isCourtCalendar"
    v-model:calendarView="calendarView"
  />
  <CourtCalendar
    v-if="isCourtCalendar"
    v-model:selectedDate="selectedDate"
    v-model:calendarView="calendarView"
    :judgeId="judgeId"
  />
  <MyCalendar v-else :judgeId="judgeId" v-model:selectedDate="selectedDate" />
</template>
<script setup lang="ts">
  import { useCommonStore } from '@/stores';
  import { CalendarViewEnum } from '@/types/common';
  import { ref, watch } from 'vue';
  import CalendarToolbar from './CalendarToolbar.vue';
  import CourtCalendar from './court-calendar/CourtCalendar.vue';
  import CourtToday from './CourtToday.vue';
  import MyCalendar from './my-calendar/MyCalendar.vue';

  const commonStore = useCommonStore();
  const judgeId = ref(commonStore.userInfo?.judgeId);
  const isCourtCalendar = ref(false);
  const selectedDate = ref(new Date());
  const calendarView = ref(CalendarViewEnum.MonthView);

  watch(
    () => commonStore.userInfo?.judgeId,
    async (newVal, _oldVal) => (judgeId.value = newVal)
  );

  watch(isCourtCalendar, (newVal) => {
    calendarView.value = newVal
      ? CalendarViewEnum.TwoWeekView
      : CalendarViewEnum.MonthView;
    selectedDate.value = new Date();
  });
</script>
