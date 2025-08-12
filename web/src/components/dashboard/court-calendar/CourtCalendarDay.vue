<template>
  <div class="d-flex flex-column calendar-day">
    <div
      class="mb-2"
      data-testid="activity-detail"
      v-for="[locationName, judgeActivities] in groupedData"
    >
      <b
        ><span data-testid="short-name">{{ locationName }}</span></b
      >

      <div
        class="d-flex justify-space-between border-b mb-1"
        v-for="[judgeInitials, activities] in judgeActivities"
      >
        <span data-testid="judge-initials">{{ judgeInitials }} -</span>
        <div class="d-flex flex-column">
          <div
            class="d-flex justify-end"
            v-for="{
              activityDescription,
              activityClassDescription,
              activityDisplayCode,
              roomCode,
              showDars,
            } in activities"
          >
            <v-tooltip :text="activityDescription">
              <template #activator="{ props }">
                <div
                  v-bind="props"
                  :class="
                    cleanActivityClassDescription(activityClassDescription)
                  "
                >
                  <span data-testid="display-code">{{
                    activityDisplayCode
                  }}</span
                  ><span class="ml-1" v-if="roomCode" data-testid="room"
                    >({{ roomCode }})</span
                  >
                </div>
              </template>
            </v-tooltip>
            <v-icon
              v-if="showDars"
              data-testid="dars"
              class="ml-1"
              :icon="mdiHeadphones"
            />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
<script setup lang="ts">
  import { CalendarDayActivity } from '@/types';
  import { mdiHeadphones } from '@mdi/js';
  import { computed } from 'vue';

  const props = defineProps<{
    activities: CalendarDayActivity[];
  }>();

  const cleanActivityClassDescription = (
    activityClassDescription: string
  ): string => {
    return activityClassDescription.trim().replace(/\s+/g, '-').toLowerCase();
  };

  const groupedData = computed<[string, [string, CalendarDayActivity[]][]][]>(
    () => {
      const data: Record<string, Record<string, CalendarDayActivity[]>> = {};

      // Arrange data to easily render judge's activities
      // Location
      //  - Judge
      //    - Activities
      for (const activity of props.activities) {
        const locationKey = activity.locationShortName;
        const judgeKey = activity.judgeInitials;

        if (!data[locationKey]) {
          data[locationKey] = {};
        }

        if (!data[locationKey][judgeKey]) {
          data[locationKey][judgeKey] = [];
        }

        data[locationKey][judgeKey].push(activity);
      }

      return Object.entries(data)
        .sort(([a], [b]) => a.localeCompare(b)) // sort location name
        .map(([locationName, judgeActivities]) => [
          locationName,
          Object.entries(judgeActivities)
            .sort(([a], [b]) => a.localeCompare(b)) // sort judge initials
            .map(([judgeInitials, activities]) => [
              judgeInitials,
              [...activities].sort((a, b) =>
                a.activityDisplayCode.localeCompare(b.activityDisplayCode)
              ), // sort activity display code
            ]),
        ]);
    }
  );
</script>
<style scoped>
  .calendar-day {
    color: var(--text-blue-800) !important;
  }
</style>
