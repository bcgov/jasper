<template>
  <div>
    <b-card v-if="isMounted" no-body>
      <div>
        <b-row>
          <h3
            class="ml-5 my-1 p-0 font-weight-normal"
            v-if="!showSections['Provided Documents']"
          >
            Provided Documents ({{ NumberOfDocuments }})
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

      <b-card v-if="!isDataReady && isMounted">
        <span class="text-muted ml-4 mb-5"> No provided documents. </span>
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

      <b-tabs
        nav-wrapper-class="bg-light text-dark"
        active-nav-item-class="text-uppercase font-weight-bold text-white bg-primary"
        pills
        no-body
        v-if="isDataReady"
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
          v-if="isDataReady"
          style="max-height: 500px; overflow-y: auto"
          no-body
          class="mx-3 mb-5"
        >
          <b-table
            :items="FilteredDocuments"
            :fields="fields"
            sort-by="appearanceDate"
            :sort-desc.sync="sortDesc"
            :no-sort-reset="true"
            sort-icon-left
            small
            striped
            responsive="sm"
          >
            <template
              v-for="(field, index) in fields"
              v-slot:[`head(${field.key})`]="data"
            >
              <b v-bind:key="index" :class="field.headerStyle">
                {{ data.label }}</b
              >
            </template>

            <template v-slot:cell(appearanceDate)="data">
              <span :style="data.field.cellStyle">
                {{ beautifyDate(data.value) }}
              </span>
            </template>

            <!-- <template v-slot:cell(enterDtm)="data" >                        
                        <span :style="data.field.cellStyle">
                            {{ data.value | beautify_date_time}}
                        </span>
                    </template>                      -->

            <template v-slot:cell(referenceDocumentTypeDsc)="data">
              <b-button
                variant="outline-primary text-info"
                :style="data.field.cellStyle"
                @click="cellClick(data)"
                size="sm"
              >
                {{ data.value }}
              </b-button>
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

            <template v-slot:cell(descriptionText)="data">
              <div
                :style="data.field.cellStyle"
                v-b-tooltip.hover
                :title="data.value.length > 45 ? data.value : ''"
              >
                {{ truncate(data.value, 45) }}
              </div>
            </template>

            <template v-slot:cell(partyName)="data">
              <div v-for="(partyName, index) in data.value" v-bind:key="index">
                <span :style="data.field.cellStyle"> {{ partyName }}</span>
              </div>
            </template>

            <template v-slot:cell(nonPartyName)="data">
              <div v-for="(nonParty, index) in data.value" v-bind:key="index">
                <span :style="data.field.cellStyle"> {{ nonParty }}</span>
              </div>
            </template>

            <template v-slot:cell()="data">
              <span class="ml-2" :style="data.field.cellStyle">
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
  import { beautifyDate, truncate } from '@/filters';
  import { HttpService } from '@/services/HttpService';
  import { useCivilFileStore, useCommonStore } from '@/stores';
  import { referenceDocumentsInfoType } from '@/types/civil';
  import { ArchiveInfoType, DocumentRequestsInfoType } from '@/types/common';
  import { CourtDocumentType, DocumentData } from '@/types/shared';
  import base64url from 'base64url';
  import {
    computed,
    defineComponent,
    inject,
    nextTick,
    onMounted,
    ref,
  } from 'vue';
  import CustomOverlay from '../CustomOverlay.vue';
  import shared from '../shared';

  export default defineComponent({
    components: {
      CustomOverlay,
    },
    setup() {
      const civilFileStore = useCivilFileStore();
      const commonStore = useCommonStore();
      const httpService = inject<HttpService>('httpService');

      if (!httpService) {
        throw new Error('Service undefined.');
      }

      const documents = ref<referenceDocumentsInfoType[]>([]);
      const loadingPdf = ref(false);
      const isMounted = ref(false);
      const isDataReady = ref(false);
      const activetab = ref('ALL');
      const sortDesc = ref(false);
      const categories = ref<string[]>([]);
      const fields = ref([]);
      const selectedDocuments = ref({} as ArchiveInfoType);
      const downloadCompleted = ref(true);
      const allDocumentsChecked = ref(false);

      const initialFields = [
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
          key: 'partyName',
          label: 'Party Name',
          sortable: true,
          headerStyle: 'text-primary',
          cellStyle: 'font-size: 16px;',
        },
        {
          key: 'nonPartyName',
          label: 'Non Party',
          sortable: true,
          headerStyle: 'text-primary',
          cellStyle: 'font-size: 16px;',
        },
        {
          key: 'referenceDocumentTypeDsc',
          label: 'Document Type',
          sortable: false,
          headerStyle: 'text-primary',
          cellStyle: 'border:0px; font-size: 16px;text-align:left;',
        },
        {
          key: 'appearanceDate',
          label: 'Appearance Date',
          sortable: true,
          headerStyle: 'text',
          cellStyle: 'font-size: 16px;',
        },
        // {key:'enterDtm',                 label:'Created Date',    sortable:true,  headerStyle:'text',          cellStyle:'font-size: 16px;'},
        {
          key: 'descriptionText',
          label: 'Description',
          sortable: false,
          headerStyle: 'text',
          cellStyle: 'font-size: 12px;',
        },
      ];

      const getDocuments = () => {
        documents.value =
          civilFileStore.civilFileInformation.referenceDocumentInfo;
        categories.value =
          civilFileStore.civilFileInformation.providedDocumentCategories;
        categories.value.sort();
        if (categories.value.indexOf('ALL') < 0)
          categories.value.unshift('ALL');
        fields.value = JSON.parse(JSON.stringify(initialFields));
        if (!civilFileStore.hasNonParty) {
          fields.value.splice(2, 1);
        }
        if (documents.value.length > 0) {
          isDataReady.value = true;
        }
        isMounted.value = true;
      };

      onMounted(() => {
        getDocuments();
        downloadCompleted.value = true;
        selectedDocuments.value = {
          zipName: '',
          csrRequests: [],
          documentRequests: [],
          ropRequests: [],
        };
      });

      const downloadDocuments = () => {
        const fileName = shared
          .generateFileName(CourtDocumentType.CivilZip, {
            location:
              civilFileStore.civilFileInformation.detailsData
                .homeLocationAgencyName,
            courtLevel:
              civilFileStore.civilFileInformation.detailsData.courtLevelCd,
            fileNumberText:
              civilFileStore.civilFileInformation.detailsData.fileNumberTxt,
          })
          .replace('documents', 'provided-documents');

        selectedDocuments.value = {
          zipName: fileName,
          csrRequests: [],
          documentRequests: [],
          ropRequests: [],
        };
        for (const doc of documents.value) {
          if (doc.isChecked && doc.isEnabled) {
            const id = doc.objectGuid;
            const documentRequest = {} as DocumentRequestsInfoType;
            documentRequest.isCriminal = false;
            const documentData: DocumentData = {
              appearanceDate: beautifyDate(doc.appearanceDate),
              courtLevel:
                civilFileStore.civilFileInformation.detailsData.courtLevelCd,
              documentDescription: doc.descriptionText,
              documentId: id,
              fileNumberText:
                civilFileStore.civilFileInformation.detailsData.fileNumberTxt,
              location:
                civilFileStore.civilFileInformation.detailsData
                  .homeLocationAgencyName,
              partyName: doc.partyName.toString(),
            };
            documentRequest.pdfFileName = shared.generateFileName(
              CourtDocumentType.ProvidedCivil,
              documentData
            );
            documentRequest.base64UrlEncodedDocumentId = base64url(id);
            documentRequest.fileId =
              civilFileStore.civilFileInformation.fileNumber;
            selectedDocuments.value.documentRequests.push(documentRequest);
          }
        }

        if (selectedDocuments.value.documentRequests.length > 0) {
          downloadCompleted.value = false;
          httpService
            .post<Blob>(
              'api/files/archive',
              selectedDocuments,
              { 'Content-Type': 'application/json' },
              'blob'
            )
            .then(
              (data) => {
                const link = document.createElement('a');
                link.href = URL.createObjectURL(data);
                document.body.appendChild(link);
                link.download = selectedDocuments.value.zipName;
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
        if (activetab.value != 'ALL') {
          for (const docInx in documents) {
            if (
              documents[docInx].referenceDocumentTypeDsc.includes(activetab) &&
              documents[docInx].isEnabled
            ) {
              documents[docInx].isChecked = checked;
            }
          }
        } else {
          for (const docInx in documents) {
            if (documents[docInx].isEnabled) {
              documents[docInx].isChecked = checked;
            }
          }
        }
      };

      const switchTab = (tabMapping) => {
        allDocumentsChecked.value = false;
        activetab.value = tabMapping;
      };

      const toggleSelectedDocuments = () => {
        nextTick(() => {
          const checkedDocs = documents.value.filter((doc) => {
            return doc.isChecked;
          });
          const enabledDocs = documents.value.filter((doc) => {
            return doc.isEnabled;
          });
          if (checkedDocs.length == enabledDocs.length)
            allDocumentsChecked.value = true;
          else allDocumentsChecked.value = false;
        });
      };

      const cellClick = (eventData) => {
        loadingPdf.value = true;
        const documentData: DocumentData = {
          appearanceDate: beautifyDate(eventData.item.appearanceDate),
          courtClass:
            civilFileStore.civilFileInformation.detailsData.courtClassCd,
          courtLevel:
            civilFileStore.civilFileInformation.detailsData.courtLevelCd,
          documentId: eventData.item.objectGuid,
          documentDescription: eventData.item.referenceDocumentTypeDsc,
          fileId: civilFileStore.civilFileInformation.fileNumber,
          fileNumberText:
            civilFileStore.civilFileInformation.detailsData.fileNumberTxt,
          partyName: eventData.item.partyName,
          location:
            civilFileStore.civilFileInformation.detailsData
              .homeLocationAgencyName,
        };
        shared.openDocumentsPdf(CourtDocumentType.ProvidedCivil, documentData);
        loadingPdf.value = false;
      };

      const FilteredDocuments = computed(() => {
        return documents.value.filter((doc) => {
          if (activetab.value != 'ALL') {
            if (doc.referenceDocumentTypeDsc.includes(activetab.value)) {
              return true;
            }

            return false;
          } else {
            return true;
          }
        });
      });

      const NumberOfDocuments = computed(() => {
        return documents.value.length;
      });

      return {
        isMounted,
        showSections: civilFileStore.showSections,
        NumberOfDocuments,
        downloadCompleted,
        enableArchive: commonStore.enableArchive,
        isDataReady,
        downloadDocuments,
        categories,
        switchTab,
        activetab,
        loadingPdf,
        FilteredDocuments,
        fields,
        sortDesc,
        beautifyDate,
        cellClick,
        allDocumentsChecked,
        checkAllDocuments,
        toggleSelectedDocuments,
        truncate,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
