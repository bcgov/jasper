<template>
  <v-container>
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
        class="mx-5"
      >
        Appearances
      </v-tab>
      <v-tab
        :prepend-icon="mdiScaleBalance"
        :class="{ 'active-tab': selectedTab === 'sentence' }"
        value="sentence"
        border="md"
        rounded="lg"
      >
        Sentence/order details
      </v-tab>
    </v-tabs>

    <v-window mandatory continuous v-model="selectedTab">
      <v-window-item value="documents">
        <!-- <DocumentsView /> -->
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

  defineProps<{ details: criminalFileDetailsType }>();

  const selectedTab = ref('appearances');
</script>

<style scoped>
  .active-tab {
    border-color: #72acca !important;
    background-color: #72acca !important;
    color: white !important;
  }
</style>
