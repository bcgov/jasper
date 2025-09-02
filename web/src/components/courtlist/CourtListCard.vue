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
  import { useCommonStore } from '@/stores';
  import { CourtListCardInfo } from '@/types/courtlist';
  import { mdiOpenInNew } from '@mdi/js';
  import { computed, PropType } from 'vue';

  const props = defineProps({
    cardInfo: {
      type: Object as PropType<CourtListCardInfo>,
      required: true,
    },
  });

  const commonStore = useCommonStore();
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
