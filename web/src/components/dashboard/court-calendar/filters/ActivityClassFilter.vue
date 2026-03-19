<template>
  <v-btn-toggle
    v-model="modelValue"
    mandatory
    density="compact"
    rounded="pill"
    class="mx-2 border border-secondary"
  >
    <v-btn value="all">All</v-btn>
    <v-btn value="SIT">Sitting</v-btn>
    <v-btn value="NS">Non-sitting</v-btn>
  </v-btn-toggle>
</template>
<script setup lang="ts">
  const modelValue = defineModel<string>({ default: 'all' });
</script>
<style scoped>
  :deep(.v-btn.v-btn--active) {
    background-color: var(--bg-blue-800);
    color: var(--text-white-500);
  }
</style>
