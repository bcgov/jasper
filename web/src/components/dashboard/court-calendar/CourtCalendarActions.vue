<template>
  <div class="d-flex align-center">
    <v-date-input
      class="cc-date-picker mx-3"
      hide-details
      label=""
      density="compact"
      v-model="selectedDate"
    >
      <template v-slot:default="">
        <v-icon :icon="mdiCalendarMonthOutline" />
      </template>
    </v-date-input>
    <v-menu v-model="menuOpen" location="bottom end">
      <template #activator="{ props }">
        <v-btn v-bind="props" variant="outlined" density="default">
          <component
            class="mr-2"
            :is="selectedView.icon"
            width="24"
            height="24"
          />
          <span>{{ selectedView.label }}</span>
          <v-icon :icon="mdiChevronDown" v-bind="props" />
        </v-btn>
      </template>

      <v-list>
        <v-list-item
          v-for="(option, i) in options"
          :key="i"
          @click="selectOption(option)"
        >
          <v-list-item-title class="d-flex align-center">
            <component :is="option.icon" class="mr-2" width="24" height="24" />
            {{ option.label }}
          </v-list-item-title>
        </v-list-item>
      </v-list>
    </v-menu>
  </div>
</template>
<script setup lang="ts">
  import MonthIcon from '@/assets/month.svg';
  import OneWeekIcon from '@/assets/one-week.svg';
  import TwoWeekIcon from '@/assets/two-week.svg';
  import { mdiCalendarMonthOutline, mdiChevronDown } from '@mdi/js';
  import { ref } from 'vue';

  const selectedDate = defineModel<Date>();

  const options = [
    { label: 'Month', icon: MonthIcon },
    { label: '2 Week', icon: TwoWeekIcon },
    { label: 'Week', icon: OneWeekIcon },
  ];

  const selectedView = ref(options[0]);
  const menuOpen = ref(false);

  const selectOption = (option) => {
    selectedView.value = option;
    menuOpen.value = false;
  };
</script>
<style scoped>
  :deep(.cc-date-picker .v-field__field) {
    width: 175px !important;
  }

  :deep(.cc-date-picker .v-input__prepend) {
    display: none;
  }

  .two-week-svg {
    width: 22px;
    height: 22px;
  }
</style>
