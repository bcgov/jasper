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
  import { computed } from 'vue';
  import { mdiBank } from '@mdi/js';
  import DivisionBadge from '../common/DivisionBadge.vue';

  const props = defineProps<{
    details: criminalFileDetailsType;
    hasBans?: boolean;
  }>();

  const details = computed(() => props.details);
  const participants = computed(() => props.details.participant ?? []);
  const bansExist = computed(
    () =>
      props.hasBans ??
      participants.value.some((participant) => (participant.ban ?? []).length > 0)
  );
  const courtClassCd = computed(() => props.details.courtClassCd);
  const proceeded = computed(() =>
    props.details.indictableYN === 'Y' ? 'By Indictment' : 'Summarily'
  );
  const names = computed(() => {
    const primaryParticipant = participants.value[0];

    if (!primaryParticipant) {
      return '';
    }

    return (
      primaryParticipant.lastNm.toUpperCase() +
      ', ' +
      primaryParticipant.givenNm +
      (participants.value.length > 1
        ? ` and ${participants.value.length - 1} other(s)`
        : '')
    );
  });
  const activityClassDesc = computed(() => props.details.activityClassDesc);
  const location = computed(() => props.details.homeLocationAgencyName);
  const crownAssigned = computed(() => props.details.crown?.find((c) => c.assigned));
  const crownName = computed(() =>
    crownAssigned.value
      ? `${crownAssigned.value.lastNm}, ${crownAssigned.value.givenNm}`
      : ''
  );
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
