<template>
  <v-expansion-panels>
    <v-expansion-panel :value="LOCATIONS">
      <v-expansion-panel-title> Locations </v-expansion-panel-title>
      <v-expansion-panel-text>
        <v-list class="pa-0">
          <v-list-item
            v-for="location in locations"
            :key="location.id"
            class="pa-0"
          >
            <v-checkbox
              :label="location.shortName"
              hide-details
              density="compact"
            >
            </v-checkbox>
          </v-list-item>
        </v-list>
      </v-expansion-panel-text>
    </v-expansion-panel>
  </v-expansion-panels>
</template>
<script setup lang="ts">
  const LOCATIONS = 'locations';

  const props = defineProps<{
    isLocationFilterLoading: boolean;
    locations: Location[];
  }>();
</script>
<style scoped>
  :deep(.v-expansion-panel) {
    width: 15rem;
  }

  :deep(.v-list-item) {
    min-height: 1rem;
  }

  :deep(.v-expansion-panel button) {
    padding-top: 0;
    padding-bottom: 0;
    min-height: 2rem !important;
  }

  :deep(.v-expansion-panel-text__wrapper) {
    padding-top: 0px;
    padding-bottom: 0px;
  }

  :deep(.v-selection-control .v-label) {
    margin-bottom: 0;
    margin-left: 0.25rem;
    font-size: 0.875rem !important;
  }

  /* Adjust checkbox size */
  :deep(.v-selection-control__wrapper) {
    width: 1.125rem;
    height: 1.125rem;
  }

  :deep(.v-checkbox .v-selection-control) {
    min-height: 0.5rem;
  }

  :deep(.v-checkbox .v-selection-control__input) {
    width: 1.125rem;
    height: 1.125rem;
  }

  :deep(.v-checkbox .v-selection-control__input i) {
    font-size: 1.125rem;
  }
</style>
