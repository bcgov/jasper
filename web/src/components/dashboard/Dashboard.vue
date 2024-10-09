<template>
    <div class="judges-dashboard">
        <header class="header">
            <div class="top-line">Court Today</div>
            <div class="bot-line">
                <div class="left">
                    Criminal
                </div>
                <div class="center">Scheduled:</div>
                <div class="right">Today's court list</div>
            </div>
        </header>
        <section class="dashboard-container">
            <div class="tools-container">
                <button class="filters" @click="toggleLeft()">
                    <img src="../../assets/filters.svg">
                    <span>
                        Filters
                    </span>
                </button>
                <button class="more" @click="toggleRight()">
                    <img src="../../assets/more3dots.svg">
                    <span>More</span>
                </button>
            </div>
            <div class="calendar-container">
                <div :class="{ 'left-menu-active': showLeftMenu, 'left-menu': !showLeftMenu }">
                    <div class="accordion-content">
                        <button class="accordion-button" v-b-toggle.locations variant="primary">
                            Locations
                            <img src="../../assets/arrow-down.svg">
                        </button>

                        <!-- Collapsible content -->
                        <b-collapse id="locations" class="mt-2">
                            <b-card>
                                <b-form-checkbox-group id="locations-filter-box" v-model="selectedLocations"
                                    :options="locations.length > 7 ? locations.slice(0, 7) : locations"
                                    name="locations-filter" @change="onCheckboxChange"></b-form-checkbox-group>
                            </b-card>
                            <button class="moreItems" @click="showAllLocations()">
                                <span>See all locations</span>
                            </button>
                        </b-collapse>
                    </div>


                    <div class="accordion-content">
                        <button class="accordion-button" v-b-toggle.presiders variant="primary">
                            Presiders
                            <img src="../../assets/arrow-down.svg">
                        </button>

                        <!-- Collapsible content -->
                        <b-collapse id="presiders" class="mt-2">
                            <b-card>
                                <b-form-checkbox-group id="presiders-filter-box" v-model="selectedPresiders"
                                    :options="presiders.length > 7 ? presiders.slice(0, 7) : presiders"
                                    name="presiders-filter" @change="onCheckboxPresidersChange"></b-form-checkbox-group>
                            </b-card>
                            <button class="moreItems" @click="showAllPresiders()">
                                <span>See all presiders</span>
                            </button>
                        </b-collapse>
                    </div>
                    <div class="accordion-content">
                        <button class="accordion-button" v-b-toggle.activities variant="primary">
                            Activities
                            <img src="../../assets/arrow-down.svg">
                        </button>

                        <!-- Collapsible content -->
                        <b-collapse id="activities" class="mt-2">
                            <b-card>
                                <b-form-checkbox-group id="activities-filter-box" v-model="selectedActivities"
                                    :options="activities.length > 7 ? activities.slice(0, 7) : activities"
                                    name="activities-filter"
                                    @change="onCheckboxActivitiesChange"></b-form-checkbox-group>
                            </b-card>
                            <button class="moreItems" @click="showAllActivities()">
                                <span>See all activities</span>
                            </button>
                        </b-collapse>
                    </div>
                </div>
                <div class="dashboard-calendar" :class="{ 'calendar-active': menuActive, '': !menuActive }">
                    <Calendar :events='arEvents' @monthChange="getMonthlySchedule" :sizeChange='sizeChange'
                        :locations='locations' />
                </div>
                <div :class="{ 'right-menu-active': showRightMenu, 'right-menu': !showRightMenu }">
                    <div>&nbsp;</div>
                    <div>&nbsp;</div>
                </div>
            </div>

        </section>
        <section class="dashboard-collapse-section">
            <div class="dashboard-collapse">Reserved Judgement (4)</div>
            <div class="dashboard-collapse">Reserved Judgement (5)</div>
        </section>
        <b-modal id="locations-click-modal" hide-footer centered size="lg" hide-backdrop ref="dayModal"
            v-model="showLocationModal">
            <div>
                <div class="modal-header-title">Locations</div>
                <div class="locations-columns">
                    <div class="column">
                        <b-form-checkbox-group id="locations-filter-box" v-model="selectedLocations"
                            :options="locations" name="locations-filter"
                            @change="onCheckboxChange"></b-form-checkbox-group>
                    </div>
                </div>
                <div class="modal-buttons">
                    <button class="moreItems" @click="resetLocations()">Reset</button>
                    <button class="modal-button-right">Save</button>
                </div>
            </div>
        </b-modal>
        <b-modal id="presiders-click-modal" hide-footer centered size="lg" hide-backdrop ref="dayModal"
            v-model="showPresiderModal">
            <div>
                <div class="modal-header-title">Presiders</div>
                <div class="presiders-columns">
                    <div class="column">
                        <b-form-checkbox-group id="presiders-filter-box" v-model="selectedPresiders"
                            :options="presiders" name="presiders-filter"
                            @change="onCheckboxPresidersChange"></b-form-checkbox-group>
                    </div>
                </div>
                <div class="modal-buttons">
                    <button class="moreItems" @click="resetPresiders()">Reset</button>
                    <button class="modal-button-right">Save</button>
                </div>
            </div>
        </b-modal>

        <b-modal id="activities-click-modal" hide-footer centered size="lg" hide-backdrop ref="dayModal"
            v-model="showActivitiesModal">
            <div>
                <div class="modal-header-title">Activities</div>
                <div class="activities-columns">
                    <div class="column">
                        <b-form-checkbox-group id="activities-filter-box" v-model="selectedActivities"
                            :options="activities" name="activities-filter"
                            @change="onCheckboxActivitiesChange"></b-form-checkbox-group>
                    </div>
                </div>
                <div class="modal-buttons">
                    <button class="moreItems" @click="resetActivities()">Reset</button>
                    <button class="modal-button-right">Save</button>
                </div>
            </div>
        </b-modal>


    </div>
</template>
<style>
@import url('https://fonts.googleapis.com/css2?family=Work+Sans:ital,wght@0,100..900;1,100..900&display=swap');

header {
    width: 100%;
    max-width: 1200px;
    margin: 0 auto;
}

button {
    outline: none !important;
}

.filters img,
.accordion-button img,
.more img {
    width: 20px;
}

.accordion-button collapsed,
.accordion-button collapsed {
    border: 0;
}

.accordion-content {
    border-bottom: 1px solid gray;
}

.left-menu-active .accordion-content {
    padding-bottom: 10px;
}

.modal-buttons {
    display: flex;
    align-items: center;
    width: 100%;
    margin: 30px auto 0 auto;
    justify-content: space-between;
}

.modal-header-title {
    font-family: "Work Sans", sans-serif;
    font-size: 24px;
    color: #183A4A;
    margin-bottom: 30px;
    margin-top: -10px;
}

.moreItems {
    font-family: "Work Sans", sans-serif;
    font-size: 16px;
    color: #183A4A;
    background: none;
    border: none;
    text-decoration: underline;
}

#locations-click-modal___BV_modal_header_,
#presiders-click-modal___BV_modal_header_,
#activities-click-modal___BV_modal_header_  {
    border-bottom: 0 !important;
    font-size: 30px;
}

#locations-click-modal #locations-filter-box,
#presiders-click-modal #presiders-filter-box,
#activities-click-modal #activities-filter-box  {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
}

#locations-click-modal .modal,
#presiders-click-modal .modal,
#activities-click-modal .modal {
    border-radius: 20px !important;
    padding: 10px;
    box-shadow: 4px 3px 6px 1px rgb(109 109 109 / 40%) !important;
}

.modal-button-right {
    cursor: pointer;
    padding: 8px 0;
    font-size: 16px;
    border: 1px solid #183A4A;
    width: 120px;
    background-color: #183A4A;
    color: #fff;
    border-radius: 20px;
    font-family: sans-serif;
    transition: all ease-in-out 0.4s;
    text-transform: capitalize;
}

.modal-button-right:hover {
    border: 1px solid #183A4A;
    background-color: #fff;
    color: #183A4A;
}

.moreItems:hover {
    text-decoration: none;
}

.top-line {
    background-color: rgba(157, 146, 146, 0.19);
    padding: 8px 15px;
    color: #fff;
}

.bot-line {
    display: flex;
    background-color: #8e8d8d;
    padding: 12px 15px;
    align-items: center;
    justify-content: space-between;
}

.calendar-search-checkbox {
    font-family: "Work Sans", sans-serif;
}

.card {
    border: none;

}

.accordion-button {
    width: 100%;
    border: 0;
    display: Flex;
    align-items: center;
    justify-content: space-between;
    background-color: transparent;
    font-family: "Work Sans", sans-serif;
    outline: none;
}

.dashboard-container {
    max-width: 1200px;
    margin: 20px auto;

}

.calendar-container {
    display: flex;
}

.dashboard-calendar {
    width: 100%;
}

.calendar-active {
    width: 80%;
}

.right-menu {
    display: none;

}

.right-menu-active {
    display: block;
    width: 19%;
    border: none;
    margin-top: 70px;
}

.left-menu {
    display: none;

}

.left-menu-active {
    display: block;
    width: 19%;
    border: none;
    margin-top: 70px;
    padding-right: 15px;
}

.dashboard-collapse-section {
    margin: 0 auto;
    max-width: 1200px;
    width: 100%;
    padding-bottom: 100px;
}

.dashboard-collapse {
    border-bottom: 1px solid #000;
    color: #000;
    padding: 10px;
    max-width: 500px;
}

.tools-container {
    max-width: 1200px;
    margin: 0 auto 0px auto;
    position: relative;
    display: Flex;
    align-items: center;
    justify-content: space-between;
}

.tools-container button {
    font-size: 16px;
    text-decoration: underline;
    color: #183A4A;
    font-family: "Work Sans", sans-serif;
    transition: all ease-in-out 0.4s;
    background: transparent;
    border: 0;
}

.tools-container button:hover {
    text-decoration: none;
}

.tools-container span {
    margin-left: 10px;
}
</style>
<script lang="ts">
import NavigationTopbar from "@components/NavigationTopbar.vue";
import Calendar from '@components/calendar/Calendar.vue';
import { Component, Vue } from 'vue-property-decorator';
@Component({
    components: {
        NavigationTopbar,
        Calendar
    }
})
export default class Dashboard extends Vue {
    public sizeChange = 0;
    public locationsFilters = [];
    public ar = [{ title: '', start: new Date() }];
    public arEvents = [{ title: '', start: new Date() }];
    public showLeftMenu = false;
    public showRightMenu = false;
    public menuActive = false;

    public locations = [];
    public selectedLocations = [];
    public showLocationModal = false;

    public presiders = [];
    public selectedPresiders = [];
    public showPresiderModal = false;

    public activities = [];
    public selectedActivities = [];
    public showActivitiesModal = false;

    courtRoomsAndLocationsJson: CourtRoomsJsonInfoType[] = [];
    courtRoomsAndLocations: courtRoomsAndLocationsInfoType[] = [];


    mounted() {
        this.loadLocations();
    }

    loadLocations() {
        this.locations = [{ text: 'Kelowna', value: 'Kelowna' }, { text: 'Quesnel', value: 'Quesnel' }, { text: 'Vanderhoof', value: 'Vanderhoof' }, { text: 'Vancouver', value: 'Vancouver' }, { text: 'Victoria', value: 'Victoria' }, { text: 'Vernon', value: 'Vernon' }, { text: 'Kamploops', value: 'Kamploops' }, { text: 'Williams Lake', value: 'Williams Lake' }
            , { text: 'Kelowna', value: 'Kelowna' }, { text: 'Quesnel', value: 'Quesnel' }, { text: 'Vanderhoof', value: 'Vanderhoof' }, { text: 'Vancouver', value: 'Vancouver' }, { text: 'Victoria', value: 'Victoria' }, { text: 'Vernon', value: 'Vernon' }, { text: 'Kamploops', value: 'Kamploops' }, { text: 'Williams Lake', value: 'Williams Lake' },
        { text: 'Kelowna', value: 'Kelowna' }, { text: 'Quesnel', value: 'Quesnel' }, { text: 'Vanderhoof', value: 'Vanderhoof' }, { text: 'Vancouver', value: 'Vancouver' }, { text: 'Victoria', value: 'Victoria' }, { text: 'Vernon', value: 'Vernon' }, { text: 'Kamploops', value: 'Kamploops' }, { text: 'Williams Lake', value: 'Williams Lake' }];

        this.loadPresiders();
        this.loadActivities();
    }
    showAllLocations() {
        this.showLocationModal = true;
    }
    resetLocations() {
        this.selectedLocations = [];
    }
    loadPresiders() {
        this.presiders = [{ text: 'Kelowna', value: 'Kelowna' }, { text: 'Quesnel', value: 'Quesnel' }];
    }
    showAllPresiders() {
        this.showPresiderModal = true;
    }
    resetPresiders() {
        this.selectedPresiders = [];
    }
    loadActivities() {
        this.activities = [{ text: 'Kelowna', value: 'Kelowna' }, { text: 'Quesnel', value: 'Quesnel' }];
    }
    showAllActivities() {
        this.showActivitiesModal = true;
    }
    resetActivities() {
        this.selectedActivities = [];
    }

    toggleRight() {
        this.arEvents = [...this.arEvents];
        this.showRightMenu = !this.showRightMenu;
        this.showLeftMenu = false;
        this.menuActive = this.showRightMenu;
        this.sizeChange = this.sizeChange++;
    }
    toggleLeft() {
        this.arEvents = [...this.arEvents];
        this.showLeftMenu = !this.showLeftMenu;
        this.showRightMenu = false;
        this.menuActive = this.showLeftMenu;
        this.sizeChange = this.sizeChange++;
    }

    public getMonthlySchedule(currentMonth): void {

        this.ar = [];
        this.$http
            .get(
                "api/assignments/monthly-schedule/" +
                `${currentMonth.getFullYear()}/${String(currentMonth.getMonth() + 1).padStart(2, '0')}`
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
                    this.ar = data;
                    // console.log('2BE data', data);
                    if (currentMonth.getMonth() + 1 === 10) {
                        this.ar = [{ title: 'CrimRem', start: new Date(), location: 'Room 300', city: 'Kelowna' },
                        { title: 'Trials', start: new Date(new Date().setDate(new Date().getDate() + 10)), city: 'Vernon', location: 'Room 102' },
                        { title: 'Pre-trial', city: 'Vernon', location: 'Room 102', start: new Date(new Date().setDate(new Date().getDate() + 7)) },
                        { title: 'Pre-trial', city: 'Vernon', location: 'Room 102', start: new Date(new Date().setDate(new Date().getDate() + 5)) },
                        { title: 'DSP', location: 'Room 300', city: 'Kelowna', start: new Date(new Date().setDate(new Date().getDate() - 1)) },
                        { title: 'Trials', location: 'Room 300', start: new Date(new Date().setDate(new Date().getDate() - 2)) }];


                    }
                    else {
                        this.ar = [{ title: 'CrimRem', location: 'Room 300', city: 'Kelowna', start: new Date(new Date().setDate(new Date().getDate() + 48)) },
                        { title: 'CrimRem', location: 'Room 300', city: 'Kelowna', start: new Date(new Date().setDate(new Date().getDate() + 45)) },
                        { title: 'Trials', location: 'Room 102', city: 'Vernon', start: new Date(new Date().setDate(new Date().getDate() + 46)) },
                        { title: 'Trials', location: 'Room 102', city: 'Vernon', start: new Date(new Date().setDate(new Date().getDate() + 48)) }];

                    }

                    this.arEvents = [...this.ar];

                    //   console.log('this.ar', this.ar);
                } else {
                    window.alert("bad data!");
                }

            });
    }

    public onCheckboxChange(selected) {
        this.selectedLocations = [...selected];

        if (this.selectedLocations.length) {
            this.arEvents = this.ar.filter((event) => this.selectedLocations.includes(event.city));
        } else {
            this.arEvents = [...this.ar];
        }

        this.loadPresiders();
        this.loadActivities();
    }

    public onCheckboxPresidersChange(selected) {
        this.selectedPresiders = [...selected];
    }
    public onCheckboxActivitiesChange(selected) {
        this.selectedActivities = [...selected];
    }
    public ExtractCourtRoomsAndLocationsInfo() {
        //   for (const jroomAndLocation of this.courtRoomsAndLocationsJson) {
        //   if (jroomAndLocation.courtRooms.length > 0) {
        //   const roomAndLocationInfo = {} as courtRoomsAndLocationsInfoType;
        //  roomAndLocationInfo.text = jroomAndLocation.name + " (" + jroomAndLocation.code + ")";
        //   roomAndLocationInfo.value = {} as locationInfoType;
        //   roomAndLocationInfo.value.Location = jroomAndLocation.name;
        //   roomAndLocationInfo.value.LocationID = jroomAndLocation.code;
        //    this.courtRoomsAndLocations.push(roomAndLocationInfo);
        //   }
        //  }
        //  this.courtRoomsAndLocations = _.sortBy(this.courtRoomsAndLocations, "text");
        //  this.selectedCourtLocation = this.courtRoomsAndLocations[0].value;
    }




}
</script>