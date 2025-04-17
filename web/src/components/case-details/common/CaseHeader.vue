<template>
  <v-container>
    <v-row>
      <v-col cols="10">
        <v-tabs
          v-model="selectedTab"
          color="#72acca"
          align-tabs="start"
          hide-slider
        >
          <v-tab
            :prepend-icon="mdiTextBoxOutline"
            :class="{ 'active-tab': selectedTab === 'documents' }"
            border="md"
            rounded="lg"
            value="documents"
          >
            Documents
          </v-tab>
          <v-tab
            :prepend-icon="mdiCalendar"
            :class="{ 'active-tab': selectedTab === 'appearances' }"
            border="md"
            rounded="lg"
            value="appearances"
            class="mx-3"
          >
            Appearances
          </v-tab>
          <v-tab
            :prepend-icon="mdiScaleBalance"
            :class="{ 'active-tab': selectedTab === 'sentence' }"
            value="sentence"
            border="md"
            rounded="lg"
            disabled
          >
            Sentence/order details
          </v-tab>
        </v-tabs>
      </v-col>
      <v-col cols="2">
        <v-btn-secondary text="View shared folder" />
      </v-col>
    </v-row>

    <v-window mandatory continuous v-model="selectedTab" class="mt-3">
      <v-window-item value="documents">
        <DocumentsView :participants="details.participant" />
      </v-window-item>
      <v-window-item value="appearances">
        <AppearancesView :appearances="details.appearances?.apprDetail" />
      </v-window-item>
      <v-window-item value="sentence">
        <!-- <SentenceOrderDetailsView /> -->
      </v-window-item>
    </v-window>
  </v-container>
</template>

<script setup lang="ts">
  import { criminalFileDetailsType } from '@/types/criminal/jsonTypes';
  import { mdiCalendar, mdiScaleBalance, mdiTextBoxOutline } from '@mdi/js';
  import { ref } from 'vue';
  import AppearancesView from './AppearancesView.vue';
  import DocumentsView from './DocumentsView.vue';

  defineProps<{ details: criminalFileDetailsType }>();
  const selectedTab = ref('documents');
</script>

<style scoped>
  .active-tab {
    border-color: #72acca !important;
    background-color: #72acca !important;
    color: white !important;
  }
</style>
