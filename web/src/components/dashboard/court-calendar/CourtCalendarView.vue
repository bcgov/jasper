ll
<template>
  <div>
    <v-skeleton-loader
      v-if="isLocationFilterLoading"
      data-testid="cc-filters-loader"
      type="list-item-avatar-two-line"
      :loading="isLocationFilterLoading"
    />
    <CourtCalendarFilters
      data-testid="cc-filters"
      v-if="locations.length > 0"
      v-model:selected-locations="selectedLocationIds"
      v-model:selected-presiders="selectedPresiderIds"
      v-model:selected-activity-class="selectedActivityClass"
      v-model:is-presiders-view="isPresidersView"
      :isLocationFilterLoading="isLocationFilterLoading"
      :locations="locations"
      :presiders="presiders"
      :judgeHomeLocationId="judgeHomeLocationId"
    />

    <v-skeleton-loader
      data-testid="cc-loader"
      v-if="isCalendarLoading"
      type="date-picker"
      :loading="isCalendarLoading"
    ></v-skeleton-loader>
    <CourtCalendar
      v-if="!isCalendarLoading && isPresidersView"
      ref="presidersViewRef"
      :calendar-view="calendarView"
      :events="presidersCalendarEvents"
      :selected-date="selectedDate"
    >
      <template #eventContent="{ event }">
        <CourtCalendarPresidersDay
          :activities="event.extendedProps.activities"
          :date="event.start"
        />
      </template>
    </CourtCalendar>
    <CourtCalendar
      v-if="!isCalendarLoading && !isPresidersView"
      ref="activitiesViewRef"
      :calendar-view="calendarView"
      :events="activitiesCalendarEvents"
      :selected-date="selectedDate"
    >
      <template #eventContent="{ event }">
        <CourtCalendarActivityDay
          :locations="event.extendedProps.locations"
          :date="event.start"
        />
      </template>
    </CourtCalendar>
  </div>
</template>
<script setup lang="ts">
  import { DashboardService, LocationService } from '@/services';
  import { useCommonStore } from '@/stores';
  import type { CourtCalendarDay } from '@/types';
  import { Activity, CalendarDay, Presider } from '@/types';
  import { CalendarViewEnum } from '@/types/common';
  import { LocationInfo } from '@/types/courtlist';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import { computed, inject, onMounted, ref, watch } from 'vue';
  import CourtCalendar from './CourtCalendar.vue';
  import CourtCalendarActivityDay from './CourtCalendarActivityDay.vue';
  import CourtCalendarPresidersDay from './CourtCalendarPresidersDay.vue';
  import CourtCalendarFilters from './filters/CourtCalendarFilters.vue';

  const presidersViewRef = ref<InstanceType<typeof CourtCalendar>>();
  const activitiesViewRef = ref<InstanceType<typeof CourtCalendar>>();

  const dashboardService = inject<DashboardService>('dashboardService');
  const locationService = inject<LocationService>('locationService');

  if (!dashboardService || !locationService) {
    throw new Error('Service is not available!');
  }

  const props = defineProps<{
    judgeId: number | undefined;
  }>();

  const selectedDate = defineModel<Date>('selectedDate');
  const calendarView = defineModel<string>('calendarView');
  const isCalendarLoading = defineModel<boolean>('isCalendarLoading');
  const isLocationFilterLoading = ref(true);
  const isPresidersView = ref(true);
  const commonStore = useCommonStore();

  if (!selectedDate.value) {
    throw new Error('selectedDate is required');
  }

  const judgeHomeLocationId =
    commonStore?.userInfo?.judgeHomeLocationId?.toString() || '';
  const presidersCalendarData = ref<CalendarDay[]>([]);
  const activitiesCalendarData = ref<CourtCalendarDay[]>([]);
  const locations = ref<LocationInfo[]>([]);
  const presiders = ref<Presider[]>([]);
  const activities = ref<Activity[]>([]);
  const selectedLocationIds = ref<string[]>([judgeHomeLocationId]);
  const selectedPresiderIds = ref<string[]>([]);
  const selectedActivityClass = ref<string>('all');
  const startDay = ref(new Date(selectedDate.value));
  const endDay = ref(new Date(selectedDate.value));
  const locationIds = computed(() => selectedLocationIds.value.join(','));

  const updateCalendar = async () => {
    if (!calendarView.value) {
      throw new Error('calendarView is required');
    }

    calculateDateRange(calendarView.value);
    await loadCalendarData();

    presidersViewRef.value?.changeView(calendarView.value);
    activitiesViewRef.value?.changeView(calendarView.value);
  };

  const loadCalendarData = async () => {
    isCalendarLoading.value = true;
    try {
      if (isPresidersView.value) {
        await loadPresidersCalendar();
      } else {
        await loadActivitiesCalendar();
      }
    } catch (error) {
      console.error('Failed to load calendar data:', error);
    } finally {
      isCalendarLoading.value = false;
    }
  };

  const loadPresidersCalendar = async () => {
    const { payload } = await dashboardService.getCourtCalendarPresiders(
      formatDateInstanceToDDMMMYYYY(startDay.value),
      formatDateInstanceToDDMMMYYYY(endDay.value),
      locationIds.value,
      props.judgeId
    );
    presidersCalendarData.value = [...payload.days];
    presiders.value = [...payload.presiders];
    activities.value = [...payload.activities];
  };

  const loadActivitiesCalendar = async () => {
    const { payload } = await dashboardService.getCourtCalendarActivities(
      formatDateInstanceToDDMMMYYYY(startDay.value),
      formatDateInstanceToDDMMMYYYY(endDay.value),
      locationIds.value
    );
    activitiesCalendarData.value = [...payload.days];
    activities.value = [...payload.activities];
  };

  const filteredPresidersCalendarData = computed(() =>
    presidersCalendarData.value.map((day) => ({
      ...day,
      activities: day.activities.filter(
        (a) =>
          (selectedPresiderIds.value.length === 0 ||
            selectedPresiderIds.value.includes(a.judgeId.toString())) &&
          (selectedActivityClass.value === 'all' ||
            a.activityClassCode === selectedActivityClass.value)
      ),
    }))
  );

  const presidersCalendarEvents = computed(() =>
    filteredPresidersCalendarData.value.map((d) => ({
      start: new Date(d.date),
      extendedProps: {
        ...d,
      } as Record<string, unknown>,
    }))
  );

  const filteredActivitiesCalendarData = computed(() =>
    activitiesCalendarData.value.map((day) => ({
      ...day,
      locations: day.locations,
    }))
  );

  const activitiesCalendarEvents = computed(() =>
    filteredActivitiesCalendarData.value.map((d) => ({
      start: new Date(d.date),
      extendedProps: {
        ...d,
      } as Record<string, unknown>,
    }))
  );

  onMounted(async () => {
    isCalendarLoading.value = true;
    await Promise.all([loadLocations(), updateCalendar()]);
  });

  watch(selectedDate, updateCalendar);

  watch(calendarView, updateCalendar);

  watch(isPresidersView, updateCalendar);

  watch(() => props.judgeId, updateCalendar);

  watch(
    selectedLocationIds,
    async () => {
      selectedPresiderIds.value = [];
      await updateCalendar();
    },
    { deep: true }
  );

  const calculateDateRange = (calendarView: string) => {
    if (!selectedDate.value) {
      throw new Error('selectedDate is required');
    }

    // Update the start and end days based on the calendar view
    switch (calendarView) {
      case CalendarViewEnum.MonthView: {
        // First and last day of the month
        startDay.value = new Date(
          selectedDate.value.getFullYear(),
          selectedDate.value.getMonth(),
          1
        );
        endDay.value = new Date(
          selectedDate.value.getFullYear(),
          selectedDate.value.getMonth() + 1,
          0
        );
        break;
      }
      case CalendarViewEnum.TwoWeekView: {
        // Two weeks starting from the first Sunday before the selected date
        const sunday = new Date(selectedDate.value);
        sunday.setDate(
          selectedDate.value.getDate() - selectedDate.value.getDay()
        );

        const twoWeeksLater = new Date(sunday);
        twoWeeksLater.setDate(sunday.getDate() + 13);

        startDay.value = sunday;
        endDay.value = twoWeeksLater;
        break;
      }
      case CalendarViewEnum.WeekView: {
        // One week starting from the first Sunday before the selected date
        const sunday = new Date(selectedDate.value);
        sunday.setDate(
          selectedDate.value.getDate() - selectedDate.value.getDay()
        );

        const saturday = new Date(sunday);
        saturday.setDate(sunday.getDate() + 6);

        startDay.value = sunday;
        endDay.value = saturday;
        break;
      }
    }
  };

  const loadLocations = async () => {
    try {
      isLocationFilterLoading.value = true;
      locations.value = await locationService.getLocations();
    } catch (error) {
      console.error('Failed to load locations:', error);
    } finally {
      isLocationFilterLoading.value = false;
    }
  };
</script>
<style scoped>
  :deep(.court-list) {
    margin-right: 4px;
  }

  /* Header Styles */
  :deep(.fc-col-header-cell-cushion),
  :deep(.fc-col-header-cell-cushion:hover) {
    color: var(--text-blue-800);
    font-size: 0.875rem;
    font-weight: normal;
    text-transform: uppercase !important;
    text-decoration: none;
  }

  :deep(.fc-event) {
    display: block;
  }

  /* Day Styles */
  :deep(.fc-daygrid-day-top) {
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
  }

  :deep(.fc-daygrid-day-number),
  :deep(.fc-daygrid-day-number:hover) {
    font-weight: bold;
    color: var(--text-blue-800);
    text-decoration: none;
  }

  :deep(.fc-daygrid-day-frame) {
    padding: 0.3125rem;
  }

  :deep(.fc-daygrid-day) {
    background-color: var(--bg-white-500) !important;
  }

  :deep(.fc-daygrid-day:hover) {
    background-color: var(--bg-blue-100) !important;
  }

  :deep(.fc-daygrid-dot-event:hover) {
    background-color: transparent;
  }

  /* Today Styles */
  :deep(.fc-day-today .fc-daygrid-day-frame) {
    position: relative;
    background-color: var(--bg-blue-50) !important;
  }

  :deep(.fc-day-today .fc-daygrid-day-frame)::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    height: 5px;
    width: 100%;
    background-color: var(--bg-blue-500);
  }

  /* Weekend Styles */
  :deep(.fc-day-sun .fc-daygrid-day-frame),
  :deep(.fc-day-sat .fc-daygrid-day-frame) {
    background-color: var(--bg-gray-400) !important;
  }

  :deep(.fc-day-sun .fc-daygrid-day-frame:hover),
  :deep(.fc-day-sat .fc-daygrid-day-frame:hover) {
    background-color: var(--bg-blue-100) !important;
  }
</style>
