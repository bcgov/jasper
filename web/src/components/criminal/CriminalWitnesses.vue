<template>
  <b-card bg-variant="white">
    <b-card style="height: 50px; background-color:#f0f0f0">
      <b-dropdown variant="light" :text="selectedType" style="position: absolute; top: 6px; bottom: 6px; left: 6px;">
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
      <b-col class="mt-4" md="8" cols="8" style="overflow: auto;" v-if="!(filteredWitnessList.length > 0)">
        <span class="text-muted" v-if="!(witnessList.length > 0)"> No witnesses. </span>
        <span class="text-muted" v-if="witnessList.length > 0 && !(filteredWitnessList.length > 0)">
          No witnesses in this category.
        </span>
      </b-col>
      <b-col class="mt-3" md="8" cols="8" style="overflow: auto;" v-if="filteredWitnessList.length > 0">
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
          <template v-for="(field, index) in witnessFields" v-slot:[`head(${field.key})`]="data">
            <b v-bind:key="index" :class="field.headerStyle"> {{ data.label }}</b>
          </template>
          <template v-slot:cell(name)="data">
            <span> {{ data.value }} </span>
            <span v-if="data.item.agency">
              <br />
              ({{ data.item.agency }}: {{ data.item.pinCode }})
            </span>
          </template>

          <template v-slot:cell(required)="data">
            <b-badge class="text-white bg-danger font-weight-bold" :style="data.field.cellStyle">
              {{ data.value }}
            </b-badge>
          </template>
        </b-table>
      </b-col>
      <b-col class="mt-4" col md="4" cols="4" style="overflow: auto;">
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
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import "@store/modules/CriminalFileInformation";
import "@store/modules/CommonInformation";
import { witnessListInfoType, witnessCountInfoType, criminalFileInformationType } from "@/types/criminal";
import { InputNamesType } from "@/types/common";
import { witnessType } from "@/types/criminal/jsonTypes";
const criminalState = namespace("CriminalFileInformation");
const commonState = namespace("CommonInformation");

@Component
export default class CriminalWitnesses extends Vue {
  @commonState.State
  public displayName!: string;

  @criminalState.State
  public criminalFileInformation!: criminalFileInformationType;

  @commonState.Action
  public UpdateDisplayName!: (newInputNames: InputNamesType) => void;

  witnessList: witnessListInfoType[] = [];
  witnessCounts: witnessCountInfoType[] = [];

  isMounted = false;
  witnessesJson: witnessType[] = [];
  numberOfTotalWitnesses = 0;
  numberOfPersonnelWitnesses = 0;
  numberOfCivilianWitnesses = 0;
  numberOfExpertWitnesses = 0;
  sortBy = "name";
  sortDesc = false;
  selectedType = "Required Only";

  witnessFields = [
    {
      key: "name",
      label: "Name",
      sortable: true,
      tdClass: "border-top text-danger",
      headerStyle: "text-primary",
      cellStyle: "",
    },
    {
      key: "type",
      label: "Type",
      sortable: true,
      tdClass: "border-top",
      headerStyle: "text-primary",
      cellStyle: "text",
    },
    {
      key: "required",
      label: "Required",
      sortable: true,
      tdClass: "border-top",
      headerStyle: "text-primary",
      cellStyle: "font-weight: normal; font-size:16px",
    },
  ];

  witnessCountsFields = [
    { key: "witnessCountFieldName", tdClass: "border-top", label: "Witness Count Field Name" },
    { key: "witnessCountValue", tdClass: "border-top", label: "Witness Count Value" },
  ];

  witnessDropDownFields = ["All Witnesses", "Required Only", "Personnel Only", "Civilian Only", "Expert Only"];

  mounted() {
    this.getWitnesses();
  }

  public getWitnesses(): void {
    const data = this.criminalFileInformation.detailsData;
    this.witnessesJson = data.witness;
    this.ExtractWitnessInfo();
    this.isMounted = true;
  }

  public ExtractWitnessInfo(): void {
    for (const witnessIndex in this.witnessesJson) {
      const witnessInfo = {} as witnessListInfoType;
      const jWitness = this.witnessesJson[witnessIndex];

      witnessInfo.firstName = jWitness.givenNm ? jWitness.givenNm : "";
      witnessInfo.lastName = jWitness.lastNm ? jWitness.lastNm : "";
      this.UpdateDisplayName({ lastName: witnessInfo.lastName, givenName: witnessInfo.firstName });
      witnessInfo.name = this.displayName;
      witnessInfo.type = jWitness.witnessTypeDsc ? jWitness.witnessTypeDsc : "";
      witnessInfo.required = jWitness.requiredYN == "Y" ? "Required" : "";
      witnessInfo.agency = jWitness.agencyDsc ? jWitness.agencyDsc : "";
      witnessInfo.pinCode = jWitness.pinCodeTxt ? jWitness.pinCodeTxt : "";
      if (jWitness.witnessTypeCd) {
        if (jWitness.witnessTypeCd == "PO" || jWitness.witnessTypeCd == "PRO") {
          this.numberOfPersonnelWitnesses += 1;
          witnessInfo.typeCategory = "Personnel";
        } else if (jWitness.witnessTypeCd == "CIV") {
          this.numberOfCivilianWitnesses += 1;
          witnessInfo.typeCategory = "Civilian";
        } else if (jWitness.witnessTypeCd == "EXP") {
          this.numberOfExpertWitnesses += 1;
          witnessInfo.typeCategory = "Expert";
        }
      }
      this.witnessList.push(witnessInfo);
    }
    this.numberOfTotalWitnesses = this.witnessList.length;
    let countInfo = {} as witnessCountInfoType;
    countInfo.witnessCountFieldName = "Personnel Witnesses";
    countInfo.witnessCountValue = this.numberOfPersonnelWitnesses;
    this.witnessCounts.push(countInfo);
    countInfo = {} as witnessCountInfoType;
    countInfo.witnessCountFieldName = "Civilian Witnesses";
    countInfo.witnessCountValue = this.numberOfCivilianWitnesses;
    this.witnessCounts.push(countInfo);
    countInfo = {} as witnessCountInfoType;
    countInfo.witnessCountFieldName = "Expert Witnesses";
    countInfo.witnessCountValue = this.numberOfExpertWitnesses;
    this.witnessCounts.push(countInfo);
    countInfo = {} as witnessCountInfoType;
    countInfo.witnessCountFieldName = "Total";
    countInfo.witnessCountValue = this.numberOfTotalWitnesses;
    this.witnessCounts.push(countInfo);
  }

  public totalBackground(item) {
    if (item.WitnessCountFieldName == "Total") {
      return "table-warning";
    }
    return;
  }

  get filteredWitnessList() {
    return this.witnessList.filter((witness) => {
      if (witness.required == "Required" && this.selectedType == "Required Only") {
        return true;
      } else if (witness.typeCategory == "Personnel" && this.selectedType == "Personnel Only") {
        return true;
      } else if (witness.typeCategory == "Civilian" && this.selectedType == "Civilian Only") {
        return true;
      } else if (witness.typeCategory == "Expert" && this.selectedType == "Expert Only") {
        return true;
      } else if (this.selectedType == "All Witnesses") {
        return true;
      }
    });
  }
}
</script>

<style scoped>
.card {
  border: white;
}
</style>
