<template>
  <div v-if="selected.length">
    <v-app-bar
      class="table-action-bar"
      :elevation="2"
      location="bottom"
      color="#183a4a"
      rounded
    >
      <v-app-bar-title class="px-5"
        >{{ selected.length }} selected</v-app-bar-title
      >
    </v-app-bar>
  </div>
</template>

<script setup>
  const props = defineProps({
    selected: {
      type: Array,
      default: true,
    },
  });
</script>

<style scoped>
  .table-action-bar {
    border-radius: 70px !important;
    max-width: 50% !important;
    margin: 0 auto !important;
    left: 50% !important;
    transform: translateX(-50%) !important;
    margin-bottom: 2.5rem !important;
  }
</style>
