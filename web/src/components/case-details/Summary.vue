<template>
  <h5 class="my-1">Case Details</h5>
  <v-card color="#efedf5">
    <division-badge :activityClassDesc division="Criminal" class="mx-3 my-3" />
    <span v-if="bansExist"><b style="color: #e30e0e">BAN</b></span>
    <v-card-title style="text-wrap: wrap">{{ names }}</v-card-title>
    <v-card-subtitle>{{ location }}</v-card-subtitle>
    <file-markers
      :participants
      :appearances
      division="Criminal"
      class="mx-3 mt-2"
    />
    <v-row class="mx-1 mt-2">
      <v-col cols="6" class="data-label">Elections</v-col>
      <v-col></v-col>
    </v-row>
    <v-row class="mx-1">
      <v-col cols="6" class="data-label">Crown</v-col>
      <v-col>{{ crownName }}</v-col>
    </v-row>
    <v-row class="mx-1 mb-1">
      <v-col cols="6" class="data-label">Case Age</v-col>
      <v-col>{{ details.caseAgeDays }} days</v-col>
    </v-row>
  </v-card>
</template>

<script setup lang="ts">
  import FileMarkers from '@/components/shared/FileMarkers.vue';
  import { criminalFileDetailsType } from '@/types/criminal/jsonTypes';
  import { computed, ref } from 'vue';
  import DivisionBadge from './DivisionBadge.vue';

  const props = defineProps<{ details: criminalFileDetailsType }>();

  const details = ref(props.details);
  const participants = ref(details.value.participant);
  const bansExist = participants.value.some((p) => p.ban.length > 0);
  const appearances = ref(details.value.appearances?.apprDetail);
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
