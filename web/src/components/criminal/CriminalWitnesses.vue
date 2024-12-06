<template>
  <b-card bg-variant="white">
    <b-card style="height: 50px; background-color: #f0f0f0">
      <b-dropdown
        variant="light"
        :text="selectedType"
        style="position: absolute; top: 6px; bottom: 6px; left: 6px"
      >
        <b-dropdown-item-button
          v-for="(witnessType, index) in witnessDropDownFields"
          :key="index"
          v-on:click="selectedType = witnessType"
        >
          {{ witnessDropDownFields[index] }}
        </b-dropdown-item-button>
      </b-dropdown>
    </b-card>
    <b-row cols="2">
      <b-col
        class="mt-4"
        md="8"
        cols="8"
        style="overflow: auto"
        v-if="!(filteredWitnessList.length > 0)"
      >
        <span class="text-muted" v-if="!(witnessList.length > 0)">
          No witnesses.
        </span>
        <span
          class="text-muted"
          v-if="witnessList.length > 0 && !(filteredWitnessList.length > 0)"
        >
          No witnesses in this category.
        </span>
      </b-col>
      <b-col
        class="mt-3"
        md="8"
        cols="8"
        style="overflow: auto"
        v-if="filteredWitnessList.length > 0"
      >
        <b-table
          :items="filteredWitnessList"
          :fields="witnessFields"
          :sort-by.sync="sortBy"
          :sort-desc.sync="sortDesc"
          :no-sort-reset="true"
          borderless
          sort-icon-left
          responsive="sm"
        >
          <template
            v-for="(field, index) in witnessFields"
            v-slot:[`head(${field.key})`]="data"
          >
            <b v-bind:key="index" :class="field.headerStyle">
              {{ data.label }}</b
            >
          </template>
          <template v-slot:cell(name)="data">
            <span> {{ data.value }} </span>
            <span v-if="data.item.agency">
              <br />
              ({{ data.item.agency }}: {{ data.item.pinCode }})
            </span>
          </template>

          <template v-slot:cell(required)="data">
            <b-badge
              class="text-white bg-danger font-weight-bold"
              :style="data.field.cellStyle"
            >
              {{ data.value }}
            </b-badge>
          </template>
        </b-table>
      </b-col>
      <b-col class="mt-4" col md="4" cols="4" style="overflow: auto">
        <h4 class="font-weight-bold">Witness Counts</h4>

        <b-table
          :items="witnessCounts"
          :fields="witnessCountsFields"
          thead-class="d-none"
          responsive="sm"
          borderless
          :tbody-tr-class="totalBackground"
        >
          <template v-slot:cell(witnessCountValue)="data">
            <span>
              <b> {{ data.value }}</b>
            </span>
          </template>
        </b-table>
      </b-col>
    </b-row>
  </b-card>
</template>

<script lang="ts">
  import { useCommonStore, useCriminalFileStore } from '@/stores';
  import { witnessCountInfoType, witnessListInfoType } from '@/types/criminal';
  import { witnessType } from '@/types/criminal/jsonTypes';
  import { computed, defineComponent, onMounted, ref } from 'vue';

  export default defineComponent({
    setup() {
      const commonStore = useCommonStore();
      const criminalFileStore = useCriminalFileStore();

      const witnessList = ref<witnessListInfoType[]>([]);
      const witnessCounts = ref<witnessCountInfoType[]>([]);

      const isMounted = ref(false);
      let witnessesJson: witnessType[] = [];

      let numberOfTotalWitnesses = 0;
      let numberOfPersonnelWitnesses = 0;
      let numberOfCivilianWitnesses = 0;
      let numberOfExpertWitnesses = 0;
      const sortBy = ref('name');
      const sortDesc = ref(false);
      const selectedType = ref('Required Only');

      const witnessFields = [
        {
          key: 'name',
          label: 'Name',
          sortable: true,
          tdClass: 'border-top text-danger',
          headerStyle: 'text-primary',
          cellStyle: '',
        },
        {
          key: 'type',
          label: 'Type',
          sortable: true,
          tdClass: 'border-top',
          headerStyle: 'text-primary',
          cellStyle: 'text',
        },
        {
          key: 'required',
          label: 'Required',
          sortable: true,
          tdClass: 'border-top',
          headerStyle: 'text-primary',
          cellStyle: 'font-weight: normal; font-size:16px',
        },
      ];

      const witnessCountsFields = [
        {
          key: 'witnessCountFieldName',
          tdClass: 'border-top',
          label: 'Witness Count Field Name',
        },
        {
          key: 'witnessCountValue',
          tdClass: 'border-top',
          label: 'Witness Count Value',
        },
      ];

      const witnessDropDownFields = [
        'All Witnesses',
        'Required Only',
        'Personnel Only',
        'Civilian Only',
        'Expert Only',
      ];

      onMounted(() => {
        getWitnesses();
      });

      const getWitnesses = () => {
        const data = criminalFileStore.criminalFileInformation.detailsData;
        witnessesJson = data.witness;
        ExtractWitnessInfo();
        isMounted.value = true;
      };

      const ExtractWitnessInfo = () => {
        for (const witnessIndex in witnessesJson) {
          const witnessInfo = {} as witnessListInfoType;
          const jWitness = witnessesJson[witnessIndex];

          witnessInfo.firstName = jWitness.givenNm ? jWitness.givenNm : '';
          witnessInfo.lastName = jWitness.lastNm ? jWitness.lastNm : '';
          commonStore.updateDisplayName({
            lastName: witnessInfo.lastName,
            givenName: witnessInfo.firstName,
          });
          witnessInfo.name = commonStore.displayName;
          witnessInfo.type = jWitness.witnessTypeDsc
            ? jWitness.witnessTypeDsc
            : '';
          witnessInfo.required = jWitness.requiredYN == 'Y' ? 'Required' : '';
          witnessInfo.agency = jWitness.agencyDsc ? jWitness.agencyDsc : '';
          witnessInfo.pinCode = jWitness.pinCodeTxt ? jWitness.pinCodeTxt : '';
          if (jWitness.witnessTypeCd) {
            if (
              jWitness.witnessTypeCd == 'PO' ||
              jWitness.witnessTypeCd == 'PRO'
            ) {
              numberOfPersonnelWitnesses += 1;
              witnessInfo.typeCategory = 'Personnel';
            } else if (jWitness.witnessTypeCd == 'CIV') {
              numberOfCivilianWitnesses += 1;
              witnessInfo.typeCategory = 'Civilian';
            } else if (jWitness.witnessTypeCd == 'EXP') {
              numberOfExpertWitnesses += 1;
              witnessInfo.typeCategory = 'Expert';
            }
          }
          witnessList.value.push(witnessInfo);
        }
        numberOfTotalWitnesses = witnessList.value.length;
        let countInfo = {} as witnessCountInfoType;
        countInfo.witnessCountFieldName = 'Personnel Witnesses';
        countInfo.witnessCountValue = numberOfPersonnelWitnesses;
        witnessCounts.value.push(countInfo);
        countInfo = {} as witnessCountInfoType;
        countInfo.witnessCountFieldName = 'Civilian Witnesses';
        countInfo.witnessCountValue = numberOfCivilianWitnesses;
        witnessCounts.value.push(countInfo);
        countInfo = {} as witnessCountInfoType;
        countInfo.witnessCountFieldName = 'Expert Witnesses';
        countInfo.witnessCountValue = numberOfExpertWitnesses;
        witnessCounts.value.push(countInfo);
        countInfo = {} as witnessCountInfoType;
        countInfo.witnessCountFieldName = 'Total';
        countInfo.witnessCountValue = numberOfTotalWitnesses;
        witnessCounts.value.push(countInfo);
      };

      const totalBackground = (item) => {
        if (item.WitnessCountFieldName == 'Total') {
          return 'table-warning';
        }
        return;
      };

      const filteredWitnessList = computed(() => {
        return witnessList.value.filter((witness) => {
          if (
            witness.required == 'Required' &&
            selectedType.value == 'Required Only'
          ) {
            return true;
          } else if (
            witness.typeCategory == 'Personnel' &&
            selectedType.value == 'Personnel Only'
          ) {
            return true;
          } else if (
            witness.typeCategory == 'Civilian' &&
            selectedType.value == 'Civilian Only'
          ) {
            return true;
          } else if (
            witness.typeCategory == 'Expert' &&
            selectedType.value == 'Expert Only'
          ) {
            return true;
          } else if (selectedType.value == 'All Witnesses') {
            return true;
          }
        });
      });

      return {
        selectedType,
        witnessDropDownFields,
        filteredWitnessList,
        witnessList,
        sortBy,
        sortDesc,
        witnessFields,
        witnessCounts,
        witnessCountsFields,
        totalBackground,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
