<template>
  <h5 class="my-1">Case Details</h5>
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
  import { criminalFileDetailsType } from '@/types/criminal/jsonTypes';
  import { computed, ref } from 'vue';
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
  .v-card {
    border-radius: 0.5rem !important;
  }
</style>
