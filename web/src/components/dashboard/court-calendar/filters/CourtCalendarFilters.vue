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
    <ActivityClassFilter v-model="selectedActivityClass" />
    <v-btn
      class="clearAll"
      v-if="selectedLocations?.length > 0 || selectedActivityClass !== 'all'"
      hide-details
      @click="clearAllFilters"
    >
      Clear All
    </v-btn>
  </div>
</template>
<script setup lang="ts">
  import { ItemGroup, Presider } from '@/types';
  import { LocationInfo } from '@/types/courtlist';
  import { computed, watch } from 'vue';
  import FilterDropdown from './FilterDropdown.vue';
  import FilterDropdownGrouped from './FilterDropdownGrouped.vue';
  import ActivityClassFilter from './ActivityClassFilter.vue';

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

  const selectedActivityClass = defineModel<string>('selectedActivityClass', {
    default: 'all',
  });

  const locationItems = computed(() =>
    props.locations.map((location) => ({
      value: location.locationId,
      text: location.shortName,
    }))
  );

  const presiderItems = computed<ItemGroup[]>(() => {
    const grouped = new Map<string, ItemGroup>();
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
    selectedActivityClass.value = 'all';
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
