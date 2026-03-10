<template>
  <div class="case-details-header">
    <h5 class="mb-0 d-flex align-center">
      Case Details
      <span
        class="pl-3"
        :class="
          labelClasses[getCourtClassLabel(details.courtClassCd)] ||
          'criminal-label'
        "
      >
        {{ details.fileNumberTxt }}
      </span>
      <CopyToClipboard :text="details.fileNumberTxt" />
    </h5>
    <TooltipIcon
      v-if="details.courtLevelCd === CourtLevelEnum.S"
      text="Supreme Court case"
      :icon="mdiBank"
    />
  </div>

  <v-card color="var(--bg-gray-500)" flat>
    <div class="mx-2 d-flex align-center pt-2">
      <DivisionBadge
        division="Criminal"
        :activityClassDesc
        :courtClassCd
        class="my-0"
      />
      <span v-if="bansExist"
        ><b style="color: var(--text-red-500)">BAN</b></span
      >
    </div>
    <v-card-item>
      <v-card-title class="my-0" style="text-wrap: wrap">
        {{ names }}
      </v-card-title>
      <v-card-subtitle>{{ location }}</v-card-subtitle>
    </v-card-item>
    <v-row class="mx-3" dense>
      <v-col cols="6" class="data-label">Proceeded</v-col>
      <v-col> {{ proceeded }}</v-col>
    </v-row>
    <v-row class="mx-3" dense>
      <v-col cols="6" class="data-label">Crown</v-col>
      <v-col>{{ crownName }}</v-col>
    </v-row>
    <v-row class="mx-3 pb-1" dense>
      <v-col cols="6" class="data-label">Case Age (days)</v-col>
      <v-col>{{ details.caseAgeDays }}</v-col>
    </v-row>
  </v-card>
</template>
<script setup lang="ts">
  import CopyToClipboard from '@/components/shared/CopyToClipboard.vue';
  import { labelClasses } from '@/constants/labelClasses';
  import { CourtLevelEnum } from '@/types/common';
  import { criminalFileDetailsType } from '@/types/criminal/jsonTypes';
  import { getCourtClassLabel } from '@/utils/utils';
  import { computed, ref } from 'vue';
  import { mdiBank } from '@mdi/js';
  import DivisionBadge from '../common/DivisionBadge.vue';

  const props = defineProps<{ details: criminalFileDetailsType }>();

  const details = ref(props.details);
  const participants = ref(details.value.participant);
  const bansExist = participants.value.some((p) => p.ban.length > 0);
  const courtClassCd = props.details.courtClassCd;
  const proceeded = computed(() =>
    details.value.indictableYN === 'Y' ? 'By Indictment' : 'Summarily'
  );
  const names = computed(() => {
    return (
      participants.value[0].lastNm.toUpperCase() +
      ', ' +
      participants.value[0].givenNm +
      (participants.value.length > 1
        ? ` and ${participants.value.length - 1} other(s)`
        : '')
    );
  });
  const activityClassDesc = details.value.activityClassDesc;
  const location = details.value.homeLocationAgencyName;
  const crownAssigned = details.value.crown?.filter((c) => c.assigned)[0];
  const crownName = crownAssigned
    ? crownAssigned.lastNm + ', ' + crownAssigned.givenNm
    : '';
</script>
<style scoped>
  .case-details-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
  }

  .v-card {
    border-radius: 0.5rem !important;
  }
</style>
