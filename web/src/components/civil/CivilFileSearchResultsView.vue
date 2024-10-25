<template>
  <b-card bg-variant="white" no-body>
    <div>
      <h2 class="mx-4 mt-5 font-weight-normal text-civil ">Civil</h2>
      <custom-overlay :show="!loadCompleted" style="padding: 0 1rem; margin-left:auto; margin-right:0.5rem;">
        <b-button
          @click="openFiles()"
          variant="outline-primary bg-success text-white"
          style="padding:0.5rem 1.5rem; margin-right:0.0rem; margin-left:auto; right:0; bottom: 1rem;  position: absolute;"
        >
          <b-icon-box-arrow-up-right class="mx-0" variant="white" scale="1"></b-icon-box-arrow-up-right> Open Selected
        </b-button>
      </custom-overlay>
      <hr class="mx-3 bg-civil" style="height: 5px;" />
    </div>

    <b-card bg-variant="light" v-if="isMounted && !isDataReady">
      <b-card style="min-height: 100px;">
        <span v-if="errorCode == 404"
          >This <b>File-Number '{{ this.$route.query.fileNumber }}'</b> at
          <b> location '{{ this.$route.query.location }}' </b> doesn't exist in the <b>civil</b> records.
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
      <b-table :items="SortedList" :fields="fields" borderless striped responsive="sm">
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
            @click="OpenCivilFilePage(data.item.fileId)"
            variant="outline-primary border-white text-civil"
            class="mr-2"
          >
            {{ data.value }}
          </b-button>
        </template>
        <template v-slot:cell(parties)="data">
          <span v-for="(party, index) in data.value" v-bind:key="index" :style="data.field.cellStyle">
            {{ party }} <br />
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
            </b-card>   -->
    </b-card>
  </b-card>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import * as _ from "underscore";
import { civilFileInformationType, fileSearchCivilInfoType } from "@/types/civil";
import { InputNamesType } from "@/types/common";
import "@store/modules/CommonInformation";
const commonState = namespace("CommonInformation");
import "@store/modules/CivilFileInformation";
const civilState = namespace("CivilFileInformation");
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
export default class CivilFileSearchResultsView extends Vue {
  @civilState.State
  public civilFileInformation!: civilFileInformationType;

  @civilState.Action
  public UpdateCivilFile!: (newCivilFileInformation: civilFileInformationType) => void;

  @commonState.State
  public displayName!: string;

  @commonState.Action
  public UpdateDisplayName!: (newInputNames: InputNamesType) => void;

  civilList: fileSearchCivilInfoType[] = [];
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
    { key: "parties", label: "Parties", tdClass: "border-top", cellStyle: "white-space: pre-line" },
    { key: "nextAppearance", label: "Next Appearance", tdClass: "border-top", cellStyle: "white-space: pre-line" },
    { key: "level", label: "Level", tdClass: "border-top" },
  ];

  mounted() {
    this.getList();
    this.loadCompleted = true;
  }

  public getList(): void {
    this.errorCode = 0;
    this.$http
      .get("api/files/civil?location=" + this.$route.query.location + "&fileNumber=" + this.$route.query.fileNumber)
      .then(
        (Response) => Response.json(),
        (err) => {
          this.errorCode = err.status;
          this.errorText = err.statusText;
          this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
            title: "An error has occured.",
            variant: "danger",
            autoHideDelay: 10000,
          });
          console.log(err);
          this.isMounted = true;
        }
      )
      .then((data) => {
        if (data) {
          if (data.length > 1) {
            console.log(data);
            for (const civilListIndex in data) {
              const civilListInfo = {} as fileSearchCivilInfoType;
              const jcivilList = data[civilListIndex];
              const partyInfo: string[] = [];
              const leftRole = jcivilList.leftRoleDsc;
              const rightRole = jcivilList.rightRoleDsc;
              for (const jParty of jcivilList.party) {
                const firstName = jParty.givenNm ? jParty.givenNm : "";
                const lastName = jParty.lastNm ? jParty.lastNm : jParty.orgNm;
                this.UpdateDisplayName({ lastName: lastName, givenName: firstName });
                const roleDsc = jParty.leftRightCd == "R" ? rightRole : leftRole;
                partyInfo.push(this.displayName + " (" + roleDsc + ")");
              }
              civilListInfo.parties = partyInfo;
              civilListInfo.fileId = jcivilList.physicalFileId;
              civilListInfo.fileNumber = jcivilList.fileNumberTxt;
              civilListInfo.nextAppearance = jcivilList.NextApprDt;
              civilListInfo.level = CourtLevel[jcivilList.courtLevelCd];
              this.civilList.push(civilListInfo);
            }

            if (this.civilList.length) {
              this.isDataReady = true;
            }
            this.isMounted = true;
          } else if (data.length == 1) {
            this.civilFileInformation.fileNumber = data[0].physicalFileId;
            this.UpdateCivilFile(this.civilFileInformation);
            this.$router.push({
              name: "CivilCaseDetails",
              params: { fileNumber: this.civilFileInformation.fileNumber },
            });
          } else {
            this.errorCode = 200;
          }
        } else {
          if (this.errorCode == 0) this.errorCode = 200;
        }
      });
  }

  public OpenCivilFilePage(fileNumber) {
    this.civilFileInformation.fileNumber = fileNumber;
    this.UpdateCivilFile(this.civilFileInformation);
    const routeData = this.$router.resolve({
      name: "CivilCaseDetails",
      params: { fileNumber: this.civilFileInformation.fileNumber },
    });
    window.open(routeData.href, "_blank");
  }

  public openFiles() {
    this.loadCompleted = false;
    for (const file of this.SortedList) {
      if (file.isChecked) {
        this.OpenCivilFilePage(file.fileId);
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
    return _.sortBy(this.civilList, "fileId");
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
