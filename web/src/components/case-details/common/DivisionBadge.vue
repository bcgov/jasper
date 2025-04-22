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
  import { computed } from 'vue';

  const props = defineProps<{ division: string; activityClassDesc: string }>();
  const getBadgeText = computed(() => {
    if (props.division?.toLowerCase() === 'criminal') {
      return `${props.division} - ${props.activityClassDesc}`;
    }
    return props.division;
  });

  const getBadgeStyle = computed(() => {
    return props.division?.toLowerCase().replace(' ', '-');
  });
</script>
<style scoped>
  .criminal {
    background-color: var(--bg-blue);
  }

  .family {
    background-color: var(--bg-green);
  }

  .small-claims {
    background-color: var(--bg-purple);
  }
</style>
