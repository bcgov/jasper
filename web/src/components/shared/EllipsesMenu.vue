<template>
  <v-menu>
    <template v-slot:activator="{ props }">
      <v-btn
        :density
        color="var(--text-blue-500)"
        :icon="mdiDotsVertical"
        variant="text"
        v-bind="props"
      />
    </template>

    <v-list>
      <v-list-item
        :disabled="!enable"
        v-for="({ title, action, enable }, i) in menuItems"
        :key="i"
        :value="i"
      >
        <v-list-item-title @click="action">{{ title }}</v-list-item-title>
      </v-list-item>
    </v-list>
  </v-menu>
</template>

<script setup lang="ts">
  import { mdiDotsVertical } from '@mdi/js';

  withDefaults(
    defineProps<{
      menuItems: { title: string; action: () => void; enable: boolean }[];
      density?: 'default' | 'comfortable' | 'compact';
    }>(),
    {
      density: 'default',
    }
  );
</script>
