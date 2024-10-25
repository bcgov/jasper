<template>
    <div class="m-2">
        <br/>
        <div class="mx-auto bg-white">
            <b-nav vertical>
                <b-nav-item style="font-size: 10px; font-weight: bold;" disabled>ON THIS FILE</b-nav-item>
                    <b-nav-item  
                        v-for="(panelItem, index) in panelItems" 
                        :key="index" 
                        style="line-height: 1.25;"
                        v-on:click="SelectPanelItem(panelItem)" >
                            {{panelItem}}
                    </b-nav-item>    
            </b-nav>
        </div>
        <!-- <div class="mt-4">
            <b-button variant="outline-primary text-dark bg-warning" @click="navigateToHomePage()">
                <b-icon-house-door class="mr-1 ml-0" variant="dark" scale="1" ></b-icon-house-door>
                Return to Main Page
            </b-button>
        </div> -->
    </div>    
</template>

<script lang="ts">
import { Component, Vue } from 'vue-property-decorator';
import { namespace } from 'vuex-facing-decorator';
import '@store/modules/CivilFileInformation';
import {civilShowSectionsType} from '@/types/civil';
const civilState = namespace('CivilFileInformation');

@Component
export default class CivilSidePanel extends Vue {

    @civilState.State
    public showSections    
    
    @civilState.Action
    public UpdateShowSections!: (newShowSections: civilShowSectionsType) => void
    
    panelItems = [ 
       'Case Details', 'Future Appearances', 'Past Appearances', 'All Documents', 'Documents', 'Provided Documents'    
    ];

    public SelectPanelItem(panelItem) {

        const sections = this.showSections
    
        for(const item of this.panelItems)
        {
            if(item == panelItem )
                sections[item] = true;
            else
                sections[item] = false;
        }
        this.UpdateShowSections(sections);
    }

    public navigateToHomePage() {
        this.$router.push({name:'Home'})
    }

}
</script>