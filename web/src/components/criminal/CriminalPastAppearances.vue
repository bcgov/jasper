<template>
  <b-card bg-variant="white" no-body>
    <div>
      <h3
        class="mx-4 font-weight-normal"
        v-if="!showSections['Past Appearances']"
      >
        Last Three Past Appearances
      </h3>
      <hr class="mx-3 bg-light" style="height: 5px" />
    </div>

    <b-card v-if="!isDataReady && isMounted" no-body>
      <span class="text-muted ml-4 mb-5"> No past appearances. </span>
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

    <criminal-past-appearances-table
      v-if="isDataReady"
      :SortedPastAppearances="SortedPastAppearances"
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
  import CriminalPastAppearancesTable from './CriminalPastAppearancesTable.vue';

  enum appearanceStatus {
    UNCF = 'Unconfirmed',
    CNCL = 'Canceled',
    SCHD = 'Scheduled',
  }

  export default defineComponent({
    components: {
      CriminalAppearanceDetails,
      CriminalPastAppearancesTable,
    },
    setup() {
      const criminalFileStore = useCriminalFileStore();

      const pastAppearancesList = ref<criminalAppearancesListType[]>([]);
      let pastAppearancesJson: criminalApprDetailType[] = [];

      const isMounted = ref(false);
      const isDataReady = ref(false);

      onMounted(() => {
        getPastAppearances();
      });

      const getPastAppearances = () => {
        const data = criminalFileStore.criminalFileInformation.detailsData;
        pastAppearancesJson = [...data.appearances.apprDetail];
        ExtractPastAppearancesInfo();
        if (pastAppearancesList.value.length) {
          isDataReady.value = true;
        }
        isMounted.value = true;
      };

      const ExtractPastAppearancesInfo = () => {
        const currentDate = new Date();

        pastAppearancesJson.forEach(
          (jApp: criminalApprDetailType, index: number) => {
            const appearanceDate = jApp.appearanceDt.split(' ')[0];
            if (new Date(appearanceDate) >= currentDate) return;

            const appInfo = extractCriminalAppearanceInfo(
              jApp,
              index,
              appearanceDate
            );

            pastAppearancesList.value.push(appInfo);
          }
        );
      };

      const SortedPastAppearances = computed(
        (): criminalAppearancesListType[] => {
          if (criminalFileStore.showSections['Past Appearances']) {
            return pastAppearancesList.value;
          } else {
            return _.sortBy(pastAppearancesList.value, 'date')
              .reverse()
              .slice(0, 3);
          }
        }
      );

      return {
        showSections: criminalFileStore.showSections,
        isDataReady,
        isMounted,
        SortedPastAppearances,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
