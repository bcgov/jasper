<template>
  <div class="d-flex flex-column calendar-day">
    <div
      class="mb-2"
      data-testid="activity-detail"
      v-for="{ locationName, judgeActivities } in groupedData"
      :key="locationName"
    >
      <b
        ><span data-testid="short-name">{{ locationName }}</span></b
      >

      <div
        data-testid="judge-activities"
        v-for="{ judgeInitials, judgeName, activities } in judgeActivities"
        :key="judgeInitials"
        :class="[
          'd-flex justify-space-between border-b mb-1',
          activities.every((a) => a.isJudgeAway) ? 'is-away' : '',
          activities.every((a) => a.isJudgeBorrowed) ? 'is-borrowed' : '',
        ]"
      >
        <v-tooltip :text="judgeName" data-testid="judge-initials">
          <template #activator="{ props }">
            <span v-bind="props">{{ judgeInitials }}</span>
          </template>
        </v-tooltip>
        <div class="d-flex flex-column">
          <div
            class="d-flex justify-end align-center"
            v-for="{
              activityDescription,
              activityClassDescription,
              activityDisplayCode,
              roomCode,
              showDars,
              period,
              activityCode,
              locationId,
            } in activities"
            :key="`${activityCode}-${period}`"
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
              size="16"
              :icon="mdiHeadphones"
              class="cursor-pointer"
              @click="openDarsModal(locationId, roomCode)"
            />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
<script setup lang="ts">
  import { useDarsStore } from '@/stores/DarsStore';
  import { CalendarDayActivity, LocationGroup } from '@/types';
  import { mdiHeadphones } from '@mdi/js';
  import { computed } from 'vue';

  const props = defineProps<{
    activities: CalendarDayActivity[];
    date?: Date;
  }>();

  const darsStore = useDarsStore();

  const openDarsModal = (locationId: number | undefined, roomCode: string) => {
    darsStore.openModal(
      props.date || new Date(),
      locationId?.toString() || null,
      roomCode || ''
    );
  };

  const cleanActivityClassDescription = (
    activityClassDescription: string
  ): string => {
    return activityClassDescription
      .trim()
      .replaceAll(/\s+/g, '-')
      .toLowerCase();
  };

  const groupedData = computed<LocationGroup[]>(() => {
    const locationMap = new Map<string, Map<string, CalendarDayActivity[]>>();

    // Group by location and judge
    for (const activity of props.activities) {
      if (!locationMap.has(activity.locationShortName)) {
        locationMap.set(activity.locationShortName, new Map());
      }

      const judgeMap = locationMap.get(activity.locationShortName)!;
      if (!judgeMap.has(activity.judgeInitials)) {
        judgeMap.set(activity.judgeInitials, []);
      }

      judgeMap.get(activity.judgeInitials)!.push(activity);
    }

    // Transform to output structure
    return Array.from(locationMap.entries()).map(
      ([locationName, judgeMap]) => ({
        locationName,
        judgeActivities: Array.from(judgeMap.entries()).map(
          ([judgeInitials, activities]) => ({
            judgeInitials,
            judgeName: activities[0]?.judgeName || '',
            activities,
          })
        ),
      })
    );
  });
</script>
<style scoped>
  .calendar-day {
    color: var(--text-blue-800) !important;
    font-size: 0.75rem;
  }

  .is-borrowed {
    background-color: var(--bg-yellow-500);
  }

  .is-away {
    background-color: var(--bg-blue-200);
  }
</style>
