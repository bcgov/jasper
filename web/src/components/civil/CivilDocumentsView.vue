<template>
  <div>
    <b-card v-if="isMounted" no-body>
      <div>
        <b-row>
          <h3
            class="ml-5 my-1 p-0 font-weight-normal"
            v-if="!showSections['Documents']"
          >
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
        <hr class="mx-3 bg-light" style="height: 5px" />
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
        <b-card
          bg-variant="light"
          style="max-height: 500px; overflow-y: auto"
          no-body
          class="mx-3 mb-5"
        >
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
            <template
              v-for="(field, index) in fields[fieldsTab]"
              v-slot:[`head(${field.key})`]="data"
            >
              <b v-bind:key="index" :class="field.headerStyle">
                {{ data.label }}</b
              >
            </template>

            <!-- v-slot:[`cell(${fields[fieldsTab][datePlace[fieldsTab]].key})`]="data" -->
            <template v-slot:cell(dateFiled)="data">
              <span
                v-if="data.item.sealed"
                :style="data.field.cellStyle"
                class="text-muted"
              >
                {{ beautifyDate(data.value) }}
              </span>
              <span v-else :style="data.field.cellStyle">
                {{ beautifyDate(data.value) }}
              </span>
            </template>

            <!-- v-slot:[`cell-${fields[fieldsTab][documentPlace[fieldsTab]].key}`]="data" -->
            <template v-slot:cell(documentType)="data">
              <b-button
                v-if="data.item.pdfAvail"
                variant="outline-primary text-info"
                :style="data.field.cellStyle"
                @click="cellClick(data)"
                size="sm"
              >
                {{ data.value }}
              </b-button>
              <span
                class="ml-2"
                v-else-if="!data.item.pdfAvail && !data.item.sealed"
              >
                {{ data.value }}
              </span>
              <span class="ml-2 text-muted" v-else-if="data.item.sealed">
                {{ data.value }}
              </span>
            </template>

            <template v-if="enableArchive" v-slot:head(select)>
              <b-form-checkbox
                class="m-0"
                v-model="allDocumentsChecked"
                @change="checkAllDocuments"
                size="sm"
              />
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
              <li
                v-for="(issue, issueIndex) in data.value"
                v-bind:key="issueIndex"
                :style="data.field.cellStyle"
              >
                <span v-if="data.item.sealed" class="text-muted">{{
                  issue
                }}</span>
                <span v-else>{{ issue }}</span>
              </li>
            </template>
            <template v-slot:cell(seq)="data">
              <span
                v-if="data.item.sealed"
                class="ml-2 text-muted"
                :style="data.field.cellStyle"
              >
                {{ data.value }}
              </span>
              <span v-else class="ml-2" :style="data.field.cellStyle">
                {{ data.value }}
              </span>
            </template>

            <template v-slot:cell()="data">
              <span v-if="data.field.key == 'filedByName'">
                <li
                  v-for="(filed, filedIndex) in data.value"
                  v-bind:key="filedIndex"
                  :style="data.field.cellStyle"
                >
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
  import { beautifyDate } from '@/filters';
  import { HttpService } from '@/services/HttpService';
  import { useCivilFileStore, useCommonStore } from '@/stores';
  import {
    csrRequestsInfoType,
    documentsInfoType,
    summaryDocumentsInfoType,
  } from '@/types/civil';
  import { ArchiveInfoType, DocumentRequestsInfoType } from '@/types/common';
  import { CourtDocumentType, DocumentData } from '@/types/shared';
  import base64url from 'base64url';
  import {
    computed,
    defineComponent,
    inject,
    nextTick,
    onMounted,
    reactive,
    ref,
    watch,
  } from 'vue';
  import CustomOverlay from '../CustomOverlay.vue';
  import shared from '../shared';

  enum fieldTab {
    Categories = 0,
    Summary,
    Orders,
    Scheduled,
    Affidavits,
  }

  export default defineComponent({
    components: {
      CustomOverlay,
    },
    setup() {
      const commonStore = useCommonStore();
      const civilFileStore = useCivilFileStore();
      const httpService = inject<HttpService>('httpService');

      if (!httpService) {
        throw new Error('Service is undefined.');
      }

      const documents = ref<documentsInfoType[]>([]);
      const summaryDocuments = ref<summaryDocumentsInfoType[]>([]);
      const loadingPdf = ref(false);
      const isMounted = ref(false);
      const activetab = ref('ALL');
      const sortDesc = ref(false);
      const categories = ref<string[]>([]);
      const fieldsTab = ref(fieldTab.Categories);
      const documentPlace = [2, 1, 2, 2, 2];
      const datePlace = [4, 2, 3, 5, 3];
      let selectedDocuments = reactive({} as ArchiveInfoType);
      const downloadCompleted = ref(true);
      const allDocumentsChecked = ref(false);

      const commonStyles = {
        cellStyle: 'font-size: 14px;',
        tdClass: 'border-top',
        thClass: '',
      };

      const headerStyles = {
        primary: 'text-primary',
        danger: 'text-danger',
        text: 'text',
      };

      const predefinedFields = {
        select: {
          key: 'select',
          label: '',
          sortable: false,
          headerStyle: headerStyles.primary,
          ...commonStyles,
          cellStyle: 'font-size: 16px;',
        },
        seq: {
          key: 'seq',
          label: 'Seq.',
          sortable: true,
          headerStyle: headerStyles.primary,
          cellStyle: commonStyles.cellStyle,
        },
        documentType: {
          key: 'documentType',
          label: 'Document Type',
          sortable: true,
          headerStyle: headerStyles.primary,
          cellStyle: 'border:0px; font-size: 14px; text-align:left;',
        },
        dateFiled: {
          key: 'dateFiled',
          label: 'Date Filed',
          sortable: true,
          headerStyle: headerStyles.danger,
          cellStyle: commonStyles.cellStyle,
        },
        act: {
          key: 'act',
          label: 'Act',
          sortable: false,
          headerStyle: headerStyles.text,
          cellStyle:
            'display: block; margin-top: 1px; font-size: 14px; max-width : 50px;',
        },
        comment: {
          key: 'comment',
          label: 'Comment',
          sortable: false,
          headerStyle: headerStyles.text,
          cellStyle: 'font-size: 12px; max-width:300px;',
          tdClass: 'max-width-300',
        },
        filedByName: {
          key: 'filedByName',
          label: 'Filed By Name',
          sortable: false,
          headerStyle: headerStyles.text,
          cellStyle:
            'white-space: pre-line; font-size: 14px; margin-left: 20px;',
        },
        swornBy: {
          key: 'swornBy',
          label: 'Sworn By',
          sortable: false,
          headerStyle: headerStyles.text,
          cellStyle:
            'white-space: pre-line; font-size: 14px; margin-left: 20px;',
        },
      };

      const getTableFieldSettings = () => [
        // Section 1
        [
          predefinedFields.select,
          predefinedFields.seq,
          predefinedFields.documentType,
          predefinedFields.act,
          predefinedFields.dateFiled,
          {
            key: 'affNo',
            label: 'Aff No.',
            sortable: false,
            headerStyle: headerStyles.text,
            cellStyle: commonStyles.cellStyle,
          },
          predefinedFields.swornBy,
          {
            key: 'issues',
            label: 'Issues',
            sortable: false,
            headerStyle: headerStyles.text,
            cellStyle:
              'white-space: pre-line; font-size: 14px; margin-left: 20px;',
          },
          predefinedFields.filedByName,
          predefinedFields.comment,
        ],
        // Section 2
        [
          predefinedFields.select,
          predefinedFields.documentType,
          {
            key: 'appearanceDate',
            label: 'Appearance Date',
            sortable: true,
            headerStyle: headerStyles.danger,
            cellStyle: commonStyles.cellStyle,
          },
        ],
        // Section 3
        [
          predefinedFields.select,
          predefinedFields.seq,
          predefinedFields.documentType,
          predefinedFields.dateFiled,
          {
            key: 'orderMadeDate',
            label: 'Order Made Date',
            sortable: true,
            headerStyle: headerStyles.primary,
            cellStyle: commonStyles.cellStyle,
          },
          predefinedFields.filedByName,
          predefinedFields.comment,
        ],
        // Section 4
        [
          predefinedFields.select,
          predefinedFields.seq,
          predefinedFields.documentType,
          predefinedFields.act,
          {
            key: 'nextAppearanceDate',
            label: 'Next Appearance Date',
            sortable: true,
            headerStyle: headerStyles.primary,
            cellStyle: commonStyles.cellStyle,
          },
          predefinedFields.dateFiled,
          {
            key: 'issues',
            label: 'Issues',
            sortable: false,
            headerStyle: headerStyles.text,
            cellStyle:
              'white-space: pre-line; font-size: 14px; margin-left: 20px;',
          },
          predefinedFields.filedByName,
          predefinedFields.comment,
        ],
        // Section 5
        [
          predefinedFields.select,
          predefinedFields.seq,
          predefinedFields.documentType,
          predefinedFields.dateFiled,
          {
            key: 'affNo',
            label: 'Aff No.',
            sortable: false,
            headerStyle: headerStyles.text,
            cellStyle: commonStyles.cellStyle,
          },
          predefinedFields.swornBy,
          predefinedFields.filedByName,
          {
            key: 'issues',
            label: 'Issues',
            sortable: false,
            headerStyle: headerStyles.text,
            cellStyle:
              'white-space: pre-line; font-size: 14px; margin-left: 20px;',
          },
          predefinedFields.act,
          predefinedFields.comment,
        ],
      ];

      const fields = ref(getTableFieldSettings());

      const getDocuments = () => {
        documents.value = civilFileStore.civilFileInformation.documentsInfo;
        summaryDocuments.value =
          civilFileStore.civilFileInformation.summaryDocumentsInfo;
        categories.value = civilFileStore.civilFileInformation.categories;
        categories.value.sort();
        if (
          categories.value.indexOf('COURT SUMMARY') < 0 &&
          summaryDocuments.value.length > 0
        )
          categories.value.push('COURT SUMMARY');
        if (categories.value.indexOf('ALL') < 0)
          categories.value.unshift('ALL');
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

      const cellClick = (eventData) => {
        loadingPdf.value = true;
        const documentType =
          eventData.value == 'CourtSummary'
            ? CourtDocumentType.CSR
            : CourtDocumentType.Civil;
        const documentData: DocumentData = {
          appearanceDate: eventData.item.appearanceDate,
          appearanceId: eventData.item.appearanceId,
          dateFiled: eventData.item.dateFiled,
          documentDescription: eventData.item.documentType,
          documentId: eventData.item.documentId,
          fileId: civilFileStore.civilFileInformation.fileNumber,
          fileNumberText:
            civilFileStore.civilFileInformation.detailsData.fileNumberTxt,
          courtClass:
            civilFileStore.civilFileInformation.detailsData.courtClassCd,
          courtLevel:
            civilFileStore.civilFileInformation.detailsData.courtLevelCd,
          location:
            civilFileStore.civilFileInformation.detailsData
              .homeLocationAgencyName,
        };
        shared.openDocumentsPdf(documentType, documentData);
        loadingPdf.value = false;
      };

      const downloadDocuments = () => {
        const fileName = shared.generateFileName(CourtDocumentType.CivilZip, {
          location:
            civilFileStore.civilFileInformation.detailsData
              .homeLocationAgencyName,
          courtLevel:
            civilFileStore.civilFileInformation.detailsData.courtLevelCd,
          fileNumberText:
            civilFileStore.civilFileInformation.detailsData.fileNumberTxt,
        });

        selectedDocuments = {
          zipName: fileName,
          csrRequests: [],
          documentRequests: [],
          ropRequests: [],
        };
        for (const doc of documents.value) {
          if (doc.isChecked && doc.isEnabled) {
            const id = doc.documentId;
            const documentRequest = {} as DocumentRequestsInfoType;
            documentRequest.isCriminal = false;
            const documentData: DocumentData = {
              courtLevel:
                civilFileStore.civilFileInformation.detailsData.courtLevelCd,
              dateFiled: beautifyDate(doc.dateFiled),
              documentDescription: doc.documentType,
              documentId: id,
              fileNumberText:
                civilFileStore.civilFileInformation.detailsData.fileNumberTxt,
              location:
                civilFileStore.civilFileInformation.detailsData
                  .homeLocationAgencyName,
            };
            documentRequest.pdfFileName = shared.generateFileName(
              CourtDocumentType.Civil,
              documentData
            );
            documentRequest.fileId =
              civilFileStore.civilFileInformation.fileNumber;
            documentRequest.base64UrlEncodedDocumentId = base64url(id);
            selectedDocuments.documentRequests.push(documentRequest);
          }
        }

        for (const doc of summaryDocuments.value) {
          if (doc.isChecked && doc.isEnabled) {
            const id = doc.appearanceId;
            const csrRequest = {} as csrRequestsInfoType;
            csrRequest.appearanceId = id;
            const documentData: DocumentData = {
              appearanceId: csrRequest.appearanceId,
              appearanceDate: beautifyDate(doc.appearanceDate),
              courtLevel:
                civilFileStore.civilFileInformation.detailsData.courtLevelCd,
              documentDescription: doc.documentType,
              fileNumberText:
                civilFileStore.civilFileInformation.detailsData.fileNumberTxt,
              location:
                civilFileStore.civilFileInformation.detailsData
                  .homeLocationAgencyName,
            };
            csrRequest.pdfFileName = shared.generateFileName(
              CourtDocumentType.CSR,
              documentData
            );
            selectedDocuments.csrRequests.push(csrRequest);
          }
        }

        if (
          selectedDocuments.csrRequests.length > 0 ||
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
              (data) => {
                const blob = data;
                const link = document.createElement('a');
                link.href = URL.createObjectURL(blob);
                document.body.appendChild(link);
                link.download = selectedDocuments.zipName;
                link.click();
                setTimeout(() => URL.revokeObjectURL(link.href), 1000);
                downloadCompleted.value = true;
              },
              (err) => {
                // $bvToast.value.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
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
        if (activetab.value == 'COURT SUMMARY') {
          for (const docInx in summaryDocuments.value) {
            if (summaryDocuments.value[docInx].isEnabled) {
              summaryDocuments.value[docInx].isChecked = checked;
            }
          }
        } else {
          if (activetab.value == 'CONCLUDED') {
            for (const docInx in documents.value) {
              if (
                documents.value[docInx].concluded === 'Y' &&
                documents.value[docInx].isEnabled
              ) {
                documents.value[docInx].isChecked = checked;
              }
            }
          } else if (activetab.value == 'SCHEDULED') {
            for (const docInx in documents.value) {
              if (
                documents.value[docInx].nextAppearanceDate &&
                documents.value[docInx].concluded !== 'Y' &&
                documents.value[docInx].isEnabled
              ) {
                documents.value[docInx].isChecked = checked;
              }
            }
          } else if (activetab.value == 'ORDERS') {
            for (const docInx in documents.value) {
              if (
                documents.value[docInx].category.toUpperCase() ==
                  activetab.value.toUpperCase() &&
                documents.value[docInx].isEnabled
              ) {
                documents.value[docInx].isChecked = checked;
              }
            }
          } else if (activetab.value != 'ALL') {
            for (const docInx in documents.value) {
              if (
                documents.value[docInx].category.toUpperCase() ==
                  activetab.value.toUpperCase() &&
                documents.value[docInx].isEnabled
              ) {
                documents.value[docInx].isChecked = checked;
              }
            }
          } else {
            for (const docInx in documents.value) {
              if (documents.value[docInx].isEnabled) {
                documents.value[docInx].isChecked = checked;
              }
            }
          }
        }
      };

      const toggleSelectedDocuments = () => {
        nextTick(() => {
          if (activetab.value == 'COURT SUMMARY') {
            const checkedDocs = summaryDocuments.value.filter((doc) => {
              return doc.isChecked;
            });
            const enabledDocs = summaryDocuments.value.filter((doc) => {
              return doc.isEnabled;
            });
            if (checkedDocs.length == enabledDocs.length)
              allDocumentsChecked.value = true;
            else allDocumentsChecked.value = false;
          } else {
            const checkedDocs = documents.value.filter((doc) => {
              return doc.isChecked;
            });
            const enabledDocs = documents.value.filter((doc) => {
              return doc.isEnabled;
            });
            if (checkedDocs.length == enabledDocs.length)
              allDocumentsChecked.value = true;
            else allDocumentsChecked.value = false;
          }
        });
      };

      const FilteredDocuments = computed(() => {
        if (activetab.value == 'COURT SUMMARY') {
          return summaryDocuments.value;
        } else {
          return documents.value.filter((doc) => {
            fieldsTab.value = fieldTab.Categories;
            if (activetab.value == 'CONCLUDED') {
              if (doc.concluded === 'Y') return true;
              else return false;
            } else if (activetab.value == 'SCHEDULED') {
              fieldsTab.value = fieldTab.Scheduled;
              if (doc.nextAppearanceDate && doc.concluded !== 'Y') {
                return true;
              } else {
                return false;
              }
            } else if (activetab.value == 'ORDERS') {
              fieldsTab.value = fieldTab.Orders;

              if (doc.category.toUpperCase() == activetab.value.toUpperCase()) {
                return true;
              }

              return false;
            } else if (activetab.value == 'AFFIDAVITS') {
              fieldsTab.value = fieldTab.Affidavits;
              if (doc.category.toUpperCase() == activetab.value.toUpperCase()) {
                return true;
              }

              return false;
            } else if (activetab.value != 'ALL') {
              if (doc.category.toUpperCase() == activetab.value.toUpperCase()) {
                return true;
              }

              return false;
            } else {
              return true;
            }
          });
        }
      });

      watch(activetab, (newValue) => {
        if (newValue == 'COURT SUMMARY') {
          fieldsTab.value = fieldTab.Summary;
        }
      });

      const sortBy = computed(() => {
        if (activetab.value == 'COURT SUMMARY') {
          return 'appearanceDate';
        } else if (activetab.value == 'ORDERS') {
          return 'seq';
        } else {
          return 'dateFiled';
        }
      });

      watch(activetab, (newValue) => {
        if (newValue == 'COURT SUMMARY') {
          sortDesc.value = true;
        } else if (newValue == 'ORDERS') {
          sortDesc.value = false;
        } else {
          sortDesc.value = true;
        }
      });

      const NumberOfDocuments = computed(() => {
        if (activetab.value == 'COURT SUMMARY') {
          return summaryDocuments.value.length;
        } else {
          return documents.value.length;
        }
      });

      return {
        isMounted,
        showSections: civilFileStore.showSections,
        NumberOfDocuments,
        downloadCompleted,
        enableArchive: commonStore.enableArchive,
        downloadDocuments,
        categories,
        switchTab,
        activetab,
        loadingPdf,
        FilteredDocuments,
        fields,
        fieldsTab,
        sortBy,
        sortDesc,
        datePlace,
        beautifyDate,
        documentPlace,
        cellClick,
        allDocumentsChecked,
        checkAllDocuments,
        toggleSelectedDocuments,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
