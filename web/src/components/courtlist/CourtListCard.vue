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
        <v-col>
          <v-btn-secondary> View shared folder </v-btn-secondary>
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
          <h5><a href="#">See more about this location</a></h5>
        </v-col>
      </v-row>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
  import { CourtListCardInfo } from '@/types/courtlist';
  import { PropType } from 'vue';

  defineProps({
    cardInfo: {
      type: Object as PropType<CourtListCardInfo>,
      required: true,
    },
  });
</script>
