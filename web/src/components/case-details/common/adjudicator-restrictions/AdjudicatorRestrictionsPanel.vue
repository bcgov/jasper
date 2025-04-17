<template>
  <div class="pt-4">
    <h5>{{ titleText }}</h5>
    <v-card class="ar-card p-3" elevation="0">
      <adjudicator-restriction
        v-for="(restriction, index) in props.adjudicatorRestrictions"
        :key="index"
        :restriction="restriction"
        :class="
          index !== props.adjudicatorRestrictions.length - 1 ? 'mb-3' : ''
        "
      />
    </v-card>
  </div>
</template>
<script setup lang="ts">
  import { AdjudicatorRestrictionsInfoType } from '@/types/common';
  import { computed } from 'vue';
  import AdjudicatorRestriction from './AdjudicatorRestriction.vue';

  const props = defineProps<{
    adjudicatorRestrictions: AdjudicatorRestrictionsInfoType[];
  }>();

  const titleText = computed(
    () => `Adjudicator Restrictions (${props.adjudicatorRestrictions?.length})`
  );
</script>
<style scoped>
  .ar-card {
    background-color: var(--bg-light-gray);
    border: 2px solid var(--border-blue);
    border-radius: 0.5rem;
  }
</style>
