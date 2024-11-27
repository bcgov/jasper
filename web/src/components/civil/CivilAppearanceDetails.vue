<template>
  <div>
    <b-card bg-variant="light" v-if="!isMounted">
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

    <b-card bg-variant="light" v-else no-body>
      <b-card bg-variant="white">
        <b-row cols="2">
          <b-col md="8" cols="8" style="overflow: auto">
            <b-overlay :show="loadingPdf" rounded="sm">
              <div>
                <b-button-group>
                  <h3 class="mx-2 mt-2 font-weight-normal" style="height: 10px">
                    Document Summary
                  </h3>
                  <b-button
                    variant="outline-primary text-info"
                    style="transform: translate(0, 5px); border: 0px"
                    class="mt-0"
                    v-b-tooltip.hover.right
                    title="Download Court Summary"
                    @click="
                      documentClick({
                        appearanceId: civilAppearanceInfo.appearanceId,
                        appearanceDate: civilAppearanceInfo.date,
                        documentDescription: 'CourtSummary',
                      })
                    "
                    size="sm"
                  >
                    <b-icon
                      icon="file-earmark-arrow-down"
                      font-scale="2"
                    ></b-icon>
                  </b-button>
                </b-button-group>
                <hr class="mb-0 bg-light" style="height: 5px" />
              </div>
              <b-card
                v-if="!(appearanceDocuments.length > 0)"
                style="border: white"
              >
                <span class="text-muted"> No documents. </span>
              </b-card>
              <b-table
                style="max-height: 300px; overflow-y: auto"
                v-if="appearanceDocuments.length > 0"
                :items="appearanceDocuments"
                :fields="documentFields"
                borderless
                small
                responsive="sm"
              >
                <template v-slot:head(result)>
                  <b>Result</b><b style="margin-left: 20px">Issues</b>
                </template>

                <template v-slot:cell(documentType)="data">
                  <b-button
                    v-if="data.item.pdfAvail"
                    variant="outline-primary text-info"
                    :style="data.field.cellStyle"
                    @click="documentClick(data)"
                    size="sm"
                  >
                    {{ data.value }}
                  </b-button>
                  <span
                    class="ml-2"
                    :style="data.field.cellLabelStyle"
                    v-else-if="!data.item.pdfAvail"
                  >
                    {{ data.value }}
                  </span>
                  <span
                    class="ml-2 text-muted"
                    v-else-if="data.item.sealed"
                    :style="data.field.cellLabelStyle"
                  >
                    {{ data.value }}
                  </span>
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
                    {{ act.code }}
                  </b-badge>
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

                <template v-slot:cell(result)="data">
                  <b-table
                    :items="data.item.issues"
                    :fields="issueFields"
                    borderless
                    thead-class="d-none"
                    small
                    responsive="sm"
                  >
                    <template v-slot:table-colgroup>
                      <col style="width: 70px" />
                    </template>

                    <template v-slot:cell(issue)="dataR">
                      <li
                        v-if="data.item.sealed"
                        class="text-muted"
                        :style="dataR.field.cellStyle"
                      >
                        {{ dataR.value }}
                      </li>
                      <li v-else :style="dataR.field.cellStyle">
                        {{ dataR.value }}
                      </li>
                    </template>
                    <template v-slot:cell(result)="dataR">
                      <span :style="dataR.field.cellStyle">
                        <b-badge
                          v-if="dataR.value"
                          variant="secondary"
                          v-b-tooltip.hover.left
                          :title="dataR.item.resultDsc"
                        >
                          {{ dataR.value }}
                        </b-badge>
                      </span>
                    </template>
                  </b-table>
                </template>
              </b-table>
              <template v-slot:overlay>
                <div style="text-align: center">
                  <loading-spinner />
                  <p id="Downloading-label">Downloading PDF file ...</p>
                </div>
              </template>
            </b-overlay>
          </b-col>
          <b-col col md="4" cols="4" style="overflow: auto">
            <div>
              <b-button-group>
                <h3
                  class="mx-2 font-weight-normal"
                  style="margin-top: 8px; height: 10px"
                >
                  Additional Info
                </h3>
                <b-button
                  size="sm"
                  style="font-size: 12px; border: 0px"
                  @click="OpenAdjudicatorComment()"
                  variant="outline-primary text-info"
                  v-if="adjudicatorComment.length > 0"
                  class="mt-1"
                  v-b-tooltip.hover.right
                  title="Adjudicator Comment"
                >
                  <b-icon icon="chat-square-fill" font-scale="2"></b-icon>
                </b-button>
              </b-button-group>
              <hr class="mb-0 bg-light" style="height: 5px" />
            </div>
            <b-card
              v-if="
                !(appearanceAdditionalInfo.length > 0) ||
                userInfo.userType == 'vc'
              "
              style="border: white"
            >
              <span class="text-muted"> No additional information. </span>
            </b-card>
            <b-table
              v-if="appearanceAdditionalInfo.length > 0"
              :items="appearanceAdditionalInfo"
              :fields="addInfoFields"
              thead-class="d-none"
              borderless
              responsive="sm"
            >
              <template v-slot:cell(key)="data">
                <b> {{ data.value }}</b>
              </template>
            </b-table>

            <div v-if="appearanceMethods.length > 0">
              <h3 class="mx-2 font-weight-normal">Appearance Methods</h3>
              <hr class="mb-0 bg-light" style="height: 5px" />
            </div>
            <b-table
              v-if="appearanceMethods.length > 0"
              :items="appearanceMethods"
              :fields="appearanceMethodsField"
              thead-class="d-none"
              borderless
              responsive="sm"
            >
              <template
                v-for="(field, index) in appearanceMethodsField"
                v-slot:[`cell(${field.key})`]="data"
              >
                <span
                  v-bind:key="index"
                  :class="data.field.cellClass"
                  :style="data.field.cellStyle"
                  ><b>{{ data.item.role }}</b> is appearing by
                  {{ data.item.method }}
                </span>
              </template>
            </b-table>
          </b-col>
        </b-row>
        <div class="mt-5">
          <h3 class="mx-2 font-weight-normal">Scheduled Parties</h3>
          <hr class="mb-0 bg-light" style="height: 5px" />
        </div>
        <b-table
          style="max-height: 200px; overflow-y: auto"
          :items="appearanceParties"
          :fields="partyFields"
          borderless
          striped
          responsive="sm"
        >
          <template
            v-for="(field, index) in partyFields"
            v-slot:[`head(${field.key})`]="data"
          >
            <b v-bind:key="index"> {{ data.label }}</b>
          </template>
          <template
            v-for="(field, index) in partyFields"
            v-slot:[`cell(${field.key})`]="data"
          >
            <span
              :style="field.cellStyle"
              v-if="data.field.key == 'currentCounsel' && data.value.length > 0"
              v-bind:key="index"
            >
              <span
                v-for="(counsel, counselIndex) in data.value"
                v-bind:key="counselIndex"
              >
                <span v-if="counsel.info.length == 0"
                  >CEIS: {{ counsel.name }}<br
                /></span>
                <span
                  class="text-success"
                  v-bind:key="index"
                  v-else-if="counsel.info.length > 0"
                  v-b-tooltip.hover.right.html="counsel.info"
                >
                  CEIS: {{ counsel.name }} <br
                /></span>
              </span>
            </span>
            <span
              :style="field.cellStyle"
              v-bind:key="`role-${index}`"
              v-else-if="data.field.key == 'role' && data.value.length > 0"
            >
              <span
                v-for="(role, roleIndex) in data.value"
                v-bind:key="roleIndex"
                >{{ role }}<br
              /></span>
            </span>
            <span
              :style="field.cellStyle"
              v-else-if="data.field.key == 'name' && data.item.info.length == 0"
              v-bind:key="`name-${index}`"
            >
              {{ data.value }}
            </span>
            <span
              class="text-success"
              :style="field.cellStyle"
              v-else-if="data.field.key == 'name' && data.item.info.length > 0"
              v-b-tooltip.hover.right.html="data.item.info"
              v-bind:key="`success-name-${index}`"
            >
              {{ data.value }}
            </span>
            <span
              :style="field.cellStyle"
              v-else-if="
                data.field.key == 'representative' && data.value.length > 0
              "
              v-bind:key="`representative-${index}`"
            >
              <span v-for="(rep, repIndex) in data.value" v-bind:key="repIndex">
                <span v-if="rep.info.length == 0">{{ rep.name }}<br /></span>
                <span
                  class="text-success"
                  v-bind:key="index"
                  v-else-if="rep.info.length > 0"
                  v-b-tooltip.hover.left.html="rep.info"
                >
                  {{ rep.name }}
                  <br
                /></span>
              </span>
            </span>
            <span
              :style="field.cellStyle"
              v-else-if="
                data.field.key == 'legalRepresentative' && data.value.length > 0
              "
              v-bind:key="`legalRepresentative-${index}`"
            >
              <span
                v-for="(legalRep, legalRepIndex) in data.value"
                v-bind:key="legalRepIndex"
                ><b>{{ legalRep.type }}</b
                >-{{ legalRep.name }}<br
              /></span>
            </span>
          </template>
        </b-table>
      </b-card>
    </b-card>

    <b-modal
      v-if="isMounted && userInfo.userType != 'vc'"
      v-model="showAdjudicatorComment"
      id="bv-modal-comment"
      hide-footer
    >
      <template v-slot:modal-title>
        <h2 class="mb-0">Adjudicator Comment</h2>
      </template>
      <b-card border-variant="white">{{ adjudicatorComment }}</b-card>
      <!-- <b-button class="mt-3 bg-info" @click="$bvModal.hide('bv-modal-comment')"
        >Close</b-button
      > -->
    </b-modal>
  </div>
</template>

<script lang="ts">
  import { beautifyDate } from '@/filters';
  import { HttpService } from '@/services/HttpService';
  import { useCivilFileStore, useCommonStore } from '@/stores';
  import {
    appearanceAdditionalInfoType,
    appearanceDocumentsType,
    appearanceMethodsType,
    appearancePartiesType,
    civilAppearanceDetailsInfoType,
  } from '@/types/civil';
  import { CourtDocumentType, DocumentData } from '@/types/shared';
  import {
    defineComponent,
    inject,
    onMounted,
    reactive,
    ref,
    toRefs,
  } from 'vue';
  import shared from '../shared';

  export default defineComponent({
    props: {
      tagcasename: {
        type: String,
        required: true,
      },
    },
    setup(props) {
      const commonStore = useCommonStore();
      const civilFileStore = useCivilFileStore();
      const httpService = inject<HttpService>('httpService');

      if (!httpService) {
        throw new Error('HttpService is not available!');
      }

      const appearanceAdditionalInfo = ref<appearanceAdditionalInfoType[]>([]);
      const appearanceDocuments = ref<appearanceDocumentsType[]>([]);
      const appearanceParties = ref<appearancePartiesType[]>([]);
      const appearanceMethods = ref<appearanceMethodsType[]>([]);

      const loadingPdf = ref(false);
      const isMounted = ref(false);
      const appearanceDetailsJson = ref<any>(null);
      const additionalInfo = reactive<civilAppearanceDetailsInfoType>({
        supplementalEquipment: '',
        securityRestriction: '',
        outOfTownJudge: '',
      });
      const adjudicatorComment = ref('');
      const showAdjudicatorComment = ref(false);

      const addInfoFields = [
        { key: 'key', label: 'key', sortable: false },
        { key: 'value', label: 'value', sortable: false },
      ];

      const issueFields = [
        {
          key: 'result',
          label: 'Result',
          sortable: false,
          cellStyle: 'display: block; font-size: 14px;',
        },
        {
          key: 'issue',
          label: 'Issue',
          sortable: false,
          cellStyle:
            'font-weight: normal; font-size: 14px; padding-top:4px; line-height: 120%;',
        },
      ];

      const documentFields = [
        {
          key: 'seq',
          label: 'Seq.',
          sortable: false,
          tdClass: 'border-top',
          cellClass: 'text',
          cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px',
        },
        {
          key: 'documentType',
          label: 'Document Type',
          sortable: false,
          tdClass: 'border-top',
          cellClass: 'text',
          cellStyle: 'border:0px; font-size: 14px;',
          cellLabelStyle:
            'font-weight: normal; font-size: 14px; padding-top:12px',
        },
        {
          key: 'act',
          label: 'Act',
          sortable: false,
          tdClass: 'border-top',
          cellClass: 'badge badge-dark mt-2',
          cellStyle:
            'display: block; margin-top: 1px; font-size: 12px; max-width : 50px;',
        },
        {
          key: 'dateFiled',
          label: 'Date Filed',
          sortable: false,
          tdClass: 'border-top',
          cellClass: 'text',
          cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px',
        },
        {
          key: 'result',
          label: 'Result',
          sortable: false,
          tdClass: 'border-top',
          cellClass: 'badge badge-dark mt-2',
          cellStyle: 'display: block; margin-top: 1px; font-size: 14px;',
        },
      ];

      const partyFields = [
        {
          key: 'name',
          label: 'Name',
          sortable: false,
          tdClass: 'border-top',
          cellStyle: 'font-weight: bold; font-size: 14px;',
        },
        {
          key: 'role',
          label: 'Role',
          sortable: false,
          tdClass: 'border-top',
          cellStyle: 'font-size: 14px; white-space: pre-line;',
        },
        {
          key: 'currentCounsel',
          label: 'Current Counsel',
          sortable: false,
          tdClass: 'border-top',
          cellStyle: 'display: block; font-size: 14px; white-space: initial;',
        },
        {
          key: 'legalRepresentative',
          label: 'Legal Representative',
          sortable: false,
          tdClass: 'border-top',
          cellStyle: 'font-size: 14px; white-space: pre-line;',
        },
        {
          key: 'representative',
          label: 'Representative',
          sortable: false,
          tdClass: 'border-top',
          cellStyle: 'display: block; font-size: 14px; white-space: initial;',
        },
      ];

      const appearanceMethodsField = [
        {
          key: 'key',
          label: 'Key',
          cellClass: 'text-danger',
          cellStyle: 'white-space: pre-line',
        },
      ];

      const {
        displayName,
        courtRoomsAndLocations,
        userInfo,
        updateDisplayName,
      } = toRefs(commonStore);
      const { civilAppearanceInfo } = toRefs(civilFileStore);

      onMounted(() => {
        getAdditionalInfo();
        getAppearanceDetails();
      });

      const getAppearanceDetails = () => {
        httpService
          .get<any>(
            'api/files/civil/' +
              civilFileStore.civilAppearanceInfo.fileNo +
              '/appearance-detail/' +
              civilFileStore.civilAppearanceInfo.appearanceId
          )
          .then(
            (Response) => Response.json(),
            (err) => {
              // this.$bvToast.toast(
              //   `Error - ${err.url} - ${err.status} - ${err.statusText}`,
              //   {
              //     title: 'An error has occured.',
              //     variant: 'danger',
              //     autoHideDelay: 10000,
              //   }
              // );
              console.log(err);
            }
          )
          .then((data) => {
            if (data) {
              appearanceDetailsJson.value = data;
              ExtractAppearanceDetailsInfo();
              const element = document.getElementById(props.tagcasename);
              if (element != null)
                setTimeout(() => {
                  element.scrollIntoView();
                }, 100);
            } else {
              window.alert('bad data!');
            }
            isMounted.value = true;
          });
      };

      const getAdditionalInfo = () => {
        additionalInfo.supplementalEquipment = civilAppearanceInfo.value
          .supplementalEquipmentTxt
          ? civilAppearanceInfo.value.supplementalEquipmentTxt
          : '';
        additionalInfo.securityRestriction = civilAppearanceInfo.value
          .securityRestrictionTxt
          ? civilAppearanceInfo.value.securityRestrictionTxt
          : '';
        additionalInfo.outOfTownJudge = civilAppearanceInfo.value
          .outOfTownJudgeTxt
          ? civilAppearanceInfo.value.outOfTownJudgeTxt
          : '';

        for (const info in additionalInfo) {
          if (additionalInfo[info].length > 0) {
            appearanceAdditionalInfo.value.push({
              key: info,
              value: additionalInfo[info],
            });
          }
        }
      };

      const ExtractAppearanceDetailsInfo = () => {
        adjudicatorComment.value = appearanceDetailsJson.value
          .adjudicatorComment
          ? appearanceDetailsJson.value.adjudicatorComment
          : '';
        for (const documentIndex in appearanceDetailsJson.value.document) {
          const docInfo = {} as appearanceDocumentsType;
          const document = appearanceDetailsJson.value.document[documentIndex];
          docInfo.seq = document.fileSeqNo;
          docInfo.documentType = document.documentTypeDescription;
          docInfo.docTypeCd = document.documentTypeCd;
          docInfo.id = document.civilDocumentId;
          docInfo.pdfAvail = document.imageId ? true : false;
          docInfo.act = [];
          if (document.documentSupport && document.documentSupport.length > 0) {
            for (const act of document.documentSupport) {
              docInfo.act.push({ code: act.actCd, description: act.actDsc });
            }
          }

          if (document.sealedYN == 'Y') {
            docInfo.sealed = true;
          } else {
            docInfo.sealed = false;
          }

          docInfo.dateFiled = document.filedDt
            ? document.filedDt.split(' ')[0]
            : '';
          docInfo.result = document.appearanceResultCd;
          docInfo.resultDescription = document.appearanceResultDesc;
          docInfo.issues = [];
          docInfo.index = documentIndex;
          if (document.issue && document.issue.length > 0) {
            for (const issue of document.issue) {
              docInfo.issues.push({
                issue: issue.issueDsc,
                result: issue.issueResultCd,
                resultDsc: issue.issueResultDsc,
              });
            }
          }

          appearanceDocuments.value.push(docInfo);
        }

        for (const party of appearanceDetailsJson.value.party) {
          const partyInfo = {} as appearancePartiesType;
          partyInfo.firstName = party.givenNm ? party.givenNm : '';
          partyInfo.lastName = party.lastNm ? party.lastNm : party.orgNm;
          updateDisplayName.value({
            lastName: partyInfo.lastName,
            givenName: partyInfo.firstName,
          });
          partyInfo.name = displayName.value;
          partyInfo.info = '';
          if (party.appearanceMethodDesc) {
            partyInfo.info = 'Appeared by ' + party.appearanceMethodDesc;
          }
          if (party.partyAppearanceMethodDesc) {
            if (partyInfo.info.length > 0) {
              partyInfo.info += '<br>';
            }
            //TODO: remove the pre-text when the longDesc is passed through the api
            partyInfo.info += 'Appearance: ' + party.partyAppearanceMethodDesc;
          }
          if (party.attendanceMethodDesc) {
            if (partyInfo.info.length > 0) {
              partyInfo.info += '<br>';
            }
            partyInfo.info += 'Attendance: ' + party.attendanceMethodDesc;
          }
          partyInfo.currentCounsel = [];
          if (party.counsel && party.counsel.length > 0) {
            for (const counsel of party.counsel) {
              let info = '';
              if (counsel.phoneNumber) {
                info = 'Phone Number: ' + counsel.phoneNumber;
              }
              if (counsel.counselAppearanceMethodDesc) {
                if (info.length > 0) {
                  info += '\n';
                }
                info += 'Appeared by ' + counsel.counselAppearanceMethodDesc;
              }
              partyInfo.currentCounsel.push({
                name: counsel.counselFullName,
                info: info,
              });
            }
          }
          partyInfo.representative = [];
          if (party.representative && party.representative.length > 0) {
            for (const rep of party.representative) {
              let info = '';
              if (rep.phoneNumber) {
                info = 'Phone Number: ' + rep.phoneNumber;
              }
              if (rep.attendenceMethodDsc) {
                if (info.length > 0) {
                  info += '<br>';
                }
                info += 'Attended by ' + rep.attendanceMethodDesc;
              }
              if (rep.instruction) {
                if (info.length > 0) {
                  info += '<br>';
                }
                info += 'Instruction: ' + rep.instruction;
              }
              partyInfo.representative.push({
                name: rep.repFullName,
                info: info,
              });
            }
          }
          partyInfo.legalRepresentative = [];
          if (
            party.legalRepresentative &&
            party.legalRepresentative.length > 0
          ) {
            for (const legalRep of party.legalRepresentative) {
              partyInfo.legalRepresentative.push({
                name: legalRep.legalRepFullName,
                type: legalRep.legalRepTypeDsc,
              });
            }
          }
          partyInfo.role = [];
          if (party.partyRole && party.partyRole.length > 0) {
            for (const role of party.partyRole) {
              partyInfo.role.push(role.roleTypeDsc);
            }
          }
          appearanceParties.value.push(partyInfo);
        }

        for (const appearanceMethod of appearanceDetailsJson.value
          .appearanceMethod) {
          const methodInfo = {} as appearanceMethodsType;
          methodInfo.role = appearanceMethod.roleTypeDesc;
          methodInfo.method = appearanceMethod.appearanceMethodDesc;
          appearanceMethods.value.push(methodInfo);
        }
      };

      const documentClick = (document) => {
        console.log(document);
        console.log(appearanceDetailsJson.value);

        loadingPdf.value = true;
        const documentType =
          document.item == null
            ? CourtDocumentType.CSR
            : CourtDocumentType.Civil;
        const location = courtRoomsAndLocations.value.filter((location) => {
          return location.locationId == appearanceDetailsJson.value?.agencyId;
        })[0]?.name;
        const documentData: DocumentData = {
          appearanceId: document.appearanceId,
          appearanceDate: appearanceDetailsJson.value?.appearanceDt.substring(
            0,
            10
          ),
          courtLevel: appearanceDetailsJson.value?.courtLevelCd,
          dateFiled: document.item ? beautifyDate(document.item.dateFiled) : '',
          documentId: document.item ? document.item.id : '',
          documentDescription: document.item
            ? document.item.documentType
            : document.documentDescription,
          fileId: civilAppearanceInfo.value.fileNo,
          fileNumberText: appearanceDetailsJson.value.fileNumberTxt,
          location: location ? location : '',
        };
        console.log(documentData);
        shared.openDocumentsPdf(documentType, documentData);
        loadingPdf.value = false;
      };

      const OpenAdjudicatorComment = () => {
        showAdjudicatorComment.value = true;
      };

      return {
        isMounted,
        loadingPdf,
        documentClick,
        civilAppearanceInfo,
        appearanceDocuments,
        documentFields,
        beautifyDate,
        issueFields,
        OpenAdjudicatorComment,
        adjudicatorComment,
        appearanceAdditionalInfo,
        userInfo,
        addInfoFields,
        appearanceMethods,
        appearanceMethodsField,
        appearanceParties,
        partyFields,
        showAdjudicatorComment,
      };
    },
  });
</script>
