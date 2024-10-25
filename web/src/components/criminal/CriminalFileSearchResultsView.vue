<template>
  <b-card bg-variant="white" no-body>
    <div>
      <h2 class="mx-4 mt-5 font-weight-normal text-criminal ">Criminal</h2>
      <custom-overlay :show="!loadCompleted" style="padding: 0 1rem; margin-left:auto; margin-right:0.5rem;">
        <b-button
          @click="openFiles()"
          variant="outline-primary bg-success text-white"
          style="padding:0.5rem 1.5rem; margin-left:auto; right:0; bottom: 1rem;  position: absolute;"
        >
          <b-icon-box-arrow-up-right class="mx-0 pl-0" variant="white" scale="1"></b-icon-box-arrow-up-right> Open
          Selected
        </b-button>
      </custom-overlay>
      <hr class="mx-3 bg-criminal" style="height: 5px;" />
    </div>

    <b-card bg-variant="light" v-if="isMounted && !isDataReady">
      <b-card style="min-height: 100px;">
        <span v-if="errorCode == 404"
          >This <b>File-Number '{{ this.$route.query.fileNumber }}'</b> at
          <b> location '{{ this.$route.query.location }}' </b> doesn't exist in the <b>criminal</b> records.
        </span>
        <span v-else-if="errorCode == 200 || errorCode == 204"> Bad Data in search results! </span>
        <span v-else-if="errorCode == 403"> You are not authorized to access this file. </span>
        <span v-else>
          Server is not responding. <b>({{ errorText }})</b>
        </span>
      </b-card>
      <!-- <b-card> 
                <b-button id="backToLandingPage" variant="outline-primary text-dark bg-warning" @click="navigateToLandingPage">
                    <b-icon-house-door class="mr-1 ml-0" variant="dark" scale="1" ></b-icon-house-door>
                    Return to Main Page
                </b-button>        
            </b-card> -->
    </b-card>

    <b-card bg-variant="light" v-if="!isMounted && !isDataReady">
      <b-overlay :show="true">
        <b-card style="min-height: 100px;" />
        <template v-slot:overlay>
          <div>
            <loading-spinner />
            <p id="loading-label">Loading ...</p>
          </div>
        </template>
      </b-overlay>
    </b-card>

    <b-card bg-variant="white" v-if="isDataReady" no-body class="mx-3" style="overflow:auto">
      <b-table :items="SortedList" :fields="fields" borderless striped responsive="sm" :tbody-tr-class="rowClass">
        <template v-for="(field, index) in fields" v-slot:[`head(${field.key})`]="data">
          <h3 v-bind:key="index">{{ data.label }}</h3>
        </template>

        <template v-slot:head(select)>
          <b-form-checkbox class="m-0" v-model="allFilesChecked" @change="checkAllFiles" size="sm" />
        </template>

        <template v-slot:cell(select)="data">
          <b-form-checkbox size="sm" class="m-0" v-model="data.item.isChecked" @change="toggleSelectedFiles" />
        </template>

        <template v-slot:cell(fileNumber)="data">
          <b-button
            :style="data.field.cellStyle"
            size="sm"
            @click="OpenCriminalFilePage(data.item.fileId)"
            variant="outline-primary text-criminal"
            class="mr-2"
          >
            {{ data.value }}
          </b-button>
        </template>
        <template v-slot:cell(participants)="data">
          <span
            v-for="(participant, index) in data.value"
            v-bind:key="index"
            :style="data.field.cellStyle"
            v-b-tooltip.hover.top.html="participant.charge"
          >
            {{ participant.name }} <br />
          </span>
        </template>

        <template v-slot:cell(nextAppearance)="data">
          <span :style="data.field.cellStyle">
            {{ data.value | beautify_date }}
          </span>
        </template>
      </b-table>

      <!-- <b-card class="mb-5" align="right">         
                <b-button id="backToLandingPage" variant="outline-primary text-dark bg-warning" @click="navigateToLandingPage">
                    <b-icon-house-door class="mr-1 ml-0" variant="dark" scale="1" ></b-icon-house-door>
                    Return to Main Page
                </b-button>
            </b-card> -->
    </b-card>
  </b-card>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import * as _ from "underscore";
import { criminalFileInformationType, fileSearchCriminalInfoType, participantInfoType } from "@/types/criminal";
import { InputNamesType } from "@/types/common";
import "@store/modules/CommonInformation";
const commonState = namespace("CommonInformation");
import "@store/modules/CriminalFileInformation";
const criminalState = namespace("CriminalFileInformation");
import CustomOverlay from "../CustomOverlay.vue";

enum CourtLevel {
  "P" = "Provincial",
  "S" = "Supreme",
}

@Component({
  components: {
    CustomOverlay,
  },
})
export default class CriminalFileSearchResultsView extends Vue {
  @criminalState.State
  public criminalFileInformation!: criminalFileInformationType;

  @criminalState.Action
  public UpdateCriminalFile!: (newCriminalFileInformation: criminalFileInformationType) => void;

  @commonState.State
  public displayName!: string;

  @commonState.Action
  public UpdateDisplayName!: (newInputNames: InputNamesType) => void;

  criminalList: fileSearchCriminalInfoType[] = [];
  isMounted = false;
  isDataReady = false;
  loadCompleted = true;
  errorCode = 0;
  errorText = "";
  allFilesChecked = false;
  selectedFiles: string[] = [];

  fields = [
    {
      key: "select",
      label: "",
      tdClass: "border-top",
      cellStyle: "font-size: 16px;",
      sortable: false,
      headerStyle: "text-primary",
      thClass: "",
    },
    {
      key: "fileNumber",
      label: "File Number",
      tdClass: "border-top",
      cellStyle: "font-size:16px; font-weight: bold; border: none;",
    },
    { key: "participants", label: "Participants", tdClass: "border-top", cellStyle: "white-space: pre-line" },
    { key: "nextAppearance", label: "Next Appearance", tdClass: "border-top", cellStyle: "white-space: pre-line" },
  ];

  mounted() {
    this.getList();
    this.loadCompleted = true;
  }

  public getList(): void {
    this.$http
      .get("api/files/criminal?location=" + this.$route.query.location + "&fileNumber=" + this.$route.query.fileNumber)
      .then(
        (Response) => Response.json(),
        (err) => {
          this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
            title: "An error has occured.",
            variant: "danger",
            autoHideDelay: 10000,
          });
          this.errorCode = err.status;
          this.errorText = err.statusText;
          console.log(err);
          this.isMounted = true;
        }
      )
      .then((data) => {
        if (data) {
          if (data.length > 1) {
            // console.log(data)
            for (const criminalListIndex in data) {
              const criminalListInfo = {} as fileSearchCriminalInfoType;
              const jcriminalList = data[criminalListIndex];
              const participantInfo: participantInfoType[] = [];
              for (const participant of jcriminalList.participant) {
                const firstName = participant.givenNm.trim().length > 0 ? participant.givenNm : "";
                const lastName = participant.lastNm ? participant.lastNm : participant.orgNm;
                this.UpdateDisplayName({ lastName: lastName, givenName: firstName });
                const charges: string[] = [];
                for (const charge of participant.charge) {
                  const chargeDesc = charge.sectionDscTxt ? charge.sectionDscTxt : "";
                  if (chargeDesc.length > 0) charges.push(chargeDesc);
                }
                participantInfo.push({ name: this.displayName, charge: charges.toString() });
              }
              criminalListInfo.participants = participantInfo;
              criminalListInfo.fileNumber = jcriminalList.fileNumberTxt;
              criminalListInfo.fileId = jcriminalList.justinNo;
              criminalListInfo.nextAppearance = jcriminalList.nextApprDt;
              const currentDate = new Date();
              criminalListInfo.today = currentDate == new Date(jcriminalList.nextApprDt);
              criminalListInfo.level = CourtLevel[jcriminalList.courtLevelCd];
              this.criminalList.push(criminalListInfo);
            }
            if (this.criminalList.length) {
              this.isDataReady = true;
            }
            this.isMounted = true;
          } else if (data.length == 1) {
            this.criminalFileInformation.fileNumber = data[0].justinNo;
            this.UpdateCriminalFile(this.criminalFileInformation);
            this.$router.push({
              name: "CriminalCaseDetails",
              params: { fileNumber: this.criminalFileInformation.fileNumber },
            });
          }
        }
      });
  }

  public OpenCriminalFilePage(fileNumber) {
    this.criminalFileInformation.fileNumber = fileNumber;
    this.UpdateCriminalFile(this.criminalFileInformation);
    const routeData = this.$router.resolve({
      name: "CriminalCaseDetails",
      params: { fileNumber: this.criminalFileInformation.fileNumber },
    });
    window.open(routeData.href, "_blank");
  }

  public openFiles() {
    this.loadCompleted = false;
    for (const file of this.SortedList) {
      if (file.isChecked) {
        this.OpenCriminalFilePage(file.fileId);
      }
    }
    this.loadCompleted = true;
  }

  public checkAllFiles(checked) {
    for (const docInx in this.SortedList) {
      this.SortedList[docInx].isChecked = checked;
    }
  }

  public toggleSelectedFiles() {
    Vue.nextTick(() => {
      const checkedDocs = this.SortedList.filter((file) => {
        return file.isChecked;
      });

      if (checkedDocs.length == this.SortedList.length) this.allFilesChecked = true;
      else this.allFilesChecked = false;
    });
  }

  get SortedList() {
    return _.sortBy(this.criminalList, "nextAppearance").reverse();
  }

  public rowClass(item, type) {
    if (!item || type !== "row") return;
    if (item.today) return "table-warning";
  }

  public navigateToLandingPage() {
    this.$router.push({ name: "Home" });
  }
}
</script>

<style scoped>
.card {
  border: white;
}
</style>
