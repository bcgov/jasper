<template>
  <div class="cc-filters mb-2">
    <FilterDropdown
      title="Locations"
      :items="locationItems"
      v-model="selectedLocations"
    />
    <FilterDropdownGrouped
      v-if="selectedLocations?.length > 0 && isPresidersView"
      title="Presiders"
      :groups="presiderItems"
      v-model="selectedPresiders"
    />
    <ActivityClassFilter
      v-model="selectedActivityClass"
      v-if="isPresidersView"
    />
    <v-btn
      class="clearAll"
      v-if="showClearAll"
      hide-details
      @click="clearAllFilters"
    >
      Clear All
    </v-btn>
    <v-btn-toggle
      v-model="isPresidersView"
      mandatory
      density="compact"
      rounded="pill"
      v-if="canToggleView"
      class="ml-auto mx-2 border border-secondary"
    >
      <v-btn :value="true">Presiders</v-btn>
      <v-btn :value="false">Activities</v-btn>
    </v-btn-toggle>
  </div>
</template>
<script setup lang="ts">
  import { ItemGroup, Presider } from '@/types';
  import { RolesEnum } from '@/types/common';
  import { LocationInfo } from '@/types/courtlist';
  import { hasRole } from '@/utils/utils';
  import { computed } from 'vue';
  import ActivityClassFilter from './ActivityClassFilter.vue';
  import FilterDropdown from './FilterDropdown.vue';
  import FilterDropdownGrouped from './FilterDropdownGrouped.vue';

  const allowedRolesForViewToggle = [
    RolesEnum.Raj,
    RolesEnum.AcjChiefJudge,
    RolesEnum.PoManager,
    RolesEnum.Admin,
  ];

  const props = defineProps<{
    isLocationFilterLoading: boolean;
    locations: LocationInfo[];
    presiders: Presider[];
    judgeHomeLocationId: string;
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

  const isPresidersView = defineModel<boolean>('isPresidersView', {
    default: true,
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
        )?.shortName || 'Unknown';
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

  const clearAllFilters = () => {
    selectedLocations.value = [props.judgeHomeLocationId];
    selectedPresiders.value = [];
    selectedActivityClass.value = 'all';
  };

  // Only show toggle if user has access to both views
  const canToggleView = computed(() => hasRole(allowedRolesForViewToggle));

  const showClearAll = computed(
    () =>
      selectedLocations.value.length > 1 ||
      (selectedLocations.value.length === 1 &&
        selectedLocations.value[0] !== props.judgeHomeLocationId) ||
      selectedPresiders.value.length > 0 ||
      selectedActivityClass.value !== 'all'
  );
</script>
<style scoped>
  .cc-filters {
    display: flex;
    align-items: center;
  }
  .clearAll {
    text-decoration: underline !important;
  }
  .view-toggle {
    border: 1px solid rgba(0, 0, 0, 0.12);
  }

  :deep(.v-btn.v-btn--active) {
    background-color: var(--bg-blue-800);
    color: var(--text-white-500);
  }
</style>
