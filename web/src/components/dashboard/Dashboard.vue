<template>
  <div class="judges-dashboard">
    <header class="header">
      <div class="top-line">Court Today</div>
      <div class="bot-line">
        <div class="left">Criminal</div>
        <div class="center">Scheduled:</div>
        <div class="right">Today's court list</div>
      </div>
    </header>
    <section class="dashboard-container">
      <div class="tools-container">
        <button class="filters" @click="toggleLeft()">
          <img src="../../assets/filters.svg" alt="filters" />
          <span> Filters </span>
        </button>
        <button class="more" @click="toggleRight()">
          <img src="../../assets/more3dots.svg" alt="more" />
          <span>More</span>
        </button>
      </div>
      <div class="calendar-container">
        <div
          :class="{
            'left-menu-active': showLeftMenu,
            'left-menu': !showLeftMenu,
          }"
        >
          <div class="accordion-content">
            <button
              class="accordion-button"
              v-b-toggle.locations
              variant="primary"
            >
              Locations
              <img src="../../assets/arrow-down.svg" alt="more" />
            </button>

            <!-- Collapsible content -->
            <b-collapse id="locations" class="mt-2">
              <b-card>
                <b-form-checkbox-group
                  id="locations-filter-box"
                  v-model="selectedLocations"
                  :options="
                    locations.length > 7 ? locations.slice(0, 7) : locations
                  "
                  name="locations-filter"
                  @change="onCheckboxChange"
                ></b-form-checkbox-group>
              </b-card>
              <button
                v-if="locations.length > 0"
                class="moreItems"
                @click="showAllLocations()"
              >
                <span>See all locations</span>
              </button>
            </b-collapse>
          </div>

          <div class="accordion-content" v-if="selectedLocations.length > 0">
            <button
              class="accordion-button"
              v-b-toggle.presiders
              variant="primary"
            >
              Presiders
              <img src="../../assets/arrow-down.svg" alt="more" />
            </button>

            <!-- Collapsible content -->
            <b-collapse id="presiders" class="mt-2">
              <b-card>
                <b-form-checkbox
                  id="select-all-presiders"
                  v-model="selectAllPresiders"
                  @change="onSelectAllPresidersChange"
                >
                  All Persiders
                </b-form-checkbox>
                <b-form-checkbox-group
                  id="presiders-filter-box"
                  v-model="selectedPresiders"
                  :options="
                    presiders.length > 7 ? presiders.slice(0, 7) : presiders
                  "
                  name="presiders-filter"
                  @change="onCheckboxPresidersChange"
                ></b-form-checkbox-group>
              </b-card>
              <button
                v-if="presiders.length > 0"
                class="moreItems"
                @click="showAllPresiders()"
              >
                <span>See all presiders</span>
              </button>
            </b-collapse>
          </div>
          <div class="accordion-content" v-if="selectedLocations.length > 0">
            <button
              class="accordion-button"
              v-b-toggle.activities
              variant="primary"
            >
              Activities
              <img src="../../assets/arrow-down.svg" alt="more" />
            </button>

            <!-- Collapsible content -->
            <b-collapse id="activities" class="mt-2">
              <b-card>
                <b-form-checkbox
                  id="select-all-activities"
                  v-model="selectAllActivities"
                  @change="onSelectAllActivitiesChange"
                >
                  All Activities
                </b-form-checkbox>
                <b-form-checkbox-group
                  id="activities-filter-box"
                  v-model="selectedActivities"
                  :options="
                    activities.length > 7 ? activities.slice(0, 7) : activities
                  "
                  name="activities-filter"
                  @change="onCheckboxActivitiesChange"
                ></b-form-checkbox-group>
              </b-card>
              <button class="moreItems" @click="showAllActivities()">
                <span v-if="activities.length > 1">See all activities</span>
              </button>
            </b-collapse>
          </div>
          <div class="activitiesPanel" v-if="selectedLocations.length > 0">
            <b-form-checkbox
              class="singleCheckbox"
              id="sittingActivities"
              v-model="sittingActivities"
            >
              Show sitting activities
            </b-form-checkbox>
          </div>
          <div v-if="selectedLocations.length > 0">
            <b-form-checkbox
              class="singleCheckbox"
              id="nonSittingActivities"
              v-model="nonSittingActivities"
            >
              Show non-sitting activities
            </b-form-checkbox>
          </div>

          <div>
            <button class="defaultButton">
              <span>Back to my calendar</span>
            </button>

            <button class="inverseButton">
              <span>Favourite current filters</span>
            </button>
          </div>
        </div>
        <div
          class="dashboard-calendar"
          :class="{ 'calendar-active': menuActive, '': !menuActive }"
        >
          <Calendar
            :events="arEvents"
            @monthChange="getMonthlySchedule"
            :sizeChange="sizeChange"
            :locations="locations"
            :isMySchedule="isMySchedule"
          />
        </div>
        <div
          :class="{
            'right-menu-active': showRightMenu,
            'right-menu': !showRightMenu,
          }"
        >
          <div>&nbsp;</div>
          <div>&nbsp;</div>
        </div>
      </div>
    </section>
    <section class="dashboard-collapse-section">
      <div class="dashboard-collapse">Reserved Judgement (5)</div>
      <div class="dashboard-collapse">Reserved Judgement (5)</div>
    </section>
    <b-modal
      id="locations-click-modal"
      hide-footer
      centered
      size="lg"
      ref="locationsModal"
      v-model="showLocationModal"
    >
      <div>
        <div class="modal-header-title">Locations</div>
        <div class="locations-columns">
          <div class="column">
            <b-form-checkbox-group
              id="locations-filter-box"
              v-model="selectedLocations"
              :options="locations"
              name="locations-filter"
              @change="onCheckboxChange"
            ></b-form-checkbox-group>
          </div>
        </div>
        <div class="modal-buttons">
          <button class="moreItems" @click="resetLocations()">Reset</button>
          <button class="modal-button-right">Save</button>
        </div>
      </div>
    </b-modal>
    <b-modal
      id="presiders-click-modal"
      hide-footer
      centered
      size="lg"
      hide-backdrop
      ref="presidersModal"
      v-model="showPresiderModal"
    >
      <div>
        <div class="modal-header-title">Presiders</div>
        <div class="presiders-columns">
          <div class="column">
            <b-form-checkbox-group
              id="presiders-filter-box"
              v-model="selectedPresiders"
              :options="presiders"
              name="presiders-filter"
              @change="onCheckboxPresidersChange"
            ></b-form-checkbox-group>
          </div>
        </div>
        <div class="modal-buttons">
          <button class="moreItems" @click="resetPresiders()">Reset</button>
          <button class="modal-button-right">Save</button>
        </div>
      </div>
    </b-modal>

    <b-modal
      id="activities-click-modal"
      hide-footer
      centered
      size="lg"
      hide-backdrop
      ref="activitiesModal"
      v-model="showActivitiesModal"
    >
      <div>
        <div class="modal-header-title">Activities</div>
        <div class="activities-columns">
          <div class="column">
            <b-form-checkbox-group
              id="activities-filter-box"
              v-model="selectedActivities"
              :options="activities"
              name="activities-filter"
              @change="onCheckboxActivitiesChange"
            ></b-form-checkbox-group>
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
    font-family: 'Work Sans', sans-serif;
    font-size: 24px;
    color: #183a4a;
    margin-bottom: 30px;
    margin-top: -10px;
  }

  .moreItems {
    font-family: 'Work Sans', sans-serif;
    font-size: 16px;
    color: #183a4a;
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
    border: 1px solid #183a4a;
    width: 120px;
    background-color: #183a4a;
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
    border: 1px solid #183a4a;
    background-color: #fff;
    color: #183a4a;
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
    font-family: 'Work Sans', sans-serif;
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
    font-family: 'Work Sans', sans-serif;
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
    color: #183a4a;
    font-family: 'Work Sans', sans-serif;
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
    font-family: 'Work Sans', sans-serif;
    font-size: 16px;
    color: #fff;
    background-color: #183a4a;
    border-radius: 20px;
    border: 1px solid transparent;
  }

  .inverseButton,
  .defaultButton:hover {
    background-color: #fff;
    font-family: 'Work Sans', sans-serif;
    font-size: 16px;
    color: #183a4a;
    border: 1px solid #183a4a;
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
  import { HttpService } from '@/services/HttpService';
import Calendar from '@components/calendar/Calendar.vue';
import { defineComponent, inject, onMounted, reactive, ref } from 'vue';

  export default defineComponent({
    components: {
      Calendar,
    },
    setup() {
      const httpService = inject<HttpService>('httpService');

      if (!httpService) {
        throw new Error('HttpService is not available!');
      }

      let sizeChange = 0;
      const locationsFilters = [];
      let ar: any[] = [];
      let arEvents = reactive<any[]>([]);
      let showLeftMenu = ref(false);
      let showRightMenu = ref(false);
      let menuActive = ref(false);

      let locations = reactive<any[]>([]);
      let selectedLocations = reactive<any[]>([]);
      let showLocationModal = ref(false);

      let presiders = reactive<any[]>([]);
      let selectedPresiders = reactive<any[]>([]);
      let selectAllPresiders = ref(true);
      let showPresiderModal = false;

      let activities = reactive<any[]>([]);
      let selectedActivities = reactive<any[]>([]);
      let selectAllActivities = ref(true);
      let showActivitiesModal = ref(false);

      const sittingActivities = ref(false);
      const nonSittingActivities = ref(false);

      let isMySchedule = ref(true);
      let currentCalendarDate = new Date('dd-mm-yyyy');

      onMounted(() => {
        loadLocations();
      });

      const loadLocations = () => {
        ar = [];
        httpService
          .get<{ text: string; value: string }[]>('api/dashboard/locations')
          .then(
            (Response) => Response,
            (err) => {
              // $bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
              //     title: "An error has occured.",
              //     variant: "danger",
              //     autoHideDelay: 10000,
              // });
              console.log(err);
            }
          )
          .then((data) => {
            if (data) {
              //   ar = JSON.parse(JSON.stringify(data, null, 2));
              locations = [...data];
            }
          });
      };

      const showAllLocations = () => {
        showLocationModal.value = true;
      };

      const resetLocations = () => {
        selectedLocations = [];
        arEvents = [...ar];

        //($refs.locationsModal as any).hide();

        getMonthlySchedule(currentCalendarDate);
      };

      const loadPresiders = (data) => {
        if (selectedLocations.length == 0) {
          presiders = [];
          selectedPresiders = [];
          selectAllPresiders.value = false;
          return;
        } else {
          selectAllPresiders.value = true;
        }

        presiders = [...data];
        selectedPresiders = presiders.map((x) => {
          return x.value;
        });
        //calendarStartDate, calendarEndDate
      };

      const showAllPresiders = () => {
        showPresiderModal = true;
      };

      const resetPresiders = () => {
        selectedPresiders = [];
      };

      const loadActivities = (data) => {
        if (selectedLocations.length == 0) {
          activities = [];
          selectedActivities = [];
          selectAllActivities.value = false;
          return;
        } else {
          selectAllActivities.value = true;
        }

        activities = [...data];

        selectedActivities = activities.map((x) => {
          return x.value;
        });
      };

      const showAllActivities = () => {
        showActivitiesModal.value = true;
      };

      const resetActivities = () => {
        selectedActivities = [];
      };

      const toggleRight = () => {
        arEvents = [...arEvents];
        showRightMenu.value = !showRightMenu;
        showLeftMenu.value = false;
        menuActive = showRightMenu;
        sizeChange = sizeChange++;
      };

      const toggleLeft = () => {
        arEvents = [...arEvents];
        showLeftMenu.value = !showLeftMenu;
        showRightMenu.value = false;
        menuActive = showLeftMenu;
        sizeChange = sizeChange++;
      };

      const getMonthlySchedule = (currentMonth) => {
        ar = [];
        currentCalendarDate = currentMonth;
        let locations = '';
        if (selectedLocations.length > 0) {
          const locationIds = selectedLocations.join(',');
          locations = `?locationId=${locationIds}`;
        }

        httpService
          .get(
            'api/dashboard/monthly-schedule/' +
              `${currentMonth.getFullYear()}/${String(currentMonth.getMonth() + 1).padStart(2, '0')}` +
              locations
          )
          .then(
            (Response) => Response,
            (err) => {
              //   $bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
              //       title: "An error has occured.",
              //       variant: "danger",
              //       autoHideDelay: 10000,
              //   });
              console.log(err);
            }
          )
          .then((data) => {
            if (data) {
              //   ar = [...data.schedule];
              //   arEvents = [...ar];
              //   loadActivities(data.activities);
              //   loadPresiders(data.presiders);
            }
          });
      };

      const onCheckboxChange = (selected) => {
        selectedLocations = [...selected];
        if (selectedLocations.length) {
          isMySchedule.value = false;
        } else {
          isMySchedule.value = true;
        }
        getMonthlySchedule(currentCalendarDate);
      };

      const onSelectAllPresidersChange = () => {
        if (selectAllPresiders) {
          selectedPresiders = presiders.map((presider) => presider.value);
        } else {
          selectedPresiders = [];
        }
        filterByPresiders();
      };

      const onCheckboxPresidersChange = (selected) => {
        selectedPresiders = [...selected];
        const options = presiders.map((x) => {
          return x.value;
        });
        if (options.length === selected.length) {
          selectAllPresiders.value = true;
        } else {
          selectAllPresiders.value = false;
        }
        filterByPresiders();
      };

      const filterByPresiders = () => {
        if (selectedPresiders.length) {
          arEvents = ar.filter(
            (event) =>
              selectedPresiders.includes(String(event.assignment.judgeId)) ||
              selectedPresiders.includes(String(event.judgeId))
          );
        } else {
          arEvents = [];
        }
      };

      const onCheckboxActivitiesChange = (selected) => {
        selectedActivities = [...selected];
        const options = activities.map((x) => {
          return x.value;
        });
        if (options.length === selected.length) {
          selectAllActivities.value = true;
        } else {
          selectAllActivities.value = false;
        }
        filterByActivities();
      };

      const onSelectAllActivitiesChange = () => {
        if (selectAllActivities) {
          selectedActivities = activities.map((activity) => activity.value);
        } else {
          selectedActivities = [];
        }
        filterByActivities();
      };

      const filterByActivities = () => {
        if (selectedActivities.length) {
          arEvents = ar.filter(
            (event) =>
              selectedActivities.includes(event.assignment.activityCode) ||
              selectedActivities.includes(
                event.assignment.activityAm.activityCode
              ) ||
              selectedActivities.includes(
                event.assignment.activityPm.activityCode
              )
          );
        } else {
          arEvents = [];
        }
      };

      return {
        toggleLeft,
        toggleRight,
        showLeftMenu,
        showRightMenu,
        selectedLocations,
        locations,
        onCheckboxChange,
        showAllLocations,
        selectAllPresiders,
        onSelectAllPresidersChange,
        selectedPresiders,
        presiders,
        onCheckboxPresidersChange,
        showAllPresiders,
        selectAllActivities,
        onSelectAllActivitiesChange,
        selectedActivities,
        activities,
        onCheckboxActivitiesChange,
        showAllActivities,
        sittingActivities,
        nonSittingActivities,
        menuActive,
        arEvents,
        getMonthlySchedule,
        sizeChange,
        isMySchedule,
        showLocationModal,
        resetLocations,
        showPresiderModal,
        resetPresiders,
        showActivitiesModal,
        resetActivities,
      };
    },
  });
</script>
