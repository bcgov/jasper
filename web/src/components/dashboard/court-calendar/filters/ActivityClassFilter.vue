<template>
  <v-btn-toggle
    v-model="modelValue"
    mandatory
    density="compact"
    rounded="pill"
    class="mx-2 border border-secondary"
  >
    <v-btn value="all">All</v-btn>
    <v-btn :value="ActivityClassEnum.Sitting">Sitting</v-btn>
    <v-btn :value="ActivityClassEnum.NonSitting">Non-sitting</v-btn>
  </v-btn-toggle>
</template>
<script setup lang="ts">
  import { ActivityClassEnum } from '@/types/common';

  const modelValue = defineModel<string>({ default: 'all' });
</script>
