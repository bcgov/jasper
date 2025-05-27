<template>
  <v-card class="elevation-0 border-0 bg-transparent">
    <b :class="['text-uppercase', getStatusStyle]">{{ restriction.status }}</b>
    {{ restriction.adjudicator }}
    <p class="m-0">{{ restriction.appliesTo }}</p>
  </v-card>
</template>
<script setup lang="ts">
  import { AdjudicatorRestrictionsInfoType } from '@/types/common';
  import { computed } from 'vue';

  const props = defineProps<{
    restriction: AdjudicatorRestrictionsInfoType;
  }>();

  const getStatusStyle = computed(() => {
    const status = props.restriction.status.trim().toLowerCase();
    return status;
  });
</script>
<style scoped>
  .seized,
  .assigned {
    color: var(--text-blue-500);
  }

  .disqualified,
  .appealed {
    color: var(--text-red-500);
  }

  p {
    color: var(--text-gray-300);
  }
</style>
