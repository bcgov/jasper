<template>
  <v-container class="p-0">
    <v-skeleton-loader
      v-if="isLoading"
      type="date-picker"
      :loading="isLoading"
    ></v-skeleton-loader>
    <div v-else class="d-flex flex-column">
      <CourtToday v-if="todaySchedule" :today="todaySchedule" />
      <CalendarToolbar v-if="selectedDate" v-model="selectedDate" />
      <MyCalendar
        v-if="calendarData && selectedDate"
        :data="calendarData"
        :selectedDate
        :isLoading="isCalendarLoading"
      />
    </div>
  </v-container>
</template>
<script setup lang="ts">
  import { DashboardService, LocationService } from '@/services';
  import { CalendarDay } from '@/types';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import { inject, onMounted, ref, watch } from 'vue';
  import CalendarToolbar from './CalendarToolbar.vue';
  import CourtToday from './CourtToday.vue';
  import MyCalendar from './MyCalendar.vue';
  import { useCommonStore } from '@/stores';

  const commonStore = useCommonStore();
  const locationsService = inject<LocationService>('locationService');
  const dashboardService = inject<DashboardService>('dashboardService');

  if (!locationsService || !dashboardService) {
    throw new Error('Service is not available!');
  }

  const isLoading = ref(true);
  const isCalendarLoading = ref(true);
  const judgeId = ref(commonStore.userInfo?.judgeId);

  let currentCalendarDate = new Date('dd-mm-yyyy');

  const selectedDate = ref(new Date());
  let startDay = new Date(
    selectedDate.value.getFullYear(),
    selectedDate.value.getMonth(),
    1
  );
  let endDay = new Date(
    selectedDate.value.getFullYear(),
    selectedDate.value.getMonth() + 1,
    0
  );
  const todaySchedule = ref<CalendarDay>();
  const calendarData = ref<CalendarDay[]>([]);

  onMounted(async () => {
    isLoading.value = true;
    try {
      await Promise.all([loadCalendarData()]);
    } catch (error) {
      console.error('Retrieving data failed.', error);
    } finally {
      isLoading.value = false;
    }
  });

  watch(selectedDate, async (newVal) => {
    startDay = new Date(newVal.getFullYear(), newVal.getMonth(), 1);
    endDay = new Date(newVal.getFullYear(), newVal.getMonth() + 1, 0);

    await loadCalendarData();
  });

  watch(
    () => commonStore.userInfo?.judgeId,
    async (newVal: number) => {
      judgeId.value = newVal;
      await loadCalendarData();
    }
  );

  const loadCalendarData = async () => {
    isCalendarLoading.value = true;
    try {
      const { payload } = await dashboardService.getMySchedule(
        formatDateInstanceToDDMMMYYYY(startDay),
        formatDateInstanceToDDMMMYYYY(endDay),
        judgeId.value
      );
      todaySchedule.value = payload.today;
      calendarData.value = [...payload.days];
    } catch (error) {
      console.error('Failed to load calendar data:', error);
    } finally {
      isCalendarLoading.value = false;
    }
  };
</script>
