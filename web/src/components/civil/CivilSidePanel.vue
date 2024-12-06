<template>
  <div class="m-2">
    <br />
    <div class="mx-auto bg-white">
      <b-nav vertical>
        <b-nav-item style="font-size: 10px; font-weight: bold" disabled
          >ON THIS FILE</b-nav-item
        >
        <b-nav-item
          v-for="(panelItem, index) in panelItems"
          :key="index"
          style="line-height: 1.25"
          v-on:click="SelectPanelItem(panelItem)"
        >
          {{ panelItem }}
        </b-nav-item>
      </b-nav>
    </div>
    <!-- <div class="mt-4">
            <b-button variant="outline-primary text-dark bg-warning" @click="navigateToHomePage()">
                <b-icon-house-door class="mr-1 ml-0" variant="dark" scale="1" ></b-icon-house-door>
                Return to Main Page
            </b-button>
        </div> -->
  </div>
</template>

<script lang="ts">
  import { useCivilFileStore } from '@/stores';
  import { defineComponent } from 'vue';

  export default defineComponent({
    setup() {
      const civilFileStore = useCivilFileStore();

      const panelItems = [
        'Case Details',
        'Future Appearances',
        'Past Appearances',
        'All Documents',
        'Documents',
        'Provided Documents',
      ];

      const SelectPanelItem = (panelItem) => {
        const sections = civilFileStore.showSections;

        for (const item of panelItems) {
          if (item == panelItem) sections[item] = true;
          else sections[item] = false;
        }
        civilFileStore.updateShowSections(sections);
      };

      return {
        panelItems,
        SelectPanelItem,
      };
    },
  });
</script>
