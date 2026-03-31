<template>
  <div class="calendar-day">
    <div
      class="mb-2"
      v-for="{ locationShortName, locationId, activities } in locations"
      :key="locationId"
    >
      <b
        ><span data-testid="short-name">{{ locationShortName }}</span></b
      >
      <ul class="activities pa-0 ma-0" data-testid="activities">
        <li
          v-for="{
            activityCode,
            activityDisplayCode,
            activityDescription,
            activityClassDescription,
            courtRooms,
          } in activities"
          :key="activityCode"
          :class="`${cleanActivityClassDescription(activityClassDescription)} border-b mb-1`"
        >
          <v-tooltip :text="activityDescription" location="top">
            <template #activator="{ props: tooltipProps }">
              <span v-bind="tooltipProps">
                {{ activityDisplayCode ?? activityDescription }}
                <span v-if="courtRooms && courtRooms.length > 0">
                  ({{ courtRooms.join(', ') }})</span
                >
              </span>
            </template>
          </v-tooltip>
        </li>
      </ul>
    </div>
  </div>
</template>
<script lang="ts" setup>
  import { CourtCalendarLocation } from '@/types';

  const props = defineProps<{
    locations: CourtCalendarLocation[];
    date?: Date;
  }>();

  const cleanActivityClassDescription = (
    activityClassDescription: string
  ): string => {
    return activityClassDescription
      .trim()
      .replaceAll(/\s+/g, '-')
      .toLowerCase();
  };
</script>
<style scoped>
  .calendar-day {
    color: var(--text-blue-800) !important;
  }

  ul {
    list-style: none;
  }
</style>
