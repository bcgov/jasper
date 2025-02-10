<template>
  <v-btn-tertiary
    v-if="showSearch"
    type="submit"
    class="mr-2"
    text="Search"
    :size
  />
  <v-btn v-if="showReset" text="Reset" @click="() => emit('reset')" :size />
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
    size: {
      type: String,
      default: 'x-large',
    },
  });
</script>
