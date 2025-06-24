<template>
  <div class="d-flex justify-center my-3">
    <div class="other-calendars text-left">
      <a class="text-decoration-underline cursor-pointer"
        ><v-icon :icon="mdiChevronRight" />View other calendars</a
      >
    </div>
    <MonthPicker v-model="selectedDate" />
    <div class="more text-right">
      <a class="text-decoration-underline cursor-pointer"
        ><v-icon :icon="mdiDotsHorizontal" />More</a
      >
    </div>
  </div>
</template>
<script setup lang="ts">
  import { mdiChevronRight, mdiDotsHorizontal } from '@mdi/js';

  const selectedDate = defineModel<Date>();
</script>
<style scoped>
  .other-calendars,
  .more {
    flex: 1;
    color: var(--text-blue-500);
  }

  .other-calendars:hover,
  .more:hover {
    color: var(--text-blue-800);
  }
</style>
