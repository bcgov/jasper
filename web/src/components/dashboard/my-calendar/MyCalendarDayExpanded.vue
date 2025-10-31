<template>
  <Teleport
    :to="`.fc-expand-wrapper[data-date='${day.date}']`"
    v-if="expandedDate === day.date"
  >
    <div class="pa-2 expanded-content" v-click-outside="close">
      <div class="header d-flex align-center justify-space-between">
        <span>{{ dayNumberText }}</span>
        <RouterLink :to="{ name: 'CourtList', query: { date: day.date } }">
          <v-icon
            data-testid="court-list"
            v-if="day.showCourtList"
            :icon="mdiListBoxOutline"
          />
        </RouterLink>
      </div>
      <div class="d-flex flex-column">
        <div
          v-for="(
            { locationName, activities, showVideo }, index
          ) in groupedActivities"
        >
          <div class="mb-2 d-flex justify-space-between">
            <span class="location" data-testid="name">{{ locationName }}</span>
            <v-icon
              data-testid="location-remote-icon"
              v-if="showVideo"
              :icon="mdiVideo"
            />
          </div>
          <div
            class="d-flex flex-column mb-1"
            v-for="{
              period,
              activityClassDescription,
              activityDescription,
              roomCode,
              isRemote,
              showDars,
              restrictions,
              activityCode,
              locationId,
            } in activities"
            :key="`${activityCode} - ${period}`"
          >
            <div class="d-flex justify-space-between">
              <div class="d-flex align-center">
                <v-chip
                  size="small"
                  density="compact"
                  class="period mr-1"
                  data-testid="period"
                  v-if="period"
                  >{{ period }}</v-chip
                >
                <div
                  :class="
                    cleanActivityClassDescription(activityClassDescription)
                  "
                >
                  <span data-testid="activity">
                    {{ activityDescription }}
                  </span>
                  <span v-if="roomCode" data-testid="room">
                    ({{ roomCode }})</span
                  >
                </div>
              </div>
              <div>
                <v-icon
                  v-if="showDars"
                  data-testid="dars"
                  :icon="mdiHeadphones"
                  class="cursor-pointer"
                  @click="openDarsModal(locationId, roomCode)"
                />
                <v-icon
                  data-testid="activity-remote-icon"
                  v-if="!showVideo && isRemote"
                  :icon="mdiVideo"
                />
              </div>
            </div>
            <div
              v-if="restrictions && restrictions.length > 0"
              class="d-flex flex-column justify-start mt-2 seized"
              data-testid="restrictions"
            >
              <span><b>Seized:</b></span>
              <a
                v-for="{
                  fileName,
                  fileId,
                  isCivil,
                  appearanceReasonCode,
                } in restrictions"
                :href="`/${isCivil ? 'civil' : 'criminal'}-file/${fileId}`"
                target="_blank"
                rel="noopener"
                >{{ fileName }} ({{ appearanceReasonCode }})</a
              >
            </div>
          </div>

          <hr v-if="index + 1 < groupedActivities.length" />
        </div>
      </div>
    </div>
  </Teleport>

  <!-- DARS Access Modal -->
  <DarsAccessModal
    v-model="showDarsModal"
    :prefillDate="darsModalData.date"
    :prefillLocationId="darsModalData.locationId"
    :prefillRoom="darsModalData.room"
  />
</template>
<script setup lang="ts">
  import DarsAccessModal from '@/components/dashboard/DarsAccessModal.vue';
  import { CalendarDay, CalendarDayActivity } from '@/types';
  import { parseDDMMMYYYYToDate } from '@/utils/dateUtils';
  import { mdiHeadphones, mdiListBoxOutline, mdiVideo } from '@mdi/js';
  import { computed, ref } from 'vue';

  const props = defineProps<{
    expandedDate: string | null;
    day: CalendarDay;
    close: () => void;
  }>();

  // DARS modal state
  const showDarsModal = ref(false);
  const darsModalData = ref({
    date: null as Date | null,
    locationId: null as number | null,
    room: '',
  });

  const openDarsModal = (locationId: number | undefined, roomCode: string) => {
    // Parse the date from the day.date string (format: DD MMM YYYY)
    const parsedDate = parseDDMMMYYYYToDate(props.day.date);
    darsModalData.value = {
      date: parsedDate || new Date(),
      locationId: locationId || null,
      room: roomCode || '',
    };
    showDarsModal.value = true;
  };

  const cleanActivityClassDescription = (
    activityClassDescription: string | undefined
  ): string => {
    return (activityClassDescription || '')
      .trim()
      .replaceAll(/\s+/g, '-')
      .toLowerCase();
  };

  const dayNumberText = computed(() => {
    const parsedDate = parseDDMMMYYYYToDate(props.day.date);
    return parsedDate?.getDate();
  });

  const groupedActivities = computed(() => {
    const data = new Map<string, CalendarDayActivity[]>();

    for (const activity of props.day.activities) {
      if (!data.has(activity.locationName)) {
        data.set(activity.locationName, []);
      }
      data.get(activity.locationName)?.push(activity);
    }

    return Array.from(data.entries()).map(([locationName, activities]) => ({
      locationName,
      showVideo: activities.every((a) => a.isRemote),
      activities,
    }));
  });
</script>
<style scoped>
  .expanded-content {
    position: absolute;
    width: 375px;
    top: 0;
    left: 0;
    z-index: 10;
    background: white;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.5);
    border: 2px solid var(--border-blue-500);
    font-size: 0.9375rem !important;
  }

  .period {
    color: var(--text-white-500);
    background-color: var(--bg-blue-800);
  }

  .header span {
    color: var(--text-blue-800);
    font-weight: bold;
    font-size: 1rem;
  }

  .header a {
    color: var(--text-blue-800);
    text-decoration: none;
  }

  .seized a {
    color: var(--text-blue-500);
  }

  .seized a:hover {
    color: var(--text-blue-500);
    text-decoration: none;
  }

  .location {
    color: var(--text-gray-400);
  }
</style>
