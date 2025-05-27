<template>
  <v-chip
    :class="['text-uppercase', 'mr-2', getBadgeStyle]"
    variant="flat"
    size="small"
  >
    {{ getBadgeText }}
  </v-chip>
</template>

<script setup lang="ts">
  import { getCourtClassStyle } from '@/utils/utils';
  import { computed } from 'vue';

  const props = defineProps<{
    division: string;
    activityClassDesc: string;
    courtClassCd: string;
  }>();
  const getBadgeText = computed(() => {
    if (props.division?.toLowerCase() === 'criminal') {
      return `${props.division} - ${props.activityClassDesc}`;
    }
    return props.division;
  });

  const getBadgeStyle = computed(() => getCourtClassStyle(props.courtClassCd));
</script>
<style scoped>
  .criminal {
    background-color: var(--bg-blue-500);
  }

  .family {
    background-color: var(--bg-green-500);
  }

  .small-claims {
    background-color: var(--bg-purple-500);
  }
</style>
