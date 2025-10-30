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
            v-if="isCriminal"
            :prepend-icon="mdiScaleBalance"
            :class="{ 'active-tab': selectedTab === 'sentence' }"
            value="sentence"
            border="md"
            rounded="lg"
          >
            Sentence/order details
          </v-tab>
        </v-tabs>
      </v-col>
    </v-row>
    <v-window mandatory continuous v-model="selectedTab" class="mt-3">
      <v-window-item value="documents">
        <CriminalDocumentsView
          v-if="isCriminal"
          :participants="criminalDetails.participant"
        />
        <CivilDocumentsView
          v-else
          :documents="civilDetails.document"
          :courtClassCd="courtClassCd!"
          :fileId="fileId"
        />
      </v-window-item>
      <v-window-item value="appearances">
        <AppearancesView
          :appearances="
            isCriminal
              ? criminalDetails.appearances?.apprDetail
              : civilDetails.appearances?.apprDetail
          "
          :isCriminal="isCriminal"
          :fileNumber="fileId"
          :courtClassCd="courtClassCd!"
        />
      </v-window-item>
      <v-window-item value="sentence">
        <SentenceOrderDetailsView
          v-if="isCriminal"
          :participants="criminalDetails.participant"
        />
      </v-window-item>
    </v-window>
  </v-container>
</template>

<script setup lang="ts">
  import CivilDocumentsView from '@/components/case-details/civil/CivilDocumentsView.vue';
  import CriminalDocumentsView from '@/components/case-details/criminal/CriminalDocumentsView.vue';
  import { civilFileDetailsType } from '@/types/civil/jsonTypes';
  import { DivisionEnum } from '@/types/common/index';
  import { criminalFileDetailsType } from '@/types/criminal/jsonTypes';
  import { FileDetailsType } from '@/types/shared';
  import { mdiCalendar, mdiScaleBalance, mdiTextBoxOutline } from '@mdi/js';
  import { computed, ref } from 'vue';
  import SentenceOrderDetailsView from '../criminal/sentence-order-details/SentenceOrderDetailsView.vue';
  import AppearancesView from './AppearancesView.vue';

  const props = defineProps<{
    details: FileDetailsType;
    activityClassCd: string;
    fileId: string;
    courtClassCd?: string;
  }>();

  const isCriminal = computed(() => props.activityClassCd === DivisionEnum.R);
  const selectedTab = ref('documents');
  const criminalDetails = ref<criminalFileDetailsType>(
    {} as criminalFileDetailsType
  );
  const civilDetails = ref<civilFileDetailsType>({} as civilFileDetailsType);

  if (isCriminal.value) {
    criminalDetails.value = props.details as criminalFileDetailsType;
  } else {
    civilDetails.value = props.details as civilFileDetailsType;
  }
</script>

<style scoped>
  .active-tab {
    border-color: #72acca !important;
    background-color: #72acca !important;
    color: white !important;
  }
</style>
