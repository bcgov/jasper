<template>
  <b-card v-if="isMounted" no-body>
    <div>
      <b-row class="ml-0">
        <h3 class="mx-4 font-weight-normal">Documents ({{ NumberOfDocuments }})</h3>
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
      <hr class="mx-3 mb-0 bg-light" style="height: 5px;" />
    </div>
    <b-card>
      <b-tabs nav-wrapper-class="bg-light text-dark" active-nav-item-class="text-white bg-primary" pills>
        <b-tab
          v-for="(tabMapping, index) in categories"
          :key="index"
          :title="tabMapping"
          v-on:click="switchTab(tabMapping)"
          v-bind:class="[activetab === tabMapping ? 'active' : '']"
        ></b-tab>
      </b-tabs>
    </b-card>

    <b-card>
      <b-dropdown variant="light text-info" :text="getNameOfParticipant(activeCriminalParticipantIndex)" class="m-0">
        <b-dropdown-item-button
          v-for="participant in SortedParticipants"
          :key="participant.index"
          v-on:click="setActiveParticipantIndex(participant.index)"
        >
          {{ participant.name }}
        </b-dropdown-item-button>
      </b-dropdown>
    </b-card>

    <b-overlay :show="loadingPdf" rounded="sm">
      <b-card class="mx-3" bg-variant="light">
        <b-table
          v-if="FilteredDocuments.length > 0"
          :items="FilteredDocuments"
          :fields="fields[fieldsTab]"
          :sort-by.sync="sortBy"
          :sort-desc.sync="sortDesc"
          :no-sort-reset="true"
          small
          striped
          borderless
          sort-icon-left
          responsive="sm"
        >
          <template v-for="(field, index) in fields[fieldsTab]" v-slot:[`head(${field.key})`]="data">
            <b v-bind:key="index" :class="field.headerStyle"> {{ data.label }}</b>
          </template>

          <template v-slot:head(date)>
            <b class="text-danger">{{ getNameOfDateInTabs }}</b>
          </template>

          <template v-if="enableArchive" v-slot:head(select)>
            <b-form-checkbox class="m-0" v-model="allDocumentsChecked" @change="checkAllDocuments" size="sm" />
          </template>

          <template v-if="enableArchive" v-slot:[`cell(${fields[fieldsTab][0].key})`]="data">
            <b-form-checkbox
              size="sm"
              class="m-0"
              :disabled="!data.item.isEnabled"
              v-model="data.item.isChecked"
              @change="toggleSelectedDocuments"
            />
          </template>

          <template v-slot:[`cell(${fields[0][1].key})`]="data">
            {{ data.value | beautify_date }}
          </template>

          <template v-slot:[`cell(${fields[fieldsTab][documentPlace[fieldsTab]].key})`]="data">
            <b-button
              v-if="data.item.pdfAvail"
              variant="outline-primary text-info"
              style="border:0px; font-size:16px"
              @click="cellClick(data)"
              size="sm"
            >
              {{ data.value }}
            </b-button>
            <span class="ml-2" v-else>
              {{ data.value }}
            </span>
          </template>

          <template v-slot:cell(statusDate)="data">
            {{ data.value | beautify_date }}
          </template>
        </b-table>
        <span v-else class="text-muted ml-4 mb-5">
          No document with label <b>{{ activetab }}</b
          >.</span
        >
      </b-card>
      <template v-slot:overlay>
        <div style="text-align: center">
          <loading-spinner />
          <p id="Downloading-label">Downloading PDF file ...</p>
        </div>
      </template>
    </b-overlay>
  </b-card>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import * as _ from "underscore";
import "@store/modules/CriminalFileInformation";
import "@store/modules/CommonInformation";
import {
  participantROPInfoType,
  participantListInfoType,
  participantDocumentsInfoType,
  criminalFileInformationType,
  ropRequestsInfoType,
} from "@/types/criminal";
import base64url from "base64url";

import shared from "../shared";
const criminalState = namespace("CriminalFileInformation");
const commonState = namespace("CommonInformation");

import CustomOverlay from "../CustomOverlay.vue";
import { InputNamesType, ArchiveInfoType, DocumentRequestsInfoType } from "@/types/common";
import { CourtDocumentType, DocumentData } from "@/types/shared";

enum fieldTab {
  Categories = 0,
  Summary,
  Bail,
}

@Component({
  components: {
    CustomOverlay,
  },
})
export default class CriminalDocumentsView extends Vue {
  @criminalState.State
  public activeCriminalParticipantIndex;

  @commonState.State
  public displayName!: string;

  @commonState.State
  public enableArchive!: boolean;

  @criminalState.Action
  public UpdateActiveCriminalParticipantIndex!: (newActiveCriminalParticipantIndex: string) => void;

  @criminalState.State
  public criminalFileInformation!: criminalFileInformationType;

  @criminalState.Action
  public UpdateCriminalFile!: (newCriminalFileInformation: criminalFileInformationType) => void;

  @commonState.Action
  public UpdateDisplayName!: (newInputNames: InputNamesType) => void;

  participantFiles: participantListInfoType[] = [];
  participantList: participantListInfoType[] = [];
  categories: string[] = [];

  courtLevel;
  courtClass;

  message = "Loading";
  loadingPdf = false;
  activetab = "ALL";
  tabIndex = 0;
  sortBy = "date";
  sortDesc = true;
  hoverRow = -1;
  hoverCol = 0;
  isMounted = false;
  isDataValid = false;

  fieldsTab = fieldTab.Categories;
  documentPlace = [2, 1, 2];
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
      { key: "date", label: "Date", sortable: true, tdClass: "border-top", headerStyle: "text-danger" },
      {
        key: "documentType",
        label: "Document Type",
        sortable: true,
        tdClass: "border-top",
        cellStyle: "text-align:left;",
        headerStyle: "text-primary",
      },
      { key: "category", label: "Category", sortable: false, tdClass: "border-top", headerStyle: "text" },
      { key: "pages", label: "Pages", sortable: false, tdClass: "border-top", headerStyle: "text" },
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
        tdClass: "border-top",
        cellStyle: "text-align:left;",
        headerStyle: "text-primary",
      },
      { key: "category", label: "Category", sortable: true, tdClass: "border-top", headerStyle: "text" },
      { key: "pages", label: "Pages", sortable: false, tdClass: "border-top", headerStyle: "text" },
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
      { key: "date", label: "Date", sortable: true, tdClass: "border-top", headerStyle: "text-danger" },
      {
        key: "documentType",
        label: "Document Type",
        sortable: true,
        tdClass: "border-top",
        cellStyle: "text-align:left;",
        headerStyle: "text-primary",
      },
      { key: "status", label: "Status", sortable: true, tdClass: "border-top", headerStyle: "text-primary" },
      { key: "statusDate", label: "Status Date", sortable: true, tdClass: "border-top", headerStyle: "text-primary" },
      { key: "category", label: "Category", sortable: false, tdClass: "border-top", headerStyle: "text" },
      { key: "pages", label: "Pages", sortable: false, tdClass: "border-top", headerStyle: "text" },
    ],
  ];

  public getDocuments(): void {
    this.participantList = this.criminalFileInformation.participantList;
    this.courtLevel = this.criminalFileInformation.courtLevel;
    this.courtClass = this.criminalFileInformation.courtClass;

    this.ExtractDocumentInfo();
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

  public setActiveParticipantIndex(index) {
    this.UpdateActiveCriminalParticipantIndex(index);
  }

  public navigateToLandingPage() {
    this.$router.push({ name: "Home" });
  }

  public getNameOfParticipant(num) {
    this.UpdateDisplayName({
      lastName: this.participantFiles[num].lastName,
      givenName: this.participantFiles[num].firstName,
    });
    return this.displayName;
  }

  public downloadDocuments() {
    // console.log(this.participantFiles["Documents"])

    const fileName = shared.generateFileName(CourtDocumentType.CriminalZip, {
      location: this.criminalFileInformation.detailsData.homeLocationAgencyName,
      courtClass: this.criminalFileInformation.detailsData.courtClassCd,
      courtLevel: this.criminalFileInformation.detailsData.courtLevelCd,
      fileNumberText: this.criminalFileInformation.detailsData.fileNumberTxt,
    });

    this.selectedDocuments = { zipName: fileName, csrRequests: [], documentRequests: [], ropRequests: [] };
    for (const doc of this.participantFiles[this.activeCriminalParticipantIndex].documents) {
      if (doc.isChecked && doc.isEnabled) {
        const id = doc.imageId;
        const documentRequest = {} as DocumentRequestsInfoType;
        documentRequest.isCriminal = true;
        const documentData: DocumentData = {
          courtClass: this.criminalFileInformation.detailsData.courtClassCd,
          courtLevel: this.criminalFileInformation.detailsData.courtLevelCd,
          dateFiled: Vue.filter("beautify_date")(doc.date),
          documentDescription: doc.documentType,
          documentId: id,
          fileId: this.criminalFileInformation.fileNumber,
          fileNumberText: this.criminalFileInformation.detailsData.fileNumberTxt,
          location: this.criminalFileInformation.detailsData.homeLocationAgencyName,
        };
        documentRequest.pdfFileName = shared.generateFileName(CourtDocumentType.Criminal, documentData);
        documentRequest.base64UrlEncodedDocumentId = base64url(id);
        documentRequest.fileId = this.criminalFileInformation.fileNumber;
        this.selectedDocuments.documentRequests.push(documentRequest);
      }
    }

    for (const doc of this.participantFiles[this.activeCriminalParticipantIndex].recordOfProceedings) {
      if (doc.isChecked && doc.isEnabled) {
        const ropRequest = {} as ropRequestsInfoType;
        const partId = doc.partId;
        const documentData: DocumentData = {
          courtClass: this.criminalFileInformation.detailsData.courtClassCd,
          courtLevel: this.criminalFileInformation.detailsData.courtLevelCd,
          documentDescription: doc.documentType,
          fileId: this.criminalFileInformation.fileNumber,
          fileNumberText: this.criminalFileInformation.detailsData.fileNumberTxt,
          location: this.criminalFileInformation.detailsData.homeLocationAgencyName,
          partId: partId,
          profSeqNo: doc.profSeqNo,
        };
        ropRequest.pdfFileName = shared.generateFileName(CourtDocumentType.ROP, documentData);
        ropRequest.partId = partId;
        ropRequest.profSequenceNumber = doc.profSeqNo;
        ropRequest.courtLevelCode = this.criminalFileInformation.courtLevel;
        ropRequest.courtClassCode = this.criminalFileInformation.courtClass;
        this.selectedDocuments.ropRequests.push(ropRequest);
      }
    }

    if (this.selectedDocuments.ropRequests.length > 0 || this.selectedDocuments.documentRequests.length > 0) {
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
    if (this.activetab == "ROP") {
      for (const docInx in this.participantFiles[this.activeCriminalParticipantIndex].recordOfProceedings) {
        if (this.participantFiles[this.activeCriminalParticipantIndex].recordOfProceedings[docInx].isEnabled) {
          this.participantFiles[this.activeCriminalParticipantIndex].recordOfProceedings[docInx].isChecked = checked;
        }
      }
    } else {
      if (this.activetab != "ALL") {
        for (const docInx in this.participantFiles[this.activeCriminalParticipantIndex].documents) {
          if (
            this.participantFiles[this.activeCriminalParticipantIndex].documents[docInx].category.toUpperCase() ==
              this.activetab.toUpperCase() &&
            this.participantFiles[this.activeCriminalParticipantIndex].documents[docInx].isEnabled
          ) {
            this.participantFiles[this.activeCriminalParticipantIndex].documents[docInx].isChecked = checked;
          }
        }
      } else {
        for (const docInx in this.participantFiles[this.activeCriminalParticipantIndex].documents) {
          if (this.participantFiles[this.activeCriminalParticipantIndex].documents[docInx].isEnabled) {
            this.participantFiles[this.activeCriminalParticipantIndex].documents[docInx].isChecked = checked;
          }
        }
      }
    }
  }

  public toggleSelectedDocuments() {
    Vue.nextTick(() => {
      if (this.activetab == "ROP") {
        const checkedDocs = this.participantFiles[this.activeCriminalParticipantIndex].recordOfProceedings.filter(
          (doc) => {
            return doc.isChecked;
          }
        );
        const enabledDocs = this.participantFiles[this.activeCriminalParticipantIndex].recordOfProceedings.filter(
          (doc) => {
            return doc.isEnabled;
          }
        );
        if (checkedDocs.length == enabledDocs.length) this.allDocumentsChecked = true;
        else this.allDocumentsChecked = false;
      } else {
        const checkedDocs = this.participantFiles[this.activeCriminalParticipantIndex].documents.filter((doc) => {
          return doc.isChecked;
        });
        const enabledDocs = this.participantFiles[this.activeCriminalParticipantIndex].documents.filter((doc) => {
          return doc.isEnabled;
        });
        if (checkedDocs.length == enabledDocs.length) this.allDocumentsChecked = true;
        else this.allDocumentsChecked = false;
      }
    });
  }

  public ExtractDocumentInfo(): void {
    let ropExists = false;

    for (const partIndex in this.participantList) {
      const partInfo = this.participantList[partIndex];
      partInfo.documents = [];
      partInfo.recordOfProceedings = [];
      const document: participantDocumentsInfoType[] = [];
      const rop: participantROPInfoType[] = [];

      for (const doc of partInfo.documentsJson) {
        if (doc.category != "rop") {
          const docInfo = {} as participantDocumentsInfoType;
          docInfo.date = doc.issueDate ? doc.issueDate.split(" ")[0] : "";
          docInfo.documentType = doc.docmFormDsc;
          docInfo.category = doc.category ? doc.category : doc.docmClassification;
          docInfo.pages = doc.documentPageCount;
          docInfo.pdfAvail = doc.imageId ? true : false;
          docInfo.imageId = doc.imageId;
          docInfo.status = doc.docmDispositionDsc;
          docInfo.statusDate = doc.docmDispositionDate?.substring(0, 10);
          docInfo.isEnabled = docInfo.pdfAvail;
          docInfo.isChecked = false;
          if (docInfo.category != "PSR") {
            docInfo.category = docInfo.category.charAt(0).toUpperCase() + docInfo.category.slice(1).toLowerCase();
          }
          if (this.categories.indexOf(docInfo.category) < 0) this.categories.push(docInfo.category);

          document.push(docInfo);
        } else {
          const docInfo = {} as participantROPInfoType;
          docInfo.documentType = "Record of Proceedings";
          docInfo.category = "ROP";
          docInfo.pages = doc.documentPageCount;
          docInfo.pdfAvail = true;
          docInfo.index = partIndex;
          docInfo.profSeqNo = partInfo.profSeqNo;
          docInfo.partId = partInfo.partId;
          docInfo.isEnabled = docInfo.pdfAvail;
          docInfo.isChecked = false;
          rop.push(docInfo);
          ropExists = true;
        }
      }
      partInfo.documents = document;
      partInfo.recordOfProceedings = rop;
      this.participantFiles.push(partInfo);
    }

    this.categories.sort();
    if (ropExists) this.categories.push("ROP");
    this.categories.unshift("ALL");
  }

  get SortedParticipants() {
    return _.sortBy(this.participantFiles, (participant) => {
      return participant.lastName ? participant.lastName.toUpperCase() : "";
    });
  }

  get FilteredDocuments() {
    if (this.activetab == "ROP") {
      this.fieldsTab = fieldTab.Summary;
      return this.participantFiles[this.activeCriminalParticipantIndex].recordOfProceedings;
    } else {
      return this.participantFiles[this.activeCriminalParticipantIndex].documents.filter((doc) => {
        this.fieldsTab = fieldTab.Categories;
        if (this.activetab == "Bail") {
          this.fieldsTab = fieldTab.Bail;

          if (doc.category.toUpperCase() == this.activetab.toUpperCase()) {
            return true;
          }

          return false;
        } else if (this.activetab != "ALL") {
          if (doc.category.toUpperCase() == this.activetab.toUpperCase()) return true;

          return false;
        } else {
          return true;
        }
      });
    }
  }

  get getNameOfDateInTabs() {
    switch (this.activetab.toLowerCase()) {
      case "all":
        return "Date Filed/Issued";
      case "scheduled":
        return "Date Sworn/Filed";
      case "bail":
        return "Date Ordered";
      case "psr":
        return "Date Filed";
      default:
        return "Date Sworn/Issued";
    }
  }

  get NumberOfDocuments() {
    if (this.activetab == "ROP") {
      return this.participantFiles[this.activeCriminalParticipantIndex].recordOfProceedings.length;
    } else {
      return this.participantFiles[this.activeCriminalParticipantIndex].documents.length;
    }
  }

  public cellClick(eventData) {
    this.loadingPdf = true;
    const documentType = eventData.item?.category == "ROP" ? CourtDocumentType.ROP : CourtDocumentType.Criminal;
    // const index = eventData.index;
    const documentData: DocumentData = {
      courtClass: this.criminalFileInformation.detailsData.courtClassCd,
      courtLevel: this.criminalFileInformation.detailsData.courtLevelCd,
      dateFiled: Vue.filter("beautify_date")(eventData.item.date),
      documentId: eventData.item?.imageId,
      documentDescription: eventData.item?.documentType,
      fileId: this.criminalFileInformation.fileNumber,
      fileNumberText: this.criminalFileInformation.detailsData.fileNumberTxt,
      partId: eventData.item?.partId,
      profSeqNo: eventData.item?.profSeqNo,
      location: this.criminalFileInformation.detailsData.homeLocationAgencyName,
    };

    shared.openDocumentsPdf(documentType, documentData);
    this.loadingPdf = false;
  }
}
</script>

<style scoped>
.card {
  border: white;
}
</style>
