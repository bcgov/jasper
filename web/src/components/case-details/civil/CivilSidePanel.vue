<template>
  <CivilSummary :details />
  <AdjudicatorRestrictionsPanel
    v-if="adjudicatorRestrictions?.length > 0"
    :adjudicatorRestrictions
  />
  <PartiesPanel :parties />
</template>
<script setup lang="ts">
  import { civilFileDetailsType } from '@/types/civil/jsonTypes';
  import { AdjudicatorRestrictionsInfoType } from '@/types/common';
  import AdjudicatorRestrictionsPanel from '../common/adjudicator-restrictions/AdjudicatorRestrictionsPanel.vue';
  import CivilSummary from './CivilSummary.vue';
  import PartiesPanel from './parties/PartiesPanel.vue';

  const props = defineProps<{
    details: civilFileDetailsType;
    adjudicatorRestrictions: AdjudicatorRestrictionsInfoType[];
  }>();

  const parties = props.details.party;
</script>
