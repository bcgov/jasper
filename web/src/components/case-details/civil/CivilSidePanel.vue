<template>
  <adjudicator-restrictions-panel
    v-if="adjudicatorRestrictions?.length > 0"
    :adjudicatorRestrictions
  />
</template>
<script setup lang="ts">
  import { civilFileDetailsType } from '@/types/civil/jsonTypes';
  import { AdjudicatorRestrictionsInfoType } from '@/types/common';
  import AdjudicatorRestrictionsPanel from '../common/adjudicator-restrictions/AdjudicatorRestrictionsPanel.vue';

  const props = defineProps<{
    details: civilFileDetailsType;
    adjudicatorRestrictions: AdjudicatorRestrictionsInfoType[];
  }>();
</script>
