<template>
  <b-card v-if="isMounted" no-body>
    <div>
      <b-row class="ml-0">
        <h3 class="mx-4 font-weight-normal">
          Documents ({{ NumberOfDocuments }})
        </h3>
        <custom-overlay
          :show="!downloadCompleted"
          style="padding: 0 1rem; margin-left: auto; margin-right: 2rem"
        >
          <b-button
            v-if="enableArchive"
            @click="downloadDocuments()"
            size="sm"
            variant="success"
            style="padding: 0 1rem; margin-left: auto; margin-right: 2rem"
          >
            Download Selected
          </b-button>
        </custom-overlay>
      </b-row>
      <hr class="mx-3 mb-0 bg-light" style="height: 5px" />
    </div>
    <b-card>
      <b-tabs
        nav-wrapper-class="bg-light text-dark"
        active-nav-item-class="text-white bg-primary"
        pills
      >
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
      <b-dropdown
        variant="light text-info"
        :text="getNameOfParticipant(activeCriminalParticipantIndex)"
        class="m-0"
      >
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
          <template
            v-for="(field, index) in fields[fieldsTab]"
            v-slot:[`head(${field.key})`]="data"
          >
            <b v-bind:key="index" :class="field.headerStyle">
              {{ data.label }}</b
            >
          </template>

          <template v-slot:head(date)>
            <b class="text-danger">{{ getNameOfDateInTabs }}</b>
          </template>

          <template v-if="enableArchive" v-slot:head(select)>
            <b-form-checkbox
              class="m-0"
              v-model="allDocumentsChecked"
              @change="checkAllDocuments"
              size="sm"
            />
          </template>

          <template v-if="enableArchive" v-slot:cell(select)]="data">
            <b-form-checkbox
              size="sm"
              class="m-0"
              :disabled="!data.item.isEnabled"
              v-model="data.item.isChecked"
              @change="toggleSelectedDocuments"
            />
          </template>

          <template v-slot:cell(date)="data">
            {{ beautifyDate(data.value) }}
          </template>

          <template v-slot:cell(documentType)="data">
            <b-button
              v-if="data.item.pdfAvail"
              variant="outline-primary text-info"
              style="border: 0px; font-size: 16px"
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
            {{ beautifyDate(data.value) }}
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
  import {
    participantDocumentsInfoType,
    participantListInfoType,
    participantROPInfoType,
    ropRequestsInfoType,
  } from '@/types/criminal';
  import base64url from 'base64url';
  import * as _ from 'underscore';

  import shared from '../shared';

  import { beautifyDate } from '@/filters';
  import { HttpService } from '@/services/HttpService';
  import { useCommonStore, useCriminalFileStore } from '@/stores';
  import { ArchiveInfoType, DocumentRequestsInfoType } from '@/types/common';
  import { CourtDocumentType, DocumentData } from '@/types/shared';
  import {
    computed,
    defineComponent,
    inject,
    nextTick,
    onMounted,
    ref,
  } from 'vue';
  //  import { useRouter } from 'vue-router';
  import CustomOverlay from '../CustomOverlay.vue';

  enum fieldTab {
    Categories = 0,
    Summary,
    Bail,
  }

  export default defineComponent({
    components: {
      CustomOverlay,
    },
    setup() {
      const commonStore = useCommonStore();
      const criminalFileStore = useCriminalFileStore();
      //      const router = useRouter();
      const httpService = inject<HttpService>('httpService');

      if (!httpService) {
        throw new Error('HttpService is not available!');
      }

      const participantFiles: participantListInfoType[] = [];
      let participantList: participantListInfoType[] = [];
      const categories = ref<string[]>([]);

      //      let courtLevel;
      //      let courtClass;
      //      const message = ref('Loading');
      const loadingPdf = ref(false);
      const activetab = ref('ALL');
      //      const tabIndex = ref(0);
      const sortBy = ref('date');
      const sortDesc = ref(true);
      //      const hoverRow = ref(-1);
      //      const hoverCol = ref(0);
      const isMounted = ref(false);
      //      const isDataValid = ref(false);

      let fieldsTab = fieldTab.Categories;
      const documentPlace = ref([2, 1, 2]);
      let selectedDocuments = {} as ArchiveInfoType;
      const downloadCompleted = ref(true);
      const allDocumentsChecked = ref(false);

      const fields = [
        [
          {
            key: 'select',
            label: '',
            sortable: false,
            headerStyle: 'text-primary',
            cellStyle: 'font-size: 16px;',
            tdClass: 'border-top',
            thClass: '',
          },
          {
            key: 'date',
            label: 'Date',
            sortable: true,
            tdClass: 'border-top',
            headerStyle: 'text-danger',
          },
          {
            key: 'documentType',
            label: 'Document Type',
            sortable: true,
            tdClass: 'border-top',
            cellStyle: 'text-align:left;',
            headerStyle: 'text-primary',
          },
          {
            key: 'category',
            label: 'Category',
            sortable: false,
            tdClass: 'border-top',
            headerStyle: 'text',
          },
          {
            key: 'pages',
            label: 'Pages',
            sortable: false,
            tdClass: 'border-top',
            headerStyle: 'text',
          },
        ],
        [
          {
            key: 'select',
            label: '',
            sortable: false,
            headerStyle: 'text-primary',
            cellStyle: 'font-size: 16px;',
            tdClass: 'border-top',
            thClass: '',
          },
          {
            key: 'documentType',
            label: 'Document Type',
            sortable: false,
            tdClass: 'border-top',
            cellStyle: 'text-align:left;',
            headerStyle: 'text-primary',
          },
          {
            key: 'category',
            label: 'Category',
            sortable: true,
            tdClass: 'border-top',
            headerStyle: 'text',
          },
          {
            key: 'pages',
            label: 'Pages',
            sortable: false,
            tdClass: 'border-top',
            headerStyle: 'text',
          },
        ],
        [
          {
            key: 'select',
            label: '',
            sortable: false,
            headerStyle: 'text-primary',
            cellStyle: 'font-size: 16px;',
            tdClass: 'border-top',
            thClass: '',
          },
          {
            key: 'date',
            label: 'Date',
            sortable: true,
            tdClass: 'border-top',
            headerStyle: 'text-danger',
          },
          {
            key: 'documentType',
            label: 'Document Type',
            sortable: true,
            tdClass: 'border-top',
            cellStyle: 'text-align:left;',
            headerStyle: 'text-primary',
          },
          {
            key: 'status',
            label: 'Status',
            sortable: true,
            tdClass: 'border-top',
            headerStyle: 'text-primary',
          },
          {
            key: 'statusDate',
            label: 'Status Date',
            sortable: true,
            tdClass: 'border-top',
            headerStyle: 'text-primary',
          },
          {
            key: 'category',
            label: 'Category',
            sortable: false,
            tdClass: 'border-top',
            headerStyle: 'text',
          },
          {
            key: 'pages',
            label: 'Pages',
            sortable: false,
            tdClass: 'border-top',
            headerStyle: 'text',
          },
        ],
      ];

      const getDocuments = () => {
        participantList =
          criminalFileStore.criminalFileInformation.participantList;
        //        courtLevel = criminalFileStore.criminalFileInformation.courtLevel;
        //        courtClass = criminalFileStore.criminalFileInformation.courtClass;

        ExtractDocumentInfo();
        isMounted.value = true;
      };

      onMounted(() => {
        getDocuments();
        downloadCompleted.value = true;
        selectedDocuments = {
          zipName: '',
          csrRequests: [],
          documentRequests: [],
          ropRequests: [],
        };
      });

      const switchTab = (tabMapping) => {
        allDocumentsChecked.value = false;
        activetab.value = tabMapping;
      };

      const setActiveParticipantIndex = (index) => {
        criminalFileStore.updateActiveCriminalParticipantIndex(index);
      };

      // const navigateToLandingPage = () => {
      //   router.push({ name: 'Home' });
      // };

      const getNameOfParticipant = (num) => {
        commonStore.updateDisplayName({
          lastName: participantFiles[num].lastName,
          givenName: participantFiles[num].firstName,
        });
        return commonStore.displayName;
      };

      const downloadDocuments = () => {
        // console.log(participantFiles["Documents"])

        const fileName = shared.generateFileName(
          CourtDocumentType.CriminalZip,
          {
            location:
              criminalFileStore.criminalFileInformation.detailsData
                .homeLocationAgencyName,
            courtClass:
              criminalFileStore.criminalFileInformation.detailsData
                .courtClassCd,
            courtLevel:
              criminalFileStore.criminalFileInformation.detailsData
                .courtLevelCd,
            fileNumberText:
              criminalFileStore.criminalFileInformation.detailsData
                .fileNumberTxt,
          }
        );

        selectedDocuments = {
          zipName: fileName,
          csrRequests: [],
          documentRequests: [],
          ropRequests: [],
        };
        for (const doc of participantFiles[
          criminalFileStore.activeCriminalParticipantIndex
        ].documents) {
          if (doc.isChecked && doc.isEnabled) {
            const id = doc.imageId;
            const documentRequest = {} as DocumentRequestsInfoType;
            documentRequest.isCriminal = true;
            const documentData: DocumentData = {
              courtClass:
                criminalFileStore.criminalFileInformation.detailsData
                  .courtClassCd,
              courtLevel:
                criminalFileStore.criminalFileInformation.detailsData
                  .courtLevelCd,
              dateFiled: beautifyDate(doc.date),
              documentDescription: doc.documentType,
              documentId: id,
              fileId: criminalFileStore.criminalFileInformation.fileNumber,
              fileNumberText:
                criminalFileStore.criminalFileInformation.detailsData
                  .fileNumberTxt,
              location:
                criminalFileStore.criminalFileInformation.detailsData
                  .homeLocationAgencyName,
            };
            documentRequest.pdfFileName = shared.generateFileName(
              CourtDocumentType.Criminal,
              documentData
            );
            documentRequest.base64UrlEncodedDocumentId = base64url(id);
            documentRequest.fileId =
              criminalFileStore.criminalFileInformation.fileNumber;
            selectedDocuments.documentRequests.push(documentRequest);
          }
        }

        for (const doc of participantFiles[
          criminalFileStore.activeCriminalParticipantIndex
        ].recordOfProceedings) {
          if (doc.isChecked && doc.isEnabled) {
            const ropRequest = {} as ropRequestsInfoType;
            const partId = doc.partId;
            const documentData: DocumentData = {
              courtClass:
                criminalFileStore.criminalFileInformation.detailsData
                  .courtClassCd,
              courtLevel:
                criminalFileStore.criminalFileInformation.detailsData
                  .courtLevelCd,
              documentDescription: doc.documentType,
              fileId: criminalFileStore.criminalFileInformation.fileNumber,
              fileNumberText:
                criminalFileStore.criminalFileInformation.detailsData
                  .fileNumberTxt,
              location:
                criminalFileStore.criminalFileInformation.detailsData
                  .homeLocationAgencyName,
              partId: partId,
              profSeqNo: doc.profSeqNo,
            };
            ropRequest.pdfFileName = shared.generateFileName(
              CourtDocumentType.ROP,
              documentData
            );
            ropRequest.partId = partId;
            ropRequest.profSequenceNumber = doc.profSeqNo;
            ropRequest.courtLevelCode =
              criminalFileStore.criminalFileInformation.courtLevel;
            ropRequest.courtClassCode =
              criminalFileStore.criminalFileInformation.courtClass;
            selectedDocuments.ropRequests.push(ropRequest);
          }
        }

        if (
          selectedDocuments.ropRequests.length > 0 ||
          selectedDocuments.documentRequests.length > 0
        ) {
          downloadCompleted.value = false;
          httpService
            .post<Blob>(
              'api/files/archive',
              selectedDocuments,
              {
                'Content-Type': 'application/json',
              },
              'blob'
            )
            .then(
              (blob) => {
                const link = document.createElement('a');
                link.href = URL.createObjectURL(blob);
                document.body.appendChild(link);
                link.download = selectedDocuments.zipName;
                link.click();
                setTimeout(() => URL.revokeObjectURL(link.href), 1000);
                downloadCompleted.value = true;
              },
              (err) => {
                // $bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
                //   title: "An error has occured.",
                //   variant: "danger",
                //   autoHideDelay: 10000,
                // });
                console.log(err);
                downloadCompleted.value = true;
              }
            );
        }
      };

      const checkAllDocuments = (checked) => {
        if (activetab.value == 'ROP') {
          for (const docInx in participantFiles[
            criminalFileStore.activeCriminalParticipantIndex
          ].recordOfProceedings) {
            if (
              participantFiles[criminalFileStore.activeCriminalParticipantIndex]
                .recordOfProceedings[docInx].isEnabled
            ) {
              participantFiles[
                criminalFileStore.activeCriminalParticipantIndex
              ].recordOfProceedings[docInx].isChecked = checked;
            }
          }
        } else {
          if (activetab.value != 'ALL') {
            for (const docInx in participantFiles[
              criminalFileStore.activeCriminalParticipantIndex
            ].documents) {
              if (
                participantFiles[
                  criminalFileStore.activeCriminalParticipantIndex
                ].documents[docInx].category.toUpperCase() ==
                  activetab.value.toUpperCase() &&
                participantFiles[
                  criminalFileStore.activeCriminalParticipantIndex
                ].documents[docInx].isEnabled
              ) {
                participantFiles[
                  criminalFileStore.activeCriminalParticipantIndex
                ].documents[docInx].isChecked = checked;
              }
            }
          } else {
            for (const docInx in participantFiles[
              criminalFileStore.activeCriminalParticipantIndex
            ].documents) {
              if (
                participantFiles[
                  criminalFileStore.activeCriminalParticipantIndex
                ].documents[docInx].isEnabled
              ) {
                participantFiles[
                  criminalFileStore.activeCriminalParticipantIndex
                ].documents[docInx].isChecked = checked;
              }
            }
          }
        }
      };

      const toggleSelectedDocuments = () => {
        nextTick(() => {
          if (activetab.value == 'ROP') {
            const checkedDocs = participantFiles[
              criminalFileStore.activeCriminalParticipantIndex
            ].recordOfProceedings.filter((doc) => {
              return doc.isChecked;
            });
            const enabledDocs = participantFiles[
              criminalFileStore.activeCriminalParticipantIndex
            ].recordOfProceedings.filter((doc) => {
              return doc.isEnabled;
            });
            if (checkedDocs.length == enabledDocs.length)
              allDocumentsChecked.value = true;
            else allDocumentsChecked.value = false;
          } else {
            const checkedDocs = participantFiles[
              criminalFileStore.activeCriminalParticipantIndex
            ].documents.filter((doc) => {
              return doc.isChecked;
            });
            const enabledDocs = participantFiles[
              criminalFileStore.activeCriminalParticipantIndex
            ].documents.filter((doc) => {
              return doc.isEnabled;
            });
            if (checkedDocs.length == enabledDocs.length)
              allDocumentsChecked.value = true;
            else allDocumentsChecked.value = false;
          }
        });
      };

      const ExtractDocumentInfo = () => {
        let ropExists = false;

        for (const partIndex in participantList) {
          const partInfo = participantList[partIndex];
          partInfo.documents = [];
          partInfo.recordOfProceedings = [];
          const document: participantDocumentsInfoType[] = [];
          const rop: participantROPInfoType[] = [];

          for (const doc of partInfo.documentsJson) {
            if (doc.category != 'rop') {
              const docInfo = {} as participantDocumentsInfoType;
              docInfo.date = doc.issueDate ? doc.issueDate.split(' ')[0] : '';
              docInfo.documentType = doc.docmFormDsc;
              docInfo.category = doc.category
                ? doc.category
                : doc.docmClassification;
              docInfo.pages = doc.documentPageCount;
              docInfo.pdfAvail = doc.imageId ? true : false;
              docInfo.imageId = doc.imageId;
              docInfo.status = doc.docmDispositionDsc;
              docInfo.statusDate = doc.docmDispositionDate?.substring(0, 10);
              docInfo.isEnabled = docInfo.pdfAvail;
              docInfo.isChecked = false;
              if (docInfo.category != 'PSR') {
                docInfo.category =
                  docInfo.category.charAt(0).toUpperCase() +
                  docInfo.category.slice(1).toLowerCase();
              }
              if (categories.value.indexOf(docInfo.category) < 0)
                categories.value.push(docInfo.category);

              document.push(docInfo);
            } else {
              const docInfo = {} as participantROPInfoType;
              docInfo.documentType = 'Record of Proceedings';
              docInfo.category = 'ROP';
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
          participantFiles.push(partInfo);
        }

        categories.value.sort();
        if (ropExists) categories.value.push('ROP');
        categories.value.unshift('ALL');
      };

      const SortedParticipants = computed(() => {
        return _.sortBy(participantFiles, (participant) => {
          return participant.lastName ? participant.lastName.toUpperCase() : '';
        });
      });

      const FilteredDocuments = computed(() => {
        if (activetab.value == 'ROP') {
          fieldsTab = fieldTab.Summary;
          return participantFiles[
            criminalFileStore.activeCriminalParticipantIndex
          ].recordOfProceedings;
        } else {
          return participantFiles[
            criminalFileStore.activeCriminalParticipantIndex
          ].documents.filter((doc) => {
            fieldsTab = fieldTab.Categories;
            if (activetab.value == 'Bail') {
              fieldsTab = fieldTab.Bail;

              if (doc.category.toUpperCase() == activetab.value.toUpperCase()) {
                return true;
              }

              return false;
            } else if (activetab.value != 'ALL') {
              if (doc.category.toUpperCase() == activetab.value.toUpperCase())
                return true;

              return false;
            } else {
              return true;
            }
          });
        }
      });

      const getNameOfDateInTabs = computed(() => {
        switch (activetab.value.toLowerCase()) {
          case 'all':
            return 'Date Filed/Issued';
          case 'scheduled':
            return 'Date Sworn/Filed';
          case 'bail':
            return 'Date Ordered';
          case 'psr':
            return 'Date Filed';
          default:
            return 'Date Sworn/Issued';
        }
      });

      const NumberOfDocuments = computed(() => {
        if (activetab.value == 'ROP') {
          return participantFiles[
            criminalFileStore.activeCriminalParticipantIndex
          ].recordOfProceedings.length;
        } else {
          return participantFiles[
            criminalFileStore.activeCriminalParticipantIndex
          ].documents.length;
        }
      });

      const cellClick = (eventData) => {
        loadingPdf.value = true;
        const documentType =
          eventData.item?.category == 'ROP'
            ? CourtDocumentType.ROP
            : CourtDocumentType.Criminal;
        // const index = eventData.index;
        const documentData: DocumentData = {
          courtClass:
            criminalFileStore.criminalFileInformation.detailsData.courtClassCd,
          courtLevel:
            criminalFileStore.criminalFileInformation.detailsData.courtLevelCd,
          dateFiled: beautifyDate(eventData.item.date),
          documentId: eventData.item?.imageId,
          documentDescription: eventData.item?.documentType,
          fileId: criminalFileStore.criminalFileInformation.fileNumber,
          fileNumberText:
            criminalFileStore.criminalFileInformation.detailsData.fileNumberTxt,
          partId: eventData.item?.partId,
          profSeqNo: eventData.item?.profSeqNo,
          location:
            criminalFileStore.criminalFileInformation.detailsData
              .homeLocationAgencyName,
        };

        shared.openDocumentsPdf(documentType, documentData);
        loadingPdf.value = false;
      };

      return {
        isMounted,
        NumberOfDocuments,
        downloadCompleted,
        categories,
        activetab,
        enableArchive: commonStore.enableArchive,
        downloadDocuments,
        switchTab,
        getNameOfParticipant,
        activeCriminalParticipantIndex:
          criminalFileStore.activeCriminalParticipantIndex,
        SortedParticipants,
        setActiveParticipantIndex,
        loadingPdf,
        FilteredDocuments,
        fields,
        fieldsTab,
        sortBy,
        sortDesc,
        allDocumentsChecked,
        getNameOfDateInTabs,
        checkAllDocuments,
        beautifyDate,
        toggleSelectedDocuments,
        cellClick,
        documentPlace,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
