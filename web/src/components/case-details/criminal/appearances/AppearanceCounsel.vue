<template>
  <div>
    <span v-if="counsel?.fullName">
      {{ counsel.fullName }}
    </span>
    <span v-else>No counsel.</span>
  </div>
</template>


<script setup lang="ts">
  import { JustinCounsel } from '@/types/criminal/jsonTypes';

  defineProps<{
    counsel: JustinCounsel
  }>();
</script>