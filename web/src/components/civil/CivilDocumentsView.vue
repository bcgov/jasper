<template>
  <div>
    <b-card v-if="isMounted" no-body>
      <div>
        <b-row>
          <h3 class="ml-5 my-1 p-0 font-weight-normal" v-if="!showSections['Documents']">
            Documents ({{ NumberOfDocuments }})
          </h3>
          <custom-overlay :show="!downloadCompleted" style="padding: 0 1rem; margin-left:auto; margin-right:2rem;">
            <b-button
              v-if="enableArchive"
              @click="downloadDocuments()"
              size="sm"
              variant="success"
              style="padding: 0 1rem; margin-left:auto; margin-right:2rem;"
            >
              Download Selected
            </b-button>
          </custom-overlay>
        </b-row>
        <hr class="mx-3 bg-light" style="height: 5px;" />
      </div>

      <b-tabs
        nav-wrapper-class="bg-light text-dark"
        active-nav-item-class="text-uppercase font-weight-bold text-white bg-primary"
        pills
        no-body
        class="mx-3"
      >
        <b-tab
          v-for="(tabMapping, index) in categories"
          :key="index"
          :title="tabMapping"
          v-on:click="switchTab(tabMapping)"
          v-bind:class="[activetab === tabMapping ? 'active mb-3' : 'mb-3']"
        ></b-tab>
      </b-tabs>

      <b-overlay :show="loadingPdf" rounded="sm">
        <b-card bg-variant="light" style="max-height: 500px; overflow-y: auto;" no-body class="mx-3 mb-5">
          <b-table
            :items="FilteredDocuments"
            :fields="fields[fieldsTab]"
            :sort-by="sortBy"
            :sort-desc.sync="sortDesc"
            :no-sort-reset="true"
            sort-icon-left
            small
            striped
            responsive="sm"
          >
            <template v-for="(field, index) in fields[fieldsTab]" v-slot:[`head(${field.key})`]="data">
              <b v-bind:key="index" :class="field.headerStyle"> {{ data.label }}</b>
            </template>

            <template v-slot:[`cell(${fields[fieldsTab][datePlace[fieldsTab]].key})`]="data">
              <span v-if="data.item.sealed" :style="data.field.cellStyle" class="text-muted">
                {{ data.value | beautify_date }}
              </span>
              <span v-else :style="data.field.cellStyle">
                {{ data.value | beautify_date }}
              </span>
            </template>

            <template v-slot:[`cell(${fields[fieldsTab][documentPlace[fieldsTab]].key})`]="data">
              <b-button
                v-if="data.item.pdfAvail"
                variant="outline-primary text-info"
                :style="data.field.cellStyle"
                @click="cellClick(data)"
                size="sm"
              >
                {{ data.value }}
              </b-button>
              <span class="ml-2" v-else-if="!data.item.pdfAvail && !data.item.sealed">
                {{ data.value }}
              </span>
              <span class="ml-2 text-muted" v-else-if="data.item.sealed">
                {{ data.value }}
              </span>
            </template>

            <template v-if="enableArchive" v-slot:head(select)>
              <b-form-checkbox class="m-0" v-model="allDocumentsChecked" @change="checkAllDocuments" size="sm" />
            </template>

            <template v-if="enableArchive" v-slot:cell(select)="data">
              <b-form-checkbox
                size="sm"
                class="m-0"
                :disabled="!data.item.isEnabled"
                v-model="data.item.isChecked"
                @change="toggleSelectedDocuments"
              />
            </template>

            <template v-slot:cell(act)="data">
              <b-badge
                variant="secondary"
                :style="data.field.cellStyle"
                v-for="(act, actIndex) in data.value"
                v-bind:key="actIndex"
                v-b-tooltip.hover.left
                :title="act.description"
              >
                {{ act.code }}<br />
              </b-badge>
            </template>

            <template v-slot:cell(issues)="data">
              <li v-for="(issue, issueIndex) in data.value" v-bind:key="issueIndex" :style="data.field.cellStyle">
                <span v-if="data.item.sealed" class="text-muted">{{ issue }}</span>
                <span v-else>{{ issue }}</span>
              </li>
            </template>
            <template v-slot:cell(seq)="data">
              <span v-if="data.item.sealed" class="ml-2 text-muted" :style="data.field.cellStyle">
                {{ data.value }}
              </span>
              <span v-else class="ml-2" :style="data.field.cellStyle">
                {{ data.value }}
              </span>
            </template>

            <template v-slot:cell()="data">
              <span v-if="data.field.key == 'filedByName'">
                <li v-for="(filed, filedIndex) in data.value" v-bind:key="filedIndex" :style="data.field.cellStyle">
                  {{ filed }}
                </li>
              </span>
              <span v-else class="ml-2" :style="data.field.cellStyle">
                {{ data.value }}
              </span>
            </template>
          </b-table>
        </b-card>
        <template v-slot:overlay>
          <div style="text-align: center">
            <loading-spinner />
            <p id="Downloading-label">Downloading PDF file ...</p>
          </div>
        </template>
      </b-overlay>
    </b-card>
  </div>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import base64url from "base64url";
import "@store/modules/CivilFileInformation";
import {
  civilFileInformationType,
  csrRequestsInfoType,
  documentsInfoType,
  summaryDocumentsInfoType,
} from "@/types/civil";
const civilState = namespace("CivilFileInformation");
import "@store/modules/CommonInformation";
const commonState = namespace("CommonInformation");

import CustomOverlay from "../CustomOverlay.vue";
import { ArchiveInfoType, DocumentRequestsInfoType } from "@/types/common";
import shared from "../shared";
import { CourtDocumentType, DocumentData } from "@/types/shared";

enum fieldTab {
  Categories = 0,
  Summary,
  Orders,
  Scheduled,
  Affidavits,
}

@Component({
  components: {
    CustomOverlay,
  },
})
export default class CivilDocumentsView extends Vue {
  @commonState.State
  public enableArchive!: boolean;

  @civilState.State
  public showSections;

  @civilState.State
  public civilFileInformation!: civilFileInformationType;

  @civilState.Action
  public UpdateCivilFile!: (newCivilFileInformation: civilFileInformationType) => void;

  documents: documentsInfoType[] = [];
  summaryDocuments: summaryDocumentsInfoType[] = [];
  loadingPdf = false;
  isMounted = false;
  activetab = "ALL";
  sortDesc = false;
  categories: string[] = [];
  fieldsTab = fieldTab.Categories;
  documentPlace = [2, 1, 2, 2, 2];
  datePlace = [4, 2, 3, 5, 3];
  selectedDocuments = {} as ArchiveInfoType;
  downloadCompleted = true;
  allDocumentsChecked = false;

  fields = [
    [
      {
        key: "select",
        label: "",
        sortable: false,
        headerStyle: "text-primary",
        cellStyle: "font-size: 16px;",
        tdClass: "border-top",
        thClass: "",
      },
      { key: "seq", label: "Seq.", sortable: true, headerStyle: "text-primary", cellStyle: "font-size: 14px;" },
      {
        key: "documentType",
        label: "Document Type",
        sortable: true,
        headerStyle: "text-primary",
        cellStyle: "border:0px; font-size: 14px; text-align:left;",
      },
      {
        key: "act",
        label: "Act",
        sortable: false,
        headerStyle: "text",
        cellStyle: "display: block; margin-top: 1px; font-size: 14px; max-width : 50px;",
      },
      {
        key: "dateFiled",
        label: "Date Filed",
        sortable: true,
        headerStyle: "text-danger",
        cellStyle: "font-size: 14px;",
      },
      { key: "affNo", label: "Aff No.", sortable: false, headerStyle: "text", cellStyle: "font-size: 14px;" },
      {
        key: "swornBy",
        label: "Sworn By",
        sortable: false,
        headerStyle: "text",
        cellStyle: "white-space: pre-line; font-size: 14px; margin-left: 20px",
      },
      {
        key: "issues",
        label: "Issues",
        sortable: false,
        headerStyle: "text",
        cellStyle: "white-space: pre-line; font-size: 14px; margin-left: 20px;",
      },
      {
        key: "filedByName",
        label: "Filed By Name",
        sortable: false,
        headerStyle: "text",
        cellStyle: "white-space: pre-line; font-size: 14px; margin-left: 20px",
      },
      {
        key: "comment",
        label: "Comment",
        sortable: false,
        headerStyle: "text",
        cellStyle: "font-size: 12px; max-width:300px;",
        tdClass: "max-width-300",
      },
    ],
    [
      {
        key: "select",
        label: "",
        sortable: false,
        headerStyle: "text-primary",
        cellStyle: "font-size: 16px;",
        tdClass: "border-top",
        thClass: "",
      },
      {
        key: "documentType",
        label: "Document Type",
        sortable: false,
        headerStyle: "text-primary",
        cellStyle: "border:0px; font-size: 14px;",
      },
      {
        key: "appearanceDate",
        label: "Appearance Date",
        sortable: true,
        headerStyle: "text-danger",
        cellStyle: "font-size: 14px;",
      },
    ],
    [
      {
        key: "select",
        label: "",
        sortable: false,
        headerStyle: "text-primary",
        cellStyle: "font-size: 16px;",
        tdClass: "border-top",
        thClass: "",
      },
      {
        key: "seq",
        label: "Seq.",
        sortable: true,
        headerStyle: "text-primary",
        cellStyle: "font-size: 14px;"
      },
      {
        key: "documentType",
        label: "Document Type",
        sortable: true,
        headerStyle: "text-primary",
        cellStyle: "border:0px; font-size: 14px; text-align:left;",
      },
      {
        key: "dateFiled",
        label: "Date Filed",
        sortable: false,
        headerStyle: "text-primary",
        cellStyle: "font-size: 14px;",
      },
      {
        key: "orderMadeDate",
        label: "Order Made Date",
        sortable: true,
        headerStyle: "text-primary",
        cellStyle: "font-size: 14px;",
      },
      {
        key: "filedByName",
        label: "Filed By Name",
        sortable: false,
        headerStyle: "text",
        cellStyle: "white-space: pre-line; font-size: 14px; margin-left: 20px",
      },
      {
        key: "comment",
        label: "Comment",
        sortable: false,
        headerStyle: "text",
        cellStyle: "font-size: 12px; max-width:300px;",
        tdClass: "max-width-300",
      },
    ],
    [
      {
        key: "select",
        label: "",
        sortable: false,
        headerStyle: "text-primary",
        cellStyle: "font-size: 16px;",
        tdClass: "border-top",
        thClass: "",
      },
      { key: "seq", label: "Seq.", sortable: true, headerStyle: "text-primary", cellStyle: "font-size: 14px;" },
      {
        key: "documentType",
        label: "Document Type",
        sortable: true,
        headerStyle: "text-primary",
        cellStyle: "border:0px; font-size: 14px; text-align:left;",
      },
      {
        key: "act",
        label: "Act",
        sortable: false,
        headerStyle: "text",
        cellStyle: "display: block; margin-top: 1px; font-size: 14px; max-width : 50px;",
      },
      {
        key: "nextAppearanceDate",
        label: "Next Appearance Date",
        sortable: true,
        headerStyle: "text-primary",
        cellStyle: "font-size: 14px;",
      },
      {
        key: "dateFiled",
        label: "Date Filed",
        sortable: false,
        headerStyle: "text-primary",
        cellStyle: "font-size: 14px;",
      },
      {
        key: "issues",
        label: "Issues",
        sortable: false,
        headerStyle: "text",
        cellStyle: "white-space: pre-line; font-size: 14px; margin-left: 20px;",
      },
      {
        key: "filedByName",
        label: "Filed By Name",
        sortable: false,
        headerStyle: "text",
        cellStyle: "white-space: pre-line; font-size: 14px; margin-left: 20px;",
      },
      {
        key: "comment",
        label: "Comment",
        sortable: false,
        headerStyle: "text",
        cellStyle: "font-size: 12px; max-width:300px;",
        tdClass: "max-width-300",
      },
    ],
    [
      {
        key: "select",
        label: "",
        sortable: false,
        headerStyle: "text-primary",
        cellStyle: "font-size: 16px;",
        tdClass: "border-top",
        thClass: "",
      },
      { key: "seq", label: "Seq.", sortable: true, headerStyle: "text-primary", cellStyle: "font-size: 14px;" },
      {
        key: "documentType",
        label: "Document Type",
        sortable: true,
        headerStyle: "text-primary",
        cellStyle: "border:0px; font-size: 14px; text-align:left;",
      },
      {
        key: "dateFiled",
        label: "Date Filed",
        sortable: true,
        headerStyle: "text-danger",
        cellStyle: "font-size: 14px;",
      },
      { key: "affNo", label: "Aff No.", sortable: false, headerStyle: "text", cellStyle: "font-size: 14px;" },
      {
        key: "swornBy",
        label: "Sworn By",
        sortable: false,
        headerStyle: "text",
        cellStyle: "white-space: pre-line; font-size: 14px; margin-left: 20px",
      },
      {
        key: "filedByName",
        label: "Filed By Name",
        sortable: false,
        headerStyle: "text",
        cellStyle: "white-space: pre-line; font-size: 14px; margin-left: 20px",
      },
      {
        key: "issues",
        label: "Issues",
        sortable: false,
        headerStyle: "text",
        cellStyle: "white-space: pre-line; font-size: 14px; margin-left: 20px;",
      },
      {
        key: "act",
        label: "Act",
        sortable: false,
        headerStyle: "text",
        cellStyle: "display: block; margin-top: 1px; font-size: 14px; max-width : 50px;",
      },
      {
        key: "comment",
        label: "Comment",
        sortable: false,
        headerStyle: "text",
        cellStyle: "font-size: 12px; max-width:300px;",
        tdClass: "max-width-300",
      },
    ],
  ];

  public getDocuments(): void {
    this.documents = this.civilFileInformation.documentsInfo;
    this.summaryDocuments = this.civilFileInformation.summaryDocumentsInfo;
    this.categories = this.civilFileInformation.categories;
    this.categories.sort();
    if (this.categories.indexOf("COURT SUMMARY") < 0 && this.summaryDocuments.length > 0)
      this.categories.push("COURT SUMMARY");
    if (this.categories.indexOf("ALL") < 0) this.categories.unshift("ALL");
    this.isMounted = true;
  }

  mounted() {
    this.getDocuments();
    this.downloadCompleted = true;
    this.selectedDocuments = { zipName: "", csrRequests: [], documentRequests: [], ropRequests: [] };
  }

  public switchTab(tabMapping) {
    this.allDocumentsChecked = false;
    this.activetab = tabMapping;
  }

  public cellClick(eventData) {
    this.loadingPdf = true;
    const documentType = eventData.value == "CourtSummary" ? CourtDocumentType.CSR : CourtDocumentType.Civil;
    const documentData: DocumentData = {
      appearanceDate: eventData.item.appearanceDate,
      appearanceId: eventData.item.appearanceId,
      dateFiled: eventData.item.dateFiled,
      documentDescription: eventData.item.documentType,
      documentId: eventData.item.documentId,
      fileId: this.civilFileInformation.fileNumber,
      fileNumberText: this.civilFileInformation.detailsData.fileNumberTxt,
      courtClass: this.civilFileInformation.detailsData.courtClassCd,
      courtLevel: this.civilFileInformation.detailsData.courtLevelCd,
      location: this.civilFileInformation.detailsData.homeLocationAgencyName
    };
    shared.openDocumentsPdf(documentType, documentData);
    this.loadingPdf = false;
  }

  public downloadDocuments() {
    const fileName = shared.generateFileName(CourtDocumentType.CivilZip, {
      location: this.civilFileInformation.detailsData.homeLocationAgencyName,
      courtLevel: this.civilFileInformation.detailsData.courtLevelCd,
      fileNumberText: this.civilFileInformation.detailsData.fileNumberTxt,
    });

    this.selectedDocuments = { zipName: fileName, csrRequests: [], documentRequests: [], ropRequests: [] };
    for (const doc of this.documents) {
      if (doc.isChecked && doc.isEnabled) {
        const id = doc.documentId;
        const documentRequest = {} as DocumentRequestsInfoType;
        documentRequest.isCriminal = false;
        const documentData: DocumentData = {
          courtLevel: this.civilFileInformation.detailsData.courtLevelCd,
          dateFiled: Vue.filter("beautify_date")(doc.dateFiled),
          documentDescription: doc.documentType,
          documentId: id,
          fileNumberText: this.civilFileInformation.detailsData.fileNumberTxt,
          location: this.civilFileInformation.detailsData.homeLocationAgencyName,
        };
        documentRequest.pdfFileName = shared.generateFileName(CourtDocumentType.Civil, documentData);
        documentRequest.fileId = this.civilFileInformation.fileNumber;
        documentRequest.base64UrlEncodedDocumentId = base64url(id);
        this.selectedDocuments.documentRequests.push(documentRequest);
      }
    }

    for (const doc of this.summaryDocuments) {
      if (doc.isChecked && doc.isEnabled) {
        const id = doc.appearanceId;
        const csrRequest = {} as csrRequestsInfoType;
        csrRequest.appearanceId = id;
        const documentData: DocumentData = {
          appearanceId: csrRequest.appearanceId,
          appearanceDate: Vue.filter("beautify_date")(doc.appearanceDate),
          courtLevel: this.civilFileInformation.detailsData.courtLevelCd,
          documentDescription: doc.documentType,
          fileNumberText: this.civilFileInformation.detailsData.fileNumberTxt,
          location: this.civilFileInformation.detailsData.homeLocationAgencyName,
        };
        csrRequest.pdfFileName = shared.generateFileName(CourtDocumentType.CSR, documentData);
        this.selectedDocuments.csrRequests.push(csrRequest);
      }
    }

    if (this.selectedDocuments.csrRequests.length > 0 || this.selectedDocuments.documentRequests.length > 0) {
      const options = {
        responseType: "blob",
        headers: {
          "Content-Type": "application/json",
        },
      };
      this.downloadCompleted = false;
      this.$http.post("api/files/archive", this.selectedDocuments, options).then(
        (response) => {
          const blob = response.data;
          const link = document.createElement("a");
          link.href = URL.createObjectURL(blob);
          document.body.appendChild(link);
          link.download = this.selectedDocuments.zipName;
          link.click();
          setTimeout(() => URL.revokeObjectURL(link.href), 1000);
          this.downloadCompleted = true;
        },
        (err) => {
          this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
            title: "An error has occured.",
            variant: "danger",
            autoHideDelay: 10000,
          });
          console.log(err);
          this.downloadCompleted = true;
        }
      );
    }
  }

  public checkAllDocuments(checked) {
    if (this.activetab == "COURT SUMMARY") {
      for (const docInx in this.summaryDocuments) {
        if (this.summaryDocuments[docInx].isEnabled) {
          this.summaryDocuments[docInx].isChecked = checked;
        }
      }
    } else {
      if (this.activetab == "CONCLUDED") {
        for (const docInx in this.documents) {
          if (this.documents[docInx].concluded === "Y" && this.documents[docInx].isEnabled) {
            this.documents[docInx].isChecked = checked;
          }
        }
      } else if (this.activetab == "SCHEDULED") {
        for (const docInx in this.documents) {
          if (
            this.documents[docInx].nextAppearanceDate &&
            this.documents[docInx].concluded !== "Y" &&
            this.documents[docInx].isEnabled
          ) {
            this.documents[docInx].isChecked = checked;
          }
        }
      } else if (this.activetab == "ORDERS") {
        for (const docInx in this.documents) {
          if (
            this.documents[docInx].category.toUpperCase() == this.activetab.toUpperCase() &&
            this.documents[docInx].isEnabled
          ) {
            this.documents[docInx].isChecked = checked;
          }
        }
      } else if (this.activetab != "ALL") {
        for (const docInx in this.documents) {
          if (
            this.documents[docInx].category.toUpperCase() == this.activetab.toUpperCase() &&
            this.documents[docInx].isEnabled
          ) {
            this.documents[docInx].isChecked = checked;
          }
        }
      } else {
        for (const docInx in this.documents) {
          if (this.documents[docInx].isEnabled) {
            this.documents[docInx].isChecked = checked;
          }
        }
      }
    }
  }

  public toggleSelectedDocuments() {
    Vue.nextTick(() => {
      if (this.activetab == "COURT SUMMARY") {
        const checkedDocs = this.summaryDocuments.filter((doc) => {
          return doc.isChecked;
        });
        const enabledDocs = this.summaryDocuments.filter((doc) => {
          return doc.isEnabled;
        });
        if (checkedDocs.length == enabledDocs.length) this.allDocumentsChecked = true;
        else this.allDocumentsChecked = false;
      } else {
        const checkedDocs = this.documents.filter((doc) => {
          return doc.isChecked;
        });
        const enabledDocs = this.documents.filter((doc) => {
          return doc.isEnabled;
        });
        if (checkedDocs.length == enabledDocs.length) this.allDocumentsChecked = true;
        else this.allDocumentsChecked = false;
      }
    });
  }

  public navigateToLandingPage() {
    this.$router.push({ name: "Home" });
  }

  public ExtractIssues(issues) {
    let issueString = "";
    for (const issue of issues) {
      issueString += issue.issueDsc + "\n";
    }
    return issueString;
  }

  get FilteredDocuments() {
    if (this.activetab == "COURT SUMMARY") {
      this.fieldsTab = fieldTab.Summary;
      return this.summaryDocuments;
    } else {
      return this.documents.filter((doc) => {
        this.fieldsTab = fieldTab.Categories;
        if (this.activetab == "CONCLUDED") {
          if (doc.concluded === "Y") return true;
          else return false;
        } else if (this.activetab == "SCHEDULED") {
          this.fieldsTab = fieldTab.Scheduled;
          if (doc.nextAppearanceDate && doc.concluded !== "Y") {
            return true;
          } else {
            return false;
          }
        } else if (this.activetab == "ORDERS") {
          this.fieldsTab = fieldTab.Orders;

          if (doc.category.toUpperCase() == this.activetab.toUpperCase()) {
            return true;
          }

          return false;
        } else if (this.activetab == "AFFIDAVITS") {
          this.fieldsTab = fieldTab.Affidavits;
          if (doc.category.toUpperCase() == this.activetab.toUpperCase()) {
            return true;
          }

          return false;
        } else if (this.activetab != "ALL") {
          if (doc.category.toUpperCase() == this.activetab.toUpperCase()) {
            return true;
          }

          return false;
        } else {
          return true;
        }
      });
    }
  }

  get sortBy() {
    if (this.activetab == "COURT SUMMARY") {
      this.sortDesc = true;
      return "appearanceDate";
    } else if (this.activetab == "ORDERS") {
      this.sortDesc = false;
      return "seq";
    } else {
      this.sortDesc = true;
      return "dateFiled"
    }
  }

  get NumberOfDocuments() {
    if (this.activetab == "COURT SUMMARY") {
      return this.summaryDocuments.length;
    } else {
      return this.documents.length;
    }
  }
}
</script>

<style scoped>
.card {
  border: white;
}
</style>
