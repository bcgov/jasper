<template>
  <div class="d-flex flex-column calendar-day">
    <div
      class="mb-2"
      data-testid="activity-detail"
      v-for="{ locationShortName, activities, showVideo } in groupedActivities"
    >
      <div class="mb-1 d-flex justify-space-between">
        <span data-testid="short-name">{{ locationShortName }}</span>
        <v-icon
          data-testid="location-remote-icon"
          v-if="showVideo"
          :icon="mdiVideo"
        />
      </div>
      <div
        class="d-flex align-center justify-space-between mb-1"
        v-for="{
          period,
          activityClassDescription,
          activityDisplayCode,
          activityDescription,
          roomCode,
          isRemote,
          restrictions,
        } in activities"
      >
        <div class="d-flex align-center">
          <v-chip
            size="small"
            density="compact"
            class="period mr-1"
            v-if="period"
            >{{ period }}</v-chip
          >
          <div :class="cleanActivityClassDescription(activityClassDescription)">
            <span data-testid="activity">
              {{
                isWeekend
                  ? 'Weekend'
                  : (activityDisplayCode ?? activityDescription)
              }}
            </span>
            <span v-if="roomCode" data-testid="room"> ({{ roomCode }})</span>
          </div>
        </div>
        <div>
          <v-icon
            data-testid="activity-remote-icon"
            v-if="!showVideo && isRemote"
            :icon="mdiVideo"
          />
          <v-chip
            size="small"
            density="compact"
            class="ar ml-1"
            data-testid="activity-restrictions"
            v-if="restrictions && restrictions.length > 0"
            >{{ restrictions.length }}</v-chip
          >
        </div>
      </div>
    </div>
  </div>
</template>
<script setup lang="ts">
  import { CalendarDayActivity } from '@/types';
  import { mdiVideo } from '@mdi/js';
  import { computed } from 'vue';

  const props = defineProps<{
    isWeekend: boolean;
    activities: CalendarDayActivity[];
  }>();

  const cleanActivityClassDescription = (
    activityClassDescription: string
  ): string => {
    return activityClassDescription.trim().replace(/\s+/g, '-').toLowerCase();
  };

  const groupedActivities = computed(() => {
    const data = new Map<string, CalendarDayActivity[]>();

    for (const activity of props.activities) {
      if (!data.has(activity.locationShortName)) {
        data.set(activity.locationShortName, []);
      }
      data.get(activity.locationShortName)?.push(activity);
    }

    return Array.from(data.entries()).map(
      ([locationShortName, activities]) => ({
        locationShortName,
        showVideo: activities.every((a) => a.isRemote),
        activities,
      })
    );
  });
</script>
<style scoped>
  .calendar-day {
    color: var(--text-blue-800) !important;
  }

  .period {
    color: var(--text-white-500);
    background-color: var(--bg-blue-800);
  }

  .ar {
    background-color: var(--bg-blue-200);
  }
</style>
