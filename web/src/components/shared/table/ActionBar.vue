<template>
  <div v-if="selected.length">
    <v-app-bar
      class="action-bar"
      :elevation="2"
      location="bottom"
      color="#183a4a"
      rounded
      floating
    >
      <v-app-bar-title class="px-2">
        <span
          style="display: inline-flex; align-items: center; font-size: 1rem"
        >
          {{ selected.length }}&nbsp;

          <template v-if="selectionPrependText">
            <strong> {{ selectionPrependText }} &nbsp;</strong>
          </template>
          selected
        </span>
      </v-app-bar-title>
      <slot></slot>
    </v-app-bar>
  </div>
</template>

<script setup lang="ts">
  defineProps({
    selected: {
      type: Array,
      default: () => [],
    },
    selectionPrependText: {
      type: String,
      required: false,
    },
  });
</script>

<style scoped>
  .action-bar {
    border-radius: 70px !important;
    min-width: 30% !important;
    max-width: 90% !important;
    width: fit-content !important;
    margin: 0 auto !important;
    left: 50% !important;
    transform: translateX(-50%) !important;
    margin-bottom: 1.5rem !important;
    position: fixed !important;
    z-index: 2 !important;
  }
</style>
