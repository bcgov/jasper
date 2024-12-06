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
    <div class="mt-4">
      <!-- <b-button variant="outline-primary text-dark bg-warning" @click="navigateToHomePage()">
            <b-icon-house-door class="mr-1 ml-0" variant="dark" scale="1" ></b-icon-house-door>
            Return to Main Page
        </b-button> -->
    </div>
  </div>
</template>

<script lang="ts">
  import { useCriminalFileStore } from '@/stores';
  import { defineComponent } from 'vue';
  //  import { useRouter } from 'vue-router';

  export default defineComponent({
    setup() {
      const criminalFileStore = useCriminalFileStore();
      //      const router = useRouter();

      const panelItems = [
        'Case Details',
        'Future Appearances',
        'Past Appearances',
        'Witnesses',
        'Documents',
        'Sentence/Order Details',
      ];

      const SelectPanelItem = (panelItem) => {
        const sections = criminalFileStore.showSections;

        for (const item of panelItems) {
          if (item == panelItem) sections[item] = true;
          else sections[item] = false;
        }

        criminalFileStore.updateShowSections(sections);
      };

      // const navigateToHomePage = () => {
      //   router.push({ name: 'Home' });
      // };

      return {
        panelItems,
        SelectPanelItem,
      };
    },
  });
</script>
