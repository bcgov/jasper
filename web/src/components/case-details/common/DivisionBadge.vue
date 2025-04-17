<template>
  <v-chip rounded="lg" :color variant="flat" size="small">
    {{
      props.division.toUpperCase() +
      (activityClassDesc ? ' - ' + activityClassDesc : '')
    }}
  </v-chip>
</template>

<script setup lang="ts">
  import { computed } from 'vue';

  const props = defineProps<{ division: string; activityClassDesc: string }>();

  const colors: { [key: string]: string } = {
    Criminal: '#4092c1',
    Family: '#2e8540',
  };

  const activityClassDesc = computed(() => {
    if (props.division !== 'Criminal') {
      return '';
    }
    if (props.activityClassDesc === 'Adult') {
      return 'ADULT';
    } else if (props.activityClassDesc === 'Youth Justice') {
      return 'YOUTH';
    } else {
      return '';
    }
  });

  const color = computed(() => colors[props.division] || '#79368f');
</script>
