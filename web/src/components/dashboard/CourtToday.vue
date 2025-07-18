<template>
  <v-banner bgColor="var(--bg-blue-100)">
    <div class="d-flex justify-space-between w-100 align-end py-4">
      <div class="d-flex">
        <div
          class="d-flex flex-column justify-center align-center mx-3 court-today"
        >
          <v-icon :icon="mdiCalendarCheckOutline" size="x-large" />
          <span>Court</span>
          <span>today</span>
        </div>
        <!-- Activity -->
        <v-slide-group show-arrows>
          <v-slide-group-item
            v-if="today.activities && today.activities.length"
            v-for="(
              {
                locationName,
                roomCode,
                activityDescription,
                period,
                activityClassDescription,
                activityDisplayCode,
                filesCount,
                continuationsCount,
              },
              index
            ) in today.activities"
          >
            <div
              v-if="activityDisplayCode"
              :class="[
                'px-4',
                { divider: index !== today.activities.length - 1 },
              ]"
            >
              <h2>{{ locationName }} {{ period ? `(${period})` : '' }}</h2>
              <div class="d-flex flex-column activity-details">
                <div class="d-flex">
                  <div v-if="roomCode">
                    <span class="mr-2">Court room:</span>
                    <span>{{ roomCode }}</span>
                  </div>
                  <div>
                    <span class="mr-2">Activity:</span>
                    <span
                      :class="
                        cleanActivityClassDescription(activityClassDescription)
                      "
                      >{{ activityDescription }}</span
                    >
                  </div>
                </div>
                <div data-testid="scheduled" v-if="filesCount" class="mt-1">
                  <span class="mr-3">Scheduled:</span>
                  <span
                    >{{ filesCount }}
                    {{ filesCount === 1 ? 'file' : 'files' }}</span
                  >
                  <span class="ml-2" v-if="continuationsCount > 0">
                    {{
                      continuationsCount === 1
                        ? `(${continuationsCount} continuation)`
                        : `(${continuationsCount} continuations)`
                    }}
                  </span>
                </div>
              </div>
            </div>
            <div v-else class="d-flex justify-center align-center">
              <h2 data-testid="no-court-scheduled" class="px-4">
                {{ activityDescription }}
              </h2>
            </div>
          </v-slide-group-item>
        </v-slide-group>
      </div>
      <v-btn-tertiary :to="{ name: 'CourtList', query: { date: today.date } }"
        >Today's court list</v-btn-tertiary
      >
    </div>
  </v-banner>
</template>
<script setup lang="ts">
  import { CalendarDay } from '@/types';
  import { mdiCalendarCheckOutline } from '@mdi/js';
  import { computed } from 'vue';

  const props = defineProps<{
    today: CalendarDay;
  }>();

  const cleanActivityClassDescription = (
    activityClassDescription: string
  ): string => {
    return activityClassDescription.trim().replace(/\s+/g, '-').toLowerCase();
  };

  const showActivityDetails = computed(
    () =>
      props.today.activities &&
      props.today.activities.length > 0 &&
      props.today.activities.every((a) => a.activityDescription)
  );
</script>
<style lang="css" scoped>
  .v-banner {
    color: var(--text-blue-800);
  }
  .court-today {
    font-size: 1rem;
  }
  .activity-details div {
    margin-right: 1.5rem;
    font-size: 1.125rem;
  }
  .divider {
    border-right: 0.125rem solid var(--border-gray-500);
  }

  .v-btn:hover {
    color: var(--text-white-500) !important;
    text-decoration: none;
  }
</style>
