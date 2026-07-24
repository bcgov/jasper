<template>
  <v-card color="#efedf5" class="w-100 mb-2">
    <v-card-title>
      <v-row>
        <v-col>
          <h3>{{ cardInfo.courtListLocation }}</h3>
        </v-col>
      </v-row>
    </v-card-title>
    <v-card-text>
      <v-row>
        <v-col>
          <h5>
            Rooms: {{ cardInfo.courtListRoom }}
            {{ cardInfo.amPM ? `(${cardInfo.amPM})` : '' }}
          </h5>
        </v-col>
        <v-col>
          <h5>
            {{ cardInfo.presider ? `Presider: ${cardInfo.presider}` : '' }}
          </h5>
        </v-col>
        <v-col>
          <h5>
            {{
              cardInfo.courtClerk ? `Court clerk: ${cardInfo.courtClerk}` : ''
            }}
          </h5>
        </v-col>
        <v-col v-if="canViewSharedFolder">
          <v-btn data-test="view-shared-folder-btn" @click="openSharedFolder">
            View Shared Folder
          </v-btn>
        </v-col>
      </v-row>

      <v-row>
        <v-col>
          <h5>Activity: {{ cardInfo.activity }}</h5>
        </v-col>
        <v-col>
          <h5>Scheduled: {{ cardInfo.fileCount }} files</h5>
        </v-col>
        <v-col>
          <h5>
            <a href="#">{{ cardInfo.email }}</a>
          </h5>
        </v-col>
        <v-col>
          <h5>
            <a :href="infoAddress" target="_blank">
              See more about this location
              <v-icon :icon="mdiOpenInNew" size="x-small" />
            </a>
          </h5>
        </v-col>
      </v-row>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
  import { PERMISSIONS } from '@/constants/permissions';
  import { useCommonStore } from '@/stores';
  import { CourtListCardInfo } from '@/types/courtlist';
  import { mdiOpenInNew } from '@mdi/js';
  import { computed, PropType } from 'vue';
  import { useRouter } from 'vue-router';

  const props = defineProps({
    cardInfo: {
      type: Object as PropType<CourtListCardInfo>,
      required: true,
    },
    date: {
      type: String,
      required: true,
    },
  });

  const commonStore = useCommonStore();
  const router = useRouter();

  const canViewSharedFolder = computed(
    () =>
      commonStore.userInfo?.permissions?.includes(
        PERMISSIONS.LIST_TRANSITORY_DOCUMENTS
      ) ?? false
  );

  const openSharedFolder = (): void => {
    const route = router.resolve({
      name: 'TransitoryDocuments',
      params: {
        locationId: props.cardInfo.courtListLocationID.toString(),
        roomCd: props.cardInfo.courtListRoom,
        date: props.date,
      },
      query: { location: props.cardInfo.courtListLocation },
    });

    window.open(route.href, '_blank', 'noopener');
  };

  const infoAddress = computed<string>(() => {
    // Try to get the location from the store using the id since it is the most reliable.
    // Failing that, try to get the location from the name
    return commonStore.courtRoomsAndLocations.filter(
      (location) =>
        location.locationId === props.cardInfo.courtListLocationID.toString() ||
        location.name === props.cardInfo.courtListLocation
    )[0]?.infoLink;
  });
</script>
