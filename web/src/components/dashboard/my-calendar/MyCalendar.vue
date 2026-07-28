<template>
  <v-skeleton-loader
    v-if="isCalendarLoading"
    type="date-picker"
    :loading="isCalendarLoading"
  ></v-skeleton-loader>
  <FullCalendar class="mx-2" v-else :options="calendarOptions">
    <template v-slot:eventContent="{ event }">
      <MyCalendarDay
        :date="event.extendedProps.date"
        :isWeekend="event.extendedProps.isWeekend"
        :activities="event.extendedProps.activities"
      />
    </template>
    <template v-slot:dayCellTopContent="{ date, dayNumberText }">
      <span>{{ dayNumberText }}</span>
      <RouterLink
        v-if="showCourtList(date)"
        class="court-list"
        :to="{
          name: 'CourtList',
          query: { date: formatDateInstanceToDDMMMYYYY(date) },
        }"
        title="View Court List"
      >
        <v-icon :icon="mdiListBoxOutline" size="18" />
      </RouterLink>
    </template>
  </FullCalendar>
  <!--
    This component is teleported into a specific calendar cell so
    it appears inside or over that day, even though it's rendered
    outside FullCalendar. Only events with activities will have
    the expanded panel.
  -->
  <template v-for="day in calendarEventsWithActivities">
    <MyCalendarDayExpanded
      :expandedDate
      :day="day"
      :close="closeExpandedPanel"
    />
  </template>
</template>
<script setup lang="ts">
  import { useAutoRefresh } from '@/composables/useAutoRefresh';
  import { DashboardService } from '@/services';
  import { CalendarDay } from '@/types';
  import { ActivityClassEnum } from '@/types/common';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import FullCalendar, {
    CalendarOptions,
    useCalendarController,
    type DateClickInfo,
    type DayCellInfo,
    type EventClickInfo,
    type MountInfo,
  } from '@fullcalendar/vue3';
  import dayGridPlugin from '@fullcalendar/vue3/daygrid';
  import interactionPlugin from '@fullcalendar/vue3/interaction';
  import classicThemePlugin from '@fullcalendar/vue3/themes/classic';
  import { mdiListBoxOutline } from '@mdi/js';
  import { computed, inject, onMounted, onUnmounted, ref, watch } from 'vue';

  const dashboardService = inject<DashboardService>('dashboardService');

  if (!dashboardService) {
    throw new Error('Service is not available!');
  }

  const props = defineProps<{
    judgeId: number | undefined;
    baseCalendarOptions: CalendarOptions;
  }>();

  const selectedDate = defineModel<Date>('selectedDate')!;
  const isCalendarLoading = defineModel<boolean>('isCalendarLoading');

  if (!selectedDate.value) {
    throw new Error('selectedDate is required');
  }

  const calendarData = ref<CalendarDay[]>([]);
  const expandedDate = ref<string | null>(null);
  const controller = useCalendarController();
  const { setupAutoRefresh } = useAutoRefresh(
    () => !!selectedDate.value,
    () => loadCalendarData(),
    () => !!isCalendarLoading.value
  );

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

  onMounted(async () => {
    globalThis.addEventListener('online', handleOnline);
    await loadCalendarData();
  });

  onUnmounted(() => {
    globalThis.removeEventListener('online', handleOnline);
  });

  const handleOnline = async () => {
    if (isCalendarLoading.value || !selectedDate.value) {
      return;
    }

    isCalendarLoading.value = false;
    await loadCalendarData();
  };

  watch(selectedDate, async (newDate) => {
    if (newDate) {
      startDay = new Date(newDate.getFullYear(), newDate.getMonth(), 1);
      endDay = new Date(newDate.getFullYear(), newDate.getMonth() + 1, 0);
      controller.gotoDate(newDate);
    }
    await loadCalendarData();
  });

  watch(
    () => props.judgeId,
    async () => {
      await loadCalendarData();
    }
  );

  const loadCalendarData = async () => {
    isCalendarLoading.value = true;
    try {
      const { payload } = await dashboardService.getMySchedule(
        formatDateInstanceToDDMMMYYYY(startDay),
        formatDateInstanceToDDMMMYYYY(endDay),
        props.judgeId
      );
      calendarData.value = [...payload];
    } catch (error) {
      console.error('Failed to load calendar data:', error);
    } finally {
      isCalendarLoading.value = false;
      setupAutoRefresh();
    }
  };

  const calendarEvents = computed(() =>
    calendarData.value.map((d) => ({
      start: new Date(d.date),
      extendedProps: {
        ...d,
      } as CalendarDay,
    }))
  );

  const hasExpandableActivities = (activities: CalendarDay['activities']) =>
    // Days with activities that are not just Sitting or NonSitting, or have any restrictions, will be expandable.
    activities.some(
      (a) =>
        ![ActivityClassEnum.Sitting, ActivityClassEnum.NonSitting].includes(
          a.activityClassCode as ActivityClassEnum
        ) || a.restrictions.length > 0
    );

  const calendarEventsWithActivities = computed(() =>
    calendarData.value.filter((d) => hasExpandableActivities(d.activities))
  );

  const showCourtList = (date: Date) => {
    const formatted = formatDateInstanceToDDMMMYYYY(date);
    const data = calendarData.value.find((d) => d.date === formatted);
    return !!data && data.activities.length > 0 && data.showCourtList;
  };

  const toggleExpandedPanel = (date: string) => {
    const data = calendarData.value.find((d) => d.date === date);
    if (!data || !hasExpandableActivities(data.activities)) {
      return;
    }
    expandedDate.value = expandedDate.value === date ? null : date;
  };

  const handleDateClick = (info: DateClickInfo) => {
    toggleExpandedPanel(formatDateInstanceToDDMMMYYYY(info.date));
  };

  const handleEventClick = (info: EventClickInfo) => {
    toggleExpandedPanel(info.event.extendedProps.date);
  };

  const dayCellDidMount = (info: MountInfo<DayCellInfo>) => {
    const date = formatDateInstanceToDDMMMYYYY(info.date);
    const data = calendarData.value.find((d) => d.date === date);
    if (!data || !hasExpandableActivities(data.activities)) {
      return;
    }

    const wrapper = document.createElement('div');
    wrapper.classList.add('fc-expand-wrapper');
    wrapper.dataset.date = date;

    info.el.classList.add('cursor-pointer');
    info.el.prepend(wrapper);
  };

  const calendarOptions = computed<CalendarOptions>(() => ({
    ...props.baseCalendarOptions,
    controller,
    initialView: 'dayGridMonth',
    initialDate: selectedDate.value,
    plugins: [classicThemePlugin, dayGridPlugin, interactionPlugin],
    events: calendarEvents.value,
    dayCellDidMount: dayCellDidMount,
    dateClick: handleDateClick,
    eventClick: handleEventClick,
  }));

  const closeExpandedPanel = (e: MouseEvent) => {
    // Determine whether the expanded panel is going to be closed.
    // Expanded panel should only close when the click is from a date
    // without an activitiy (e.g. Weekend, Non-sitting, Sitting).
    const target = e.target as HTMLElement;
    if (!target) {
      expandedDate.value = null;
      return;
    }

    // Find the nearest Calendar cell
    const dayGridCell = target.closest('.day-cell');
    if (!dayGridCell) {
      expandedDate.value = null;
      return;
    }

    // Traverse down and retrieve the element that has a data-formatted-date attr
    const dateEl = dayGridCell.querySelector(
      '[data-formatted-date]'
    ) as HTMLElement | null;
    if (!dateEl || !dateEl.dataset.formattedDate) {
      expandedDate.value = null;
      return;
    }

    // If the date is in the calendarEventsWithActivities,
    // then the click happened on a cell that has an expanded panel.
    // If not found, we can safely close the panel.
    const date = dateEl.dataset.formattedDate;
    const hasActivity = calendarEventsWithActivities.value.some(
      (e) => e.date === date
    );
    if (!hasActivity) {
      expandedDate.value = null;
    }
  };
</script>
<style scoped>
  .court-list {
    margin-left: auto;
    color: var(--text-blue-800);
  }

  :deep(.fc-event) {
    display: block;
  }
</style>
