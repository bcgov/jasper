<template>
  <v-tooltip :disabled="!showTooltip">
    <template #activator="{ props }">
      <span v-bind="props">{{ value }}</span>
    </template>

    <div class="d-flex flex-column">
      <div v-for="(item, index) in values.slice(1)" :key="index">
        {{ item }}
      </div>
    </div>
  </v-tooltip>
</template>
<script setup lang="ts">
  import { computed } from 'vue';

  const props = defineProps<{
    values: string[];
  }>();

  const value = computed(() =>
    props.values.length > 1
      ? `${props.values[0]} +${props.values.length - 1}`
      : props.values[0] || ''
  );

  const showTooltip = computed(() => props.values.length > 1);
</script>
