<template>
  <Summary v-if="details" :details />
  <adjudicator-restrictions-panel
    v-if="adjudicatorRestrictions?.length > 0"
    :adjudicatorRestrictions
  />
  <accused-panel
    v-if="details"
    :accused="details.participant"
    :activityClass="details.activityClassDesc"
    :appearances
  />
</template>

<script setup lang="ts">
  import { AdjudicatorRestrictionsInfoType } from '@/types/common';
  import { criminalFileDetailsType } from '@/types/criminal/jsonTypes';
  import { ref } from 'vue';
  import AccusedPanel from './Accused/AccusedPanel.vue';
  import AdjudicatorRestrictionsPanel from './AdjudicatorRestrictions/AdjudicatorRestrictionsPanel.vue';
  import Summary from './Summary.vue';

  const props = defineProps<{
    details: criminalFileDetailsType;
    adjudicatorRestrictions: AdjudicatorRestrictionsInfoType[];
  }>();

  const details = ref(props.details);
  const appearances = ref(props.details.appearances?.apprDetail);
</script>
