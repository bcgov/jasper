<template>
  <CourtToday :judgeId="judgeId" />
  <CourtCalendar
    v-if="isCourtCalendar"
    v-model:isCourtCalendar="isCourtCalendar"
  />
  <MyCalendar
    v-else
    :judgeId="judgeId"
    v-model:isCourtCalendar="isCourtCalendar"
  />
</template>
<script setup lang="ts">
  import { DashboardService } from '@/services';
  import { useCommonStore } from '@/stores';
  import { inject, ref, watch } from 'vue';
  import CourtCalendar from './court-calendar/CourtCalendar.vue';
  import CourtToday from './CourtToday.vue';
  import MyCalendar from './my-calendar/MyCalendar.vue';

  const commonStore = useCommonStore();
  const dashboardService = inject<DashboardService>('dashboardService');

  if (!dashboardService) {
    throw new Error('Service is not available!');
  }

  const judgeId = ref(commonStore.userInfo?.judgeId);
  const isCourtCalendar = ref(false);

  watch(
    () => commonStore.userInfo?.judgeId,
    async (newVal, _oldVal) => {
      judgeId.value = newVal;
    }
  );
</script>
