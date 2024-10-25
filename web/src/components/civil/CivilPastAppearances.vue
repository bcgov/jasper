<template>
<b-card bg-variant="white" no-body>
    <div>
        <h3 class="mx-4 font-weight-normal" v-if="!showSections['Past Appearances']"> Last Three Past Appearances</h3>
        <hr class="mx-3 bg-light" style="height: 5px;"/> 
    </div>

    <b-card v-if="!isDataReady && isMounted">
        <span class="text-muted ml-4 mb-5"> No past appearances. </span>
    </b-card>

    <b-card bg-variant="light" v-if= "!isMounted && !isDataReady">
        <b-overlay :show= "true"> 
            <b-card  style="min-height: 100px;"/>                   
            <template v-slot:overlay>               
            <div> 
                    <loading-spinner/> 
                    <p id="loading-label">Loading ...</p>
            </div>                
            </template> 
        </b-overlay> 
    </b-card>

    <b-card bg-variant="white" v-if="isDataReady" style="overflow: auto;" no-body class="mx-3 mb-5">           
        <b-table
        :items="SortedPastAppearances"
        :fields="fields"
        :sort-by.sync="sortBy"
        :sort-desc.sync="sortDesc"
        :no-sort-reset="true"
        sort-icon-left
        borderless
        @sort-changed="sortChanged"
        small
        responsive="sm"
        >   
            <template v-for="(field,index) in fields" v-slot:[`head(${field.key})`]="data">
                <b v-bind:key="index" :class="field.headerStyle" > {{ data.label }}</b>
            </template>

            <template  v-slot:cell()="data">
                <b-badge                        
                    :style="data.field.cellStyle" 
                    variant="white" > 
                        {{data.value}} 
                </b-badge>
            </template>

            <template v-slot:cell(date)="data" >
                <span :class="data.field.cellClass" :style="data.field.cellStyle"> 
                    <b-button 
                        style="transform: translate(-2px,-7px); font-size:14px;" 
                        size="sm" 
                        @click="OpenDetails(data);data.toggleDetails();" 
                        variant="outline-primary border-white  text-info" 
                        class="mr-2">
                        <b-icon-caret-right-fill v-if="!data.item['_showDetails']"></b-icon-caret-right-fill>
                        <b-icon-caret-down-fill v-if="data.item['_showDetails']"></b-icon-caret-down-fill>
                        {{data.item.formattedDate}}
                    </b-button>
                </span> 
            </template>
            <template v-slot:row-details>
                <civil-appearance-details/>
            </template>

            <template  v-slot:cell(reason)="data">
                <b-badge
                        :class="data.field.cellClass"
                        variant="secondary"
                        v-b-tooltip.hover.right                            
                        :title="data.item.reasonDescription"
                        :style="data.field.cellStyle">  
                        {{data.value}}
                </b-badge>
            </template>

            <template v-if="fromA2a" v-slot:cell(summary)="data">
                <span :class="data.field.cellClass" :style="data.field.cellStyle"> 
                    <b-button
                        variant="outline-primary text-info"
                        style="transform:translate(0,5px);border:0px;"
                        class="mt-0"
                        v-b-tooltip.hover.right
                        title="Download Summary Sheet"
                        @click="documentClick(
                            {
                                appearanceId: data.item.appearanceId,
                                appearanceDate: data.item.date,
                                documentDescription: 'CourtSummary',
                            },
                            data.item.appearanceId
                      )"
                        size="sm"
                    >
                        <b-icon icon="file-earmark-arrow-down" font-scale="2"></b-icon>
                    </b-button>
                </span>
            </template>

            <template v-slot:cell(result)="data" >
                <span
                        v-if="data.value"
                        :class="data.field.cellClass"
                        variant="outline-primary border-white"
                        v-b-tooltip.hover.right                            
                        :title="data.item.resultDescription"
                        :style="data.field.cellStyle"> 
                        {{data.value}}
                </span>
            </template>

            <template v-slot:cell(presider)="data">
                <b-badge                              
                        variant="secondary"
                        v-if="data.value"
                        :class="data.field.cellClass"
                        :style="data.field.cellStyle"
                        v-b-tooltip.hover.left                           
                        :title="data.item.judgeFullName"> 
                        {{data.value}}
                </b-badge>
            </template>                

            <template v-slot:cell(status)="data">
                <b :class = "data.item.statusStyle" :style="data.field.cellStyle"> {{data.value}} </b>
            </template>
            
        </b-table>
    </b-card>
</b-card>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import * as _ from 'underscore';
import CivilAppearanceDetails from '@components/civil/CivilAppearanceDetails.vue';
import "@store/modules/CommonInformation";
import "@store/modules/CivilFileInformation";
import {civilFileInformationType, civilAppearanceInfoType, civilAppearancesListType} from '@/types/civil';
import { CourtRoomsJsonInfoType, InputNamesType, DurationType } from '@/types/common'
import { civilApprDetailType } from "@/types/civil/jsonTypes";
import { CourtDocumentType, DocumentData } from "@/types/shared";
import shared from "../shared";
const civilState = namespace("CivilFileInformation");
const commonState = namespace("CommonInformation");


enum appearanceStatus {UNCF='Unconfirmed', CNCL='Canceled', SCHD='Scheduled' }

@Component({
    components: {
        CivilAppearanceDetails
    }
})
export default class CivilPastAppearances extends Vue {    

    @civilState.State
    public showSections

     @commonState.State
    public statusStyle 

    @commonState.State
    public displayName!: string;    

    @commonState.State
    public duration    

    @commonState.State
    public time

    @civilState.State
    public civilAppearanceInfo!: civilAppearanceInfoType;

    @civilState.State
    public civilFileInformation!: civilFileInformationType;

    @commonState.State
    public courtRoomsAndLocations!: CourtRoomsJsonInfoType[];

    @civilState.Action
    public UpdateCivilAppearanceInfo!: (newCivilAppearanceInfo: civilAppearanceInfoType) => void    

    @commonState.Action
    public UpdateTime!: (time: string) => void

     @commonState.Action
    public UpdateDisplayName!: (newInputNames: InputNamesType) => void
    
    @commonState.Action
    public UpdateDuration!: (duration: DurationType) => void   
    
    @commonState.Action
    public UpdateStatusStyle!: (statusStyle: string) => void

    pastAppearancesList: civilAppearancesListType[] = [];

    isMounted = false;
    isDataReady = false;
    pastAppearancesJson: civilApprDetailType[] = [];    
    sortBy = 'date';
    sortDesc = true;    
    fromA2a = false;

    fields =  
    [
        {key:'date',           label:'Date',            sortable:true,  tdClass: 'border-top', headerStyle:'text-primary',  cellClass:'text-info mt-2 d-inline-flex', cellStyle: 'display: inline-flex; font-size: 14px;'},
        {key:'reason',         label:'Reason',          sortable:true,  tdClass: 'border-top', headerStyle:'text-primary',  cellClass:'badge badge-secondary mt-2',   cellStyle: 'font-size: 14px;'},
        {key:'documentType',   label:'Document Type',   sortable:false, tdClass: 'border-top', headerStyle:'text',          cellClass:'text',                         cellStyle: 'font-weight: normal;font-size: 14px; padding-top:12px;text-align:left;'},
        {key:'result',         label:'Result',          sortable:true,  tdClass: 'border-top', headerStyle:'text-primary',  cellClass:'badge badge-secondary mt-2',   cellStyle: 'font-size: 14px;'},
        {key:'time',           label:'Time',            sortable:false, tdClass: 'border-top', headerStyle:'text',          cellClass:'text',                         cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px'},
        {key:'duration',       label:'Duration',        sortable:false, tdClass: 'border-top', headerStyle:'text',          cellClass:'text',                         cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px'},
        {key:'location',       label:'Location',        sortable:true,  tdClass: 'border-top', headerStyle:'text-primary',  cellClass:'text',                         cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px'},
        {key:'room',           label:'Room',            sortable:false, tdClass: 'border-top', headerStyle:'text',          cellClass:'text',                         cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px'},
        {key:'presider',       label:'Presider',        sortable:true,  tdClass: 'border-top', headerStyle:'text-primary',  cellClass:'badge badge-secondary mt-2',   cellStyle: 'font-size: 14px;'},        
        {key:'status',         label:'Status',          sortable:true,  tdClass: 'border-top', headerStyle:'text-primary',  cellClass:'badge',                        cellStyle: 'font-size: 14px; width:110px;'}
    ];
    
    mounted() {
        this.getPastAppearances();
        this.isFromA2a();
    }

    public getPastAppearances(): void {
        const data = this.civilFileInformation.detailsData;
        this.pastAppearancesJson = data.appearances.apprDetail;              
        this.ExtractPastAppearancesInfo();
        if(this.pastAppearancesList.length) {                    
            this.isDataReady = true;
        }
        this.isMounted = true;           
    }
  
    public ExtractPastAppearancesInfo(): void {
        const currentDate = new Date();
        for (const appIndex in this.pastAppearancesJson) {
            const appInfo = {} as civilAppearancesListType;
            const jApp = this.pastAppearancesJson[appIndex];

            appInfo.index = appIndex;
            appInfo.date = jApp.appearanceDt.split(' ')[0]
            if(new Date(appInfo.date) >= currentDate) continue;            
            appInfo.formattedDate = Vue.filter('beautify_date')(appInfo.date);
            appInfo.documentType = jApp.documentTypeDsc? jApp.documentTypeDsc: '';
            appInfo.result = jApp.appearanceResultCd;
            appInfo.resultDescription = jApp.appearanceResultDsc? jApp.appearanceResultDsc: '';
            appInfo.time = this.getTime(jApp.appearanceTm.split(' ')[1].substr(0,5));
            appInfo.reason = jApp.appearanceReasonCd;
            appInfo.reasonDescription = jApp.appearanceReasonDsc? jApp.appearanceReasonDsc: '';
            appInfo.duration = this.getDuration(jApp.estimatedTimeHour, jApp.estimatedTimeMin)           
            appInfo.location = jApp.courtLocation? jApp.courtLocation: '';
            appInfo.room =jApp.courtRoomCd              
            appInfo.status = jApp.appearanceStatusCd ? appearanceStatus[jApp.appearanceStatusCd] :''
            appInfo.statusStyle = this.getStatusStyle(appInfo.status)
            appInfo.presider =  jApp.judgeInitials ? jApp.judgeInitials :''
            appInfo.judgeFullName =  jApp.judgeInitials ? jApp.judgeFullNm : ''

            appInfo.appearanceId = jApp.appearanceId
            appInfo.supplementalEquipment = jApp.supplementalEquipmentTxt
            appInfo.securityRestriction = jApp.securityRestrictionTxt
            appInfo.outOfTownJudge = jApp.outOfTownJudgeTxt
                       
            this.pastAppearancesList.push(appInfo); 
        }
    }

    public getStatusStyle(status) {
        this.UpdateStatusStyle(status);
        return this.statusStyle;
    }

    public getTime(time) {
        this.UpdateTime(time);
        return this.time;     
    }

    public getDuration(hr, min) {
        this.UpdateDuration({'hr': hr, 'min': min});
        return this.duration;
    }

    public OpenDetails(data)
    {
        if(!data.detailsShowing)
        {
            this.civilAppearanceInfo.fileNo = this.civilFileInformation.fileNumber;

            this.civilAppearanceInfo.date = data.item.formattedDate;
            this.civilAppearanceInfo.appearanceId = data.item.appearanceId;
            this.civilAppearanceInfo.supplementalEquipmentTxt = data.item.supplementalEquipment;
            this.civilAppearanceInfo.securityRestrictionTxt = data.item.securityRestriction;
            this.civilAppearanceInfo.outOfTownJudgeTxt = data.item.outOfTownJudge;

            this.UpdateCivilAppearanceInfo(this.civilAppearanceInfo);
        }
        
    }

    public sortChanged() 
    {
        this.SortedPastAppearances.forEach((item) => {
            this.$set(item, '_showDetails', false)
        })
    }

    get SortedPastAppearances()
    {           
        if(this.showSections['Past Appearances'])
        {
            return this.pastAppearancesList;
        }
        else
        {
            return _.sortBy(this.pastAppearancesList,"date").reverse().slice(0, 3);           
        }     
    }

    public documentClick(document, appearanceId) {
        this.$http
            .get(
                "api/files/civil/" +
                this.civilFileInformation.fileNumber +
                "/appearance-detail/" +
                appearanceId
            )
            .then(
                (Response) => Response.json(),
                (err) => {
                    this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
                        title: "An error has occured.",
                        variant: "danger",
                        autoHideDelay: 10000,
                    });
                console.log(err);
                }
            )
            .then((data) => {
                if (data) {
                    const documentType = document.item == null ? CourtDocumentType.CSR : CourtDocumentType.Civil;
                    const location = this.courtRoomsAndLocations.filter((location) => {
                        return location.locationId == data?.agencyId;
                    })[0]?.name;
                    const documentData: DocumentData = {
                        appearanceId: document.appearanceId,
                        appearanceDate: data?.appearanceDt.substring(0, 10),
                        courtLevel: data?.courtLevelCd,
                        dateFiled: document.item ? Vue.filter("beautify_date")(document.item.dateFiled) : "",
                        documentId: document.item ? document.item.id : "",
                        documentDescription: document.item ? document.item.documentType : document.documentDescription,
                        fileId: this.civilFileInformation.fileNumber,
                        fileNumberText: data.fileNumberTxt,
                        location: location ? location : "",
                    };
                    console.log(documentData);
                    shared.openDocumentsPdf(documentType, documentData);
                } else {
                    window.alert("bad data!");
                }
            });
    }

    public isFromA2a() {
        if (this.$route.query?.fromA2A && this.fromA2a == false) {
            this.fromA2a = true;
            const newFields: any[] = [];
            this.fields.forEach( (field) => {
                if (field.key == "result") {
                    newFields.push({
                        key:'summary',
                        label:'Court Summary',
                        sortable:false,
                        tdClass: 'border-top',
                        headerStyle:'text',
                        cellClass:'text',
                        cellStyle: 'font-weight: normal;font-size: 14px; padding-top:12px;text-align:left;'
                    });
                }
                newFields.push(field);
            });
            this.fields = newFields;
        }
    }
}
</script>

<style scoped>
 .card {
        border: white;
    }
</style>