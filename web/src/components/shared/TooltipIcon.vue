<template>
  <v-tooltip :text :location interactive>
    <template v-slot:activator="{ props }">
      <v-icon v-bind="props" :icon="icon" color="primary" size="large" />
    </template>
  </v-tooltip>
</template>

<script setup lang="ts">
  defineProps<{
    icon: string;
    text: string;
    location?: 'top' | 'bottom' | 'start' | 'end' | 'left' | 'right';
    interactive?: boolean;
  }>();
</script>
