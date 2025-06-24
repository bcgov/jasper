<template>
  <div class="d-flex flex-column calendar-day">
    <div
      class="mb-2"
      data-testid="activity-detail"
      v-for="{
        locationShortName,
        roomCode,
        period,
        isRemote,
        activityDescription,
        activityClassDescription,
        activityDisplayCode,
      } in activities"
    >
      <div class="mb-1" data-testid="short-name">{{ locationShortName }}</div>
      <div class="d-flex">
        <v-chip density="compact" class="p-0" v-if="period">{{
          period
        }}</v-chip>
        <div :class="cleanActivityClassDescription(activityClassDescription)">
          <span data-testid="activity">
            {{ activityDisplayCode ?? activityDescription }}
          </span>
          <span v-if="roomCode" data-testid="room"> ({{ roomCode }})</span>
        </div>
        <v-icon class="ml-1" v-if="isRemote" :icon="mdiVideo" />
      </div>
    </div>
  </div>
</template>
<script setup lang="ts">
  import { CalendarDayActivity } from '@/types';
  import { mdiVideo } from '@mdi/js';

  const props = defineProps<{ activities: CalendarDayActivity[] }>();

  const cleanActivityClassDescription = (
    activityClassDescription: string
  ): string => {
    return activityClassDescription.trim().replace(/\s+/g, '-').toLowerCase();
  };
</script>
<style scoped>
  .calendar-day {
    color: var(--text-blue-800) !important;
  }
  .civil {
    color: var(--text-purple-500);
  }
  .criminal {
    color: var(--text-blue-600);
  }
  .family {
    color: var(--text-green-500);
  }
  .mixed {
    color: var(--text-orange-500);
  }
  .non-sitting {
    color: var(--text-gray-400);
  }
  .sitting,
  .judge-sitting {
    color: var(--text-red-500);
  }
  .specialty {
    color: var(--text-blue-900);
  }
</style>
