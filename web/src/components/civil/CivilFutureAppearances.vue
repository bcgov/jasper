<template>
<b-card bg-variant="white" no-body>
    <div>
        <h3 class="mx-4 font-weight-normal" v-if="!showSections['Future Appearances']"> Next Three Future Appearances</h3>
        <hr class="mx-3 bg-light" style="height: 5px;"/> 
    </div>

    <b-card v-if="!isDataReady && isMounted">
        <span class="text-muted ml-4 mb-5"> No future appearances. </span>
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
        :items="SortedFutureAppearances"
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
                    <b-button style="transform: translate(-2px,-7px);font-size:14px;" 
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
import {InputNamesType, DurationType } from '@/types/common';
import { civilApprDetailType } from "@/types/civil/jsonTypes";
const civilState = namespace("CivilFileInformation");
const commonState = namespace("CommonInformation");

enum appearanceStatus {UNCF='Unconfirmed', CNCL='Canceled', SCHD='Scheduled' }

@Component({
    components: {
        CivilAppearanceDetails
    }
})
export default class CivilFutureAppearances extends Vue {

    @commonState.State
    public displayName!: string;

    @civilState.State
    public showSections

    @commonState.State
    public duration

    @commonState.State
    public time

    @commonState.State
    public statusStyle
   
    @civilState.State
    public civilFileInformation!: civilFileInformationType;
    
    @civilState.State
    public civilAppearanceInfo!: civilAppearanceInfoType;

    @civilState.Action
    public UpdateCivilAppearanceInfo!: (newCivilAppearanceInfo: civilAppearanceInfoType) => void        

    @commonState.Action
    public UpdateDisplayName!: (newInputNames: InputNamesType) => void    

    @commonState.Action
    public UpdateDuration!: (duration: DurationType) => void    

    @commonState.Action
    public UpdateTime!: (time: string) => void
    
    @commonState.Action
    public UpdateStatusStyle!: (statusStyle: string) => void

    futureAppearancesList: civilAppearancesListType[] = [];
   
    isMounted = false;
    isDataReady = false;
    futureAppearancesJson: civilApprDetailType[] = [];    
    sortBy = 'date';
    sortDesc = true;    

    fields =  
    [
        {key:'date',            label:'Date',            sortable:true,  tdClass: 'border-top', headerStyle:'text-primary',  cellClass:'text-info mt-2 d-inline-flex', cellStyle: 'display: inline-flex; font-size: 14px;'},
        {key:'reason',          label:'Reason',          sortable:true,  tdClass: 'border-top', headerStyle:'text-primary',  cellClass:'badge badge-secondary mt-2',   cellStyle: 'font-size: 14px; text-align:left;'},
        {key:'documentType',    label:'Document Type',   sortable:false, tdClass: 'border-top', headerStyle:'text',          cellClass:'text',                         cellStyle: 'font-weight: normal;font-size: 14px; padding-top:12px'},
        {key:'result',          label:'Result',          sortable:true,  tdClass: 'border-top', headerStyle:'text-primary',  cellClass:'badge badge-secondary mt-2',   cellStyle: 'font-size: 14px;'},
        {key:'time',            label:'Time',            sortable:false, tdClass: 'border-top', headerStyle:'text',          cellClass:'text',                         cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px'},
        {key:'duration',        label:'Duration',        sortable:false, tdClass: 'border-top', headerStyle:'text',          cellClass:'text',                         cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px'},
        {key:'location',        label:'Location',        sortable:true,  tdClass: 'border-top', headerStyle:'text-primary',  cellClass:'text',                         cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px'},
        {key:'room',            label:'Room',            sortable:false, tdClass: 'border-top', headerStyle:'text',          cellClass:'text',                         cellStyle: 'font-weight: normal; font-size: 14px; padding-top:12px'},
        {key:'presider',        label:'Presider',        sortable:true,  tdClass: 'border-top', headerStyle:'text-primary',  cellClass:'badge badge-secondary mt-2',   cellStyle: 'font-size: 14px;'},        
        {key:'status',          label:'Status',          sortable:true,  tdClass: 'border-top', headerStyle:'text-primary',  cellClass:'badge',                        cellStyle: 'font-size: 14px;'}
    ];
    
     mounted() {
        this.getFutureAppearances();
    }

    public getFutureAppearances(): void {      
    
        const data = this.civilFileInformation.detailsData;
        this.futureAppearancesJson = data.appearances.apprDetail;              
        this.ExtractFutureAppearancesInfo();
        if(this.futureAppearancesList.length) {                    
            this.isDataReady = true;
        }
    
        this.isMounted = true;           
    }  
  
    public ExtractFutureAppearancesInfo(): void {
        const currentDate = new Date();
        for (const appIndex in this.futureAppearancesJson) {
            const appInfo = {} as civilAppearancesListType;
            const jApp = this.futureAppearancesJson[appIndex];

            appInfo.index = appIndex;
            appInfo.date = jApp.appearanceDt.split(' ')[0]
            if(new Date(appInfo.date) < currentDate) continue;            
            appInfo.formattedDate = Vue.filter('beautify_date')(appInfo.date);
            appInfo.documentType = jApp.documentTypeDsc?jApp.documentTypeDsc:'';
            appInfo.result = jApp.appearanceResultCd;
            appInfo.resultDescription = jApp.appearanceResultDsc? jApp.appearanceResultDsc: '';
            appInfo.time = this.getTime(jApp.appearanceTm.split(' ')[1].substr(0,5));
            appInfo.reason = jApp.appearanceReasonCd;
            appInfo.reasonDescription = jApp.appearanceReasonDsc? jApp.appearanceReasonDsc: '';
            appInfo.duration = this.getDuration(jApp.estimatedTimeHour, jApp.estimatedTimeMin)           
            appInfo.location = jApp.courtLocation? jApp.courtLocation: '';
            appInfo.room =jApp.courtRoomCd;              
            appInfo.status = jApp.appearanceStatusCd ? appearanceStatus[jApp.appearanceStatusCd] :''
            appInfo.statusStyle = this.getStatusStyle(appInfo.status);
            appInfo.presider =  jApp.judgeInitials ? jApp.judgeInitials :''
            appInfo.judgeFullName =  jApp.judgeInitials ? jApp.judgeFullNm : ''
            appInfo.appearanceId = jApp.appearanceId;
            appInfo.supplementalEquipment = jApp.supplementalEquipmentTxt;
            appInfo.securityRestriction = jApp.securityRestrictionTxt;
            appInfo.outOfTownJudge = jApp.outOfTownJudgeTxt;
                       
            this.futureAppearancesList.push(appInfo); 
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
        this.SortedFutureAppearances.forEach((item) => {
            this.$set(item, '_showDetails', false)
        })
    }

    get SortedFutureAppearances()
    {           
        if(this.showSections['Future Appearances'])
        {
            return this.futureAppearancesList;
        }
        else
        {
            return _.sortBy(this.futureAppearancesList,"date").reverse().slice(0, 3);           
        }   
    }
}
</script>

<style scoped>
 .card {
        border: white;
    }
</style>