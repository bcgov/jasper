<template>
  <div class="cc-filters mb-2">
    <FilterDropdown
      title="Locations"
      :items="locationItems"
      v-model="selectedLocations"
    />
    <FilterDropdownGrouped
      v-if="selectedLocations?.length > 0"
      title="Presiders"
      :groups="presiderItems"
      v-model="selectedPresiders"
    />
    <v-btn
      class="clearAll"
      v-if="selectedLocations?.length > 0"
      hide-details
      @click="clearAllFilters"
    >
      Clear All
    </v-btn>
  </div>
</template>
<script setup lang="ts">
  import { Presider } from '@/types';
  import { LocationInfo } from '@/types/courtlist';
  import { computed } from 'vue';
  import FilterDropdown from './FilterDropdown.vue';
  import FilterDropdownGrouped from './FilterDropdownGrouped.vue';

  const props = defineProps<{
    isLocationFilterLoading: boolean;
    locations: LocationInfo[];
    presiders: Presider[];
  }>();

  const selectedLocations = defineModel<string[]>('selectedLocations', {
    default: [],
  });

  const selectedPresiders = defineModel<string[]>('selectedPresiders', {
    default: [],
  });

  const locationItems = computed(() =>
    props.locations.map((location) => ({
      value: location.locationId,
      text: location.shortName,
    }))
  );

  const presiderItems = computed<GroupedItems[]>(() => {
    const grouped = new Map<string, GroupedItems>();
    for (const presider of props.presiders) {
      const key =
        props.locations.find(
          (loc) => loc.locationId === presider.homeLocationId.toString()
        )?.shortName || '';
      if (!grouped.has(key)) {
        grouped.set(key, { label: key, items: [] });
      }
      grouped.get(key)!.items.push({
        value: presider.id.toString(),
        text: `${presider.initials} - ${presider.name}`,
      });
    }
    for (const group of grouped.values()) {
      group.items.sort((a, b) => a.text.localeCompare(b.text));
    }
    return [...grouped.values()].sort((a, b) => a.label.localeCompare(b.label));
  });

  watch(presiderItems, (newGroups) => {
    selectedPresiders.value = newGroups.flatMap((g) =>
      g.items.map((p) => p.value)
    );
  });

  const clearAllFilters = () => {
    selectedLocations.value = [];
    selectedPresiders.value = [];
  };
</script>
<style scoped>
  .cc-filters {
    display: flex;
    align-items: center;
  }
  .clearAll {
    text-decoration: underline !important;
  }
</style>
