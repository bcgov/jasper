<template>
  <v-btn-tertiary v-if="showSearch" type="submit" size="large" class="mr-2" text="Search" />
  <v-btn v-if="showReset" size="large" text="Reset" @click="() => emit('reset')" />
</template>

<script setup>
  const emit = defineEmits(['reset']);

  const props = defineProps({
    showSearch: {
      type: Boolean,
      default: true,
    },
    showReset: {
      type: Boolean,
      default: true,
    },
  });
</script>