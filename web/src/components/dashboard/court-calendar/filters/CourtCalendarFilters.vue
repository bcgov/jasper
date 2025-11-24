<template>
  <div class="filter-container">
    <div v-if="selectedLocations.length > 0" class="selected-filters mb-3">
      <div class="d-flex justify-space-between align-center mb-2">
        <span>Selected Filters</span>
        <v-btn
          variant="text"
          size="small"
          class="clear-all-btn"
          @click="clearAllFilters"
        >
          Clear All
        </v-btn>
      </div>
      <div class="selected-filters-chips">
        <v-chip
          v-for="locationId in selectedLocations"
          :key="locationId"
          closable
          size="small"
          class="ma-1"
          @click:close="removeLocation(locationId)"
        >
          {{ getLocationName(locationId) }}
        </v-chip>
      </div>
    </div>
    <v-expansion-panels multiple flat>
      <v-expansion-panel>
        <v-expansion-panel-title> Locations </v-expansion-panel-title>
        <v-expansion-panel-text>
          <FilterItem
            v-model="selectedLocations"
            title="Locations"
            :items="locationItems"
            :preview-count="5"
          />
        </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>
  </div>
</template>
<script setup lang="ts">
  import { LocationInfo } from '@/types/courtlist';
  import { computed } from 'vue';
  import FilterItem from './FilterItem.vue';

  const props = defineProps<{
    isLocationFilterLoading: boolean;
    locations: LocationInfo[];
  }>();

  const selectedLocations = defineModel<string[]>('selectedLocations', {
    default: [],
  });

  const locationItems = computed(() =>
    props.locations.map((location) => ({
      value: location.locationId,
      text: location.shortName,
    }))
  );

  const removeLocation = (locationId: string) => {
    const index = selectedLocations.value.indexOf(locationId);
    if (index > -1) {
      selectedLocations.value.splice(index, 1);
    }
  };

  const clearAllFilters = () => {
    selectedLocations.value = [];
  };

  const getLocationName = (locationId: string) => {
    const location = props.locations.find((l) => l.locationId === locationId);
    return location?.shortName || locationId;
  };
</script>
<style scoped>
  .filter-container {
    width: 15rem;
    flex-shrink: 0;
  }

  :deep(.v-expansion-panel) {
    width: 100%;
    box-shadow: none !important;
    border: none !important;
  }

  :deep(.v-expansion-panel::before) {
    box-shadow: none !important;
  }

  :deep(.v-expansion-panel--active > .v-expansion-panel-title) {
    border-bottom: none !important;
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
    padding: 0 1rem;
  }

  :deep(.v-selection-control .v-label) {
    margin-bottom: 0;
    margin-left: 0.25rem;
    font-size: 0.875rem !important;
  }

  :deep(.v-expansion-panel-title) {
    padding: 0 1rem;
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

  .selected-filters {
    padding: 0.75rem;
    background-color: var(--bg-gray-100);
    border-radius: 4px;
    border: 1px solid var(--bg-gray-300);
    width: 100%;
    box-sizing: border-box;
  }

  .selected-filters-chips {
    max-height: 10rem;
    overflow-y: auto;
    overflow-x: hidden;
  }

  .selected-filters-chips::-webkit-scrollbar {
    width: 6px;
  }

  .selected-filters-chips::-webkit-scrollbar-track {
    background: var(--bg-scrollbar-track);
    border-radius: 3px;
  }

  .selected-filters-chips::-webkit-scrollbar-thumb {
    background: var(--bg-scrollbar-thumb);
    border-radius: 3px;
  }

  .selected-filters-chips::-webkit-scrollbar-thumb:hover {
    background: var(--bg-scrollbar-thumb-hover);
  }

  .clear-all-btn {
    text-transform: none;
    letter-spacing: normal;
    min-width: auto;
    padding: 0 0.5rem;
  }

  .clear-all-btn :deep(.v-btn__content) {
    font-size: 0.875rem;
    color: var(--text-blue-600);
    text-decoration: underline;
  }

  .clear-all-btn:hover :deep(.v-btn__content) {
    color: var(--text-blue-900);
  }
</style>
