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
            <button
              type="button"
              class="link-button text-decoration-underline cursor-pointer"
              @click="showLocationDialog = true"
            >
              See more about this location
            </button>
          </h5>
        </v-col>
      </v-row>
    </v-card-text>

    <CourtLocationInfoDialog
      v-model="showLocationDialog"
      :agencyIdCode="matchedLocation?.agencyIdentifierCd"
      :locationUrl="matchedLocation?.infoLink"
    />
  </v-card>
</template>

<script setup lang="ts">
  import CourtLocationInfoDialog from '@/components/courtlist/CourtLocationInfoDialog.vue';
  import { PERMISSIONS } from '@/constants/permissions';
  import { useCommonStore } from '@/stores';
  import { CourtListCardInfo } from '@/types/courtlist';
  import { computed, PropType, ref } from 'vue';
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

  const showLocationDialog = ref(false);

  const matchedLocation = computed(() => {
    // Match on id first since it is the most reliable, then fall back to name.
    return commonStore.courtRoomsAndLocations.find(
      (location) =>
        location.locationId === props.cardInfo.courtListLocationID.toString() ||
        location.name === props.cardInfo.courtListLocation
    );
  });
</script>

<style scoped>
  .link-button {
    background: none;
    border: 0;
    padding: 0;
    color: inherit;
    font: inherit;
  }
</style>
