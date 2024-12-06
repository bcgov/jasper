<template>
  <b-card bg-variant="white" no-body>
    <div>
      <h3
        class="mx-4 font-weight-normal"
        v-if="!showSections['Future Appearances']"
      >
        Next Three Future Appearances
      </h3>
      <hr class="mx-3 bg-light" style="height: 5px" />
    </div>

    <b-card v-if="!isDataReady && isMounted" no-body>
      <span class="text-muted ml-4 mb-5"> No future appearances. </span>
    </b-card>

    <b-card bg-variant="light" v-if="!isMounted && !isDataReady">
      <b-overlay :show="true">
        <b-card style="min-height: 100px" />
        <template v-slot:overlay>
          <div>
            <loading-spinner />
            <p id="loading-label">Loading ...</p>
          </div>
        </template>
      </b-overlay>
    </b-card>

    <criminal-future-appearances-table
      v-if="isDataReady"
      :SortedFutureAppearances="SortedFutureAppearances"
    />
  </b-card>
</template>

<script lang="ts">
  import CriminalAppearanceDetails from '@/components/criminal/CriminalAppearanceDetails.vue';
  import { useCriminalFileStore } from '@/stores';
  import { criminalAppearancesListType } from '@/types/criminal';
  import { criminalApprDetailType } from '@/types/criminal/jsonTypes';
  import { extractCriminalAppearanceInfo } from '@/utils/utils';
  import * as _ from 'underscore';
  import { computed, defineComponent, onMounted, ref } from 'vue';
  import CriminalFutureAppearancesTable from './CriminalFutureAppearancesTable.vue';

  export default defineComponent({
    components: {
      CriminalAppearanceDetails,
      CriminalFutureAppearancesTable,
    },
    setup() {
      const criminalFileStore = useCriminalFileStore();

      const futureAppearancesList = ref<criminalAppearancesListType[]>([]);
      let futureAppearancesJson: criminalApprDetailType[] = [];
      const isMounted = ref(false);
      const isDataReady = ref(false);

      onMounted(() => {
        getFutureAppearances();
      });

      const getFutureAppearances = () => {
        const data = criminalFileStore.criminalFileInformation.detailsData;
        futureAppearancesJson = [...data.appearances.apprDetail];
        ExtractFutureAppearancesInfo();
        if (futureAppearancesList.value.length) {
          isDataReady.value = true;
        }
        isMounted.value = true;
      };

      const ExtractFutureAppearancesInfo = () => {
        const currentDate = new Date();

        futureAppearancesJson.forEach(
          (jApp: criminalApprDetailType, index: number) => {
            const appearanceDate = jApp.appearanceDt.split(' ')[0];
            if (new Date(appearanceDate) < currentDate) return;

            const appInfo = extractCriminalAppearanceInfo(
              jApp,
              index,
              appearanceDate
            );

            futureAppearancesList.value.push(appInfo);
          }
        );
      };

      const SortedFutureAppearances = computed(
        (): criminalAppearancesListType[] => {
          if (criminalFileStore.showSections['Future Appearances']) {
            return futureAppearancesList.value;
          } else {
            return _.sortBy(futureAppearancesList.value, 'date')
              .reverse()
              .slice(0, 3);
          }
        }
      );

      return {
        showSections: criminalFileStore.showSections,
        isMounted,
        isDataReady,
        SortedFutureAppearances,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
