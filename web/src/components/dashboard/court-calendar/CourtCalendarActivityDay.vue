<template>
  <div v-for="{ locationName } in locations">
    {{ locationName }}
  </div>
</template>
<script lang="ts" setup>
  import { CourtCalendarLocation } from '@/types';

  const props = defineProps<{
    locations: CourtCalendarLocation[];
    date?: Date;
  }>();
</script>
