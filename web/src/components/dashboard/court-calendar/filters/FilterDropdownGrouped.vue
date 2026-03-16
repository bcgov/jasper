<template>
  <FilterDropdownBase
    :title="title"
    :selected-count="modelValue.length"
    :show-search="showSearch"
    v-slot="{ menu, searchQuery }"
    @vue:mounted="onMounted"
  >
    <v-list max-height="400">
      <template v-for="group in filteredGroups(searchQuery)" :key="group.label">
        <h6 class="pa-2 mb-0">{{ group.label }}</h6>

        <!-- group children -->
        <v-list-item
          v-for="item in group.items"
          :key="item.value"
          :title="item.text"
          class="pl-3"
          @click="toggleItem(item.value)"
        >
          <template #prepend>
            <v-checkbox-btn
              :model-value="modelValue.includes(item.value)"
              hide-details
              @click.stop
              @update:model-value="toggleItem(item.value)"
            />
          </template>
        </v-list-item>
      </template>
    </v-list>
  </FilterDropdownBase>
</template>

<script setup lang="ts">
  import { ItemGroup } from '@/types';
  import FilterDropdownBase from './FilterDropdownBase.vue';

  const props = withDefaults(
    defineProps<{
      title: string;
      groups: ItemGroup[];
      modelValue: string[];
      showSearch?: boolean;
    }>(),
    {
      showSearch: true,
    }
  );

  const emit = defineEmits<{
    'update:modelValue': [value: string[]];
  }>();

  const onMounted = () => {};

  const filteredGroups = (searchQuery: string): ItemGroup[] => {
    if (!searchQuery) return props.groups;
    const query = searchQuery.toLowerCase();
    return props.groups
      .map((group) => ({
        ...group,
        items: group.items.filter((item) =>
          item.text.toLowerCase().includes(query)
        ),
      }))
      .filter((group) => group.items.length > 0);
  };

  const isGroupSelected = (group: ItemGroup): boolean =>
    group.items.every((item) => props.modelValue.includes(item.value));

  const isGroupIndeterminate = (group: ItemGroup): boolean => {
    const selectedCount = group.items.filter((item) =>
      props.modelValue.includes(item.value)
    ).length;
    return selectedCount > 0 && selectedCount < group.items.length;
  };

  const toggleGroup = (group: ItemGroup) => {
    const allSelected = isGroupSelected(group);
    const groupValues = group.items.map((i) => i.value);

    const newSelection = allSelected
      ? props.modelValue.filter((v) => !groupValues.includes(v))
      : [...new Set([...props.modelValue, ...groupValues])];

    emit('update:modelValue', newSelection);
  };

  const toggleItem = (itemValue: string) => {
    const newSelection = [...props.modelValue];
    const index = newSelection.indexOf(itemValue);
    if (index > -1) {
      newSelection.splice(index, 1);
    } else {
      newSelection.push(itemValue);
    }
    emit('update:modelValue', newSelection);
  };
</script>

<style scoped>
  .group-header {
    font-weight: 600;
    font-size: 0.8rem;
    display: flex;
    align-items: center;
    gap: 0.25rem;
  }
</style>
