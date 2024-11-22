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
                    <img src="../../assets/filters.svg" alt="filters">
                    <span>
                        Filters
                    </span>
                </button>
                <button class="more" @click="toggleRight()">
                    <img src="../../assets/more3dots.svg" alt="more">
                    <span>More</span>
                </button>
            </div>
            <div class="calendar-container">
                <div :class="{ 'left-menu-active': showLeftMenu, 'left-menu': !showLeftMenu }">
                    <div class="accordion-content">
                        <button class="accordion-button" v-b-toggle.locations variant="primary">
                            Locations
                            <img src="../../assets/arrow-down.svg" alt="more">
                        </button>

                        <!-- Collapsible content -->
                        <b-collapse id="locations" class="mt-2">
                            <b-card>
                                <b-form-checkbox-group id="locations-filter-box" v-model="selectedLocations"
                                    :options="locations.length > 7 ? locations.slice(0, 7) : locations"
                                    name="locations-filter" @change="onCheckboxChange"></b-form-checkbox-group>
                            </b-card>
                            <button v-if="locations.length > 0" class="moreItems" @click="showAllLocations()">
                                <span>See all locations</span>
                            </button>
                        </b-collapse>
                    </div>

                    <div class="accordion-content" v-if="selectedLocations.length > 0">
                        <button class="accordion-button" v-b-toggle.presiders variant="primary">
                            Presiders
                            <img src="../../assets/arrow-down.svg" alt="more">
                        </button>

                        <!-- Collapsible content -->
                        <b-collapse id="presiders" class="mt-2">
                            <b-card>
                                <b-form-checkbox id="select-all-presiders" v-model="selectAllPresiders"
                                    @change="onSelectAllPresidersChange">
                                    All Persiders
                                </b-form-checkbox>
                                <b-form-checkbox-group id="presiders-filter-box" v-model="selectedPresiders"
                                    :options="presiders.length > 7 ? presiders.slice(0, 7) : presiders"
                                    name="presiders-filter" @change="onCheckboxPresidersChange"></b-form-checkbox-group>
                            </b-card>
                            <button v-if="presiders.length > 0" class="moreItems" @click="showAllPresiders()">
                                <span>See all presiders</span>
                            </button>
                        </b-collapse>
                    </div>
                    <div class="accordion-content" v-if="selectedLocations.length > 0">
                        <button class="accordion-button" v-b-toggle.activities variant="primary">
                            Activities
                            <img src="../../assets/arrow-down.svg" alt="more">
                        </button>

                        <!-- Collapsible content -->
                        <b-collapse id="activities" class="mt-2">
                            <b-card>
                                <b-form-checkbox id="select-all-activities" v-model="selectAllActivities"
                                    @change="onSelectAllActivitiesChange">
                                    All Activities
                                </b-form-checkbox>
                                <b-form-checkbox-group id="activities-filter-box" v-model="selectedActivities"
                                    :options="activities.length > 7 ? activities.slice(0, 7) : activities"
                                    name="activities-filter"
                                    @change="onCheckboxActivitiesChange"></b-form-checkbox-group>
                            </b-card>
                            <button class="moreItems" @click="showAllActivities()">
                                <span v-if="activities.length > 1">See all activities</span>
                            </button>
                        </b-collapse>
                    </div>
                    <div class="activitiesPanel" v-if="selectedLocations.length > 0">
                        <b-form-checkbox class="singleCheckbox" id="sittingActivities" v-model="sittingActivities">
                            Show sitting activities
                        </b-form-checkbox>
                    </div>
                    <div v-if="selectedLocations.length > 0">
                        <b-form-checkbox class="singleCheckbox" id="nonSittingActivities"
                            v-model="nonSittingActivities">
                            Show non-sitting activities
                        </b-form-checkbox>
                    </div>

                    <div> <button class="defaultButton">
                            <span>Back to my calendar</span>
                        </button>

                        <button class="inverseButton">
                            <span>Favourite current filters</span>
                        </button>

                    </div>


                </div>
                <div class="dashboard-calendar" :class="{ 'calendar-active': menuActive, '': !menuActive }">
                    <Calendar :events='arEvents' @monthChange="getMonthlySchedule" :sizeChange='sizeChange'
                        :locations='locations' :isMySchedule='isMySchedule' />
                </div>
                <div :class="{ 'right-menu-active': showRightMenu, 'right-menu': !showRightMenu }">
                    <div>&nbsp;</div>
                    <div>&nbsp;</div>
                </div>
            </div>

        </section>
        <section class="dashboard-collapse-section">
            <div class="dashboard-collapse">Reserved Judgement (5)</div>
            <div class="dashboard-collapse">Reserved Judgement (5)</div>
        </section>
        <b-modal id="locations-click-modal" hide-footer centered size="lg" ref="locationsModal"
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
        <b-modal id="presiders-click-modal" hide-footer centered size="lg" hide-backdrop ref="presidersModal"
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

        <b-modal id="activities-click-modal" hide-footer centered size="lg" hide-backdrop ref="activitiesModal"
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

.accordion-button.collapsed {
    border: 0;
}

.accordion-content {
    border-bottom: 1px solid gray;
}

.singleCheckbox {
    display: flex;
    align-items: center;
    justify-content: flex-start;
    margin-left: 15px;
    margin-top: 5px;
}

.activitiesPanel {
    margin-top: 25px;
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
#activities-click-modal___BV_modal_header_ {
    border-bottom: 0 !important;
    font-size: 30px;
}

#locations-click-modal #locations-filter-box,
#presiders-click-modal #presiders-filter-box,
#activities-click-modal #activities-filter-box {
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

.accordion-content div[role='group'] {
    display: flex !important;
    align-items: flex-start;
    justify-content: flex-start;
    flex-direction: column;

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
    max-width: 1440px;
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
    max-width: 1440px;
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

.inverseButton:hover,
.defaultButton {
    font-family: "Work Sans", sans-serif;
    font-size: 16px;
    color: #fff;
    background-color: #183A4A;
    border-radius: 20px;
    border: 1px solid transparent;
}

.inverseButton,
.defaultButton:hover {
    background-color: #fff;
    font-family: "Work Sans", sans-serif;
    font-size: 16px;
    color: #183A4A;
    border: 1px solid #183A4A
}

.defaultButton {
    margin: 90px auto 10px auto;
    padding: 5px 10px;
    width: 90%;
    border-radius: 20px;
}

.inverseButton {
    border-radius: 20px;
    width: 90%;
    margin: 0 auto;
    padding: 5px 10px;
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
    public ar: any[] = [];
    public arEvents: any[] = [];
    public showLeftMenu = false;
    public showRightMenu = false;
    public menuActive = false;

    public locations: any[] = [];
    public selectedLocations: any[] = [];
    public showLocationModal = false;

    public presiders: any[] = [];
    public selectedPresiders: any[] = [];
    public selectAllPresiders = true;
    public showPresiderModal = false;

    public activities: any[] = [];
    public selectedActivities: any[] = [];
    public selectAllActivities = true;
    public showActivitiesModal = false;

    public sittingActivities = false;
    public nonSittingActivities = false;

    public isMySchedule = true;
    public currentCalendarDate = new Date("dd-mm-yyyy");


    mounted() {
        this.loadLocations();
    }

    loadLocations() {
        this.ar = [];
        this.$http
            .get("api/dashboard/locations")
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
                    //   this.ar = JSON.parse(JSON.stringify(data, null, 2));
                    this.locations = [...data];
                } else {
                    window.alert("bad data!");
                }

            });
    }
    showAllLocations() {
        this.showLocationModal = true;
    }
    resetLocations() {
        this.selectedLocations = [];
        this.arEvents = [...this.ar];
        (this.$refs.locationsModal as any).hide();

        this.getMonthlySchedule(this.currentCalendarDate);
    }
    loadPresiders(data) {
        if (this.selectedLocations.length == 0) {
            this.presiders = [];
            this.selectedPresiders = [];
            this.selectAllPresiders = false;
            return;
        }
        else {
            this.selectAllPresiders = true;
        }

        this.presiders = [...data];
        this.selectedPresiders = this.presiders.map((x) => {
            return x.value
        });
        //calendarStartDate, calendarEndDate


    }
    showAllPresiders() {
        this.showPresiderModal = true;
    }
    resetPresiders() {
        this.selectedPresiders = [];
    }
    loadActivities(data) {
        if (this.selectedLocations.length == 0) {
            this.activities = [];
            this.selectedActivities = [];
            this.selectAllActivities = false;
            return;
        }
        else {
            this.selectAllActivities = true;
        }

        this.activities = [...data];

        this.selectedActivities = this.activities.map((x) => {
            return x.value;
        });
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
        this.currentCalendarDate = currentMonth;
        let locations = "";
        if (this.selectedLocations.length > 0) {
            const locationIds = this.selectedLocations.join(',');
            locations = `?locationId=${locationIds}`;
        }

        this.$http
            .get(
                "api/dashboard/monthly-schedule/" +
                `${currentMonth.getFullYear()}/${String(currentMonth.getMonth() + 1).padStart(2, '0')}`
                + locations
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
                    this.ar = [...data.schedule];

                    //this.ar = data;
                    this.arEvents = [...this.ar];
                    this.loadActivities(data.activities);
                    this.loadPresiders(data.presiders);

                } else {
                    window.alert("bad data!");
                }

            });
    }

    public onCheckboxChange(selected) {
        this.selectedLocations = [...selected];
        if (this.selectedLocations.length) {
            this.isMySchedule = false;
        } else {
            this.isMySchedule = true;
        }
        this.getMonthlySchedule(this.currentCalendarDate);
    }

    public onSelectAllPresidersChange() {
        if (this.selectAllPresiders) {
            this.selectedPresiders = this.presiders.map(presider => presider.value);
        } else {
            this.selectedPresiders = [];
        }
        this.filterByPresiders();
    }

    public onCheckboxPresidersChange(selected) {
        this.selectedPresiders = [...selected];
        const options = this.presiders.map((x) => {
            return x.value;
        });
        if (options.length === selected.length) {
            this.selectAllPresiders = true;
        } else {
            this.selectAllPresiders = false;
        }
        this.filterByPresiders();
    }

    public filterByPresiders() {
        if (this.selectedPresiders.length) {
            this.arEvents = this.ar.filter((event) =>
                this.selectedPresiders.includes(String(event.assignment.judgeId)) ||
                this.selectedPresiders.includes(String(event.judgeId))
            );
        } else {
            this.arEvents = [];
        }

    }


    public onCheckboxActivitiesChange(selected) {
        this.selectedActivities = [...selected];
        const options = this.activities.map((x) => {
            return x.value;
        });
        if (options.length === selected.length) {
            this.selectAllActivities = true;
        } else {
            this.selectAllActivities = false;
        }
        this.filterByActivities();
    }

    public onSelectAllActivitiesChange() {
        if (this.selectAllActivities) {
            this.selectedActivities = this.activities.map(activity => activity.value);
        } else {
            this.selectedActivities = [];
        }
        this.filterByActivities();
    }

    public filterByActivities() {
        if (this.selectedActivities.length) {
            this.arEvents = this.ar.filter((event) =>
                this.selectedActivities.includes(event.assignment.activityCode) ||
                this.selectedActivities.includes(event.assignment.activityAm.activityCode) ||
                this.selectedActivities.includes(event.assignment.activityPm.activityCode)
            );
        } else {
            this.arEvents = [];
        }
    }

}
</script>