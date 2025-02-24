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
                  :options="getLocations"
                  name="locations-filter"
                  :value-field="'locationId'"
                  :text-field="'name'"
                  @change="onLocationChecked"
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
                  v-model="areAllPresidersChecked"
                  @change="onSelectAllPresiders"
                >
                  All Persiders
                </b-form-checkbox>
                <b-form-checkbox-group
                  id="presiders-filter-box"
                  v-model="selectedPresiders"
                  :options="getPresiders"
                  name="presiders-filter"
                  :value-field="'value'"
                  :text-field="'text'"
                  @change="onPreciderChecked"
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
                  v-model="areAllActivitiesChecked"
                  @change="onSelectAllActivities"
                >
                  All Activities
                </b-form-checkbox>
                <b-form-checkbox-group
                  id="activities-filter-box"
                  v-model="selectedActivities"
                  :options="getActivities"
                  name="activities-filter"
                  :value-field="'value'"
                  :text-field="'text'"
                  @change="onActivityChecked"
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
              :value-field="'locationId'"
              :text-field="'name'"
            ></b-form-checkbox-group>
          </div>
        </div>
        <div class="modal-buttons">
          <button class="moreItems" @click="onResetLocations">Reset</button>
          <button class="modal-button-right" @click="onFilterLocations">
            Save
          </button>
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
      v-model="isPresiderModalVisible"
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
              @change="onPreciderChecked"
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
      v-model="isActivitiesModalVisible"
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
              @change="onActivityChecked"
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
  import { DashboardService, LocationService } from '@/services';
import { Activity, CalendarDay, Location, Presider } from '@/types';
import { computed, defineComponent, inject, onMounted, Ref, ref } from 'vue';
import Calendar from '../calendar/Calendar.vue';

  export default defineComponent({
    components: {
      Calendar,
    },
    setup() {
      const locationsService = inject<LocationService>('locationService');
      const dashboardService = inject<DashboardService>('dashboardService');

      if (!locationsService || !dashboardService) {
        throw new Error('Service is not available!');
      }

      const isMySchedule = ref(true);

      const locations = ref<Location[]>([]);
      const selectedLocations = ref<string[]>([]);
      const isLocationModalVisible = ref(false);

      const presiders = ref<Presider[]>([]);
      const selectedPresiders = ref<string[]>([]);
      const isPresiderModalVisible = ref(false);
      const areAllPresidersChecked = ref(true);

      const activities = ref<Activity[]>([]);
      const selectedActivities = ref<string[]>([]);
      const areAllActivitiesChecked = ref(true);
      const isActivitiesModalVisible = ref(false);

      const filteredEvents = ref<CalendarDay[]>([]);

      let allEvents: CalendarDay[] = [];

      let sizeChange = 0;

      const showLeftMenu = ref(false);
      const showRightMenu = ref(false);
      const menuActive = ref(false);

      const sittingActivities = ref(false);
      const nonSittingActivities = ref(false);

      let currentCalendarDate = new Date('dd-mm-yyyy');

      onMounted(async () => {
        await loadLocations();
      });

      const loadLocations = async () => {
        const data = await locationsService.getLocations();
        // Location with locationId comes from PCSS. Exclude data from JC for now
        locations.value = [...data.filter(l => "locationId" in l)];
      };

      const showAllLocations = () => {
        isLocationModalVisible.value = true;
      };

      const onFilterLocations = async () => {
        if (selectedLocations.value.length === 0) {
          return;
        }

        isLocationModalVisible.value = false;
        await getMonthlySchedule(currentCalendarDate);
      };

      const onResetLocations = async () => {
        selectedLocations.value.length = 0;
        isLocationModalVisible.value = false;
        await getMonthlySchedule(currentCalendarDate);
      };

      const loadPresiders = (data: Presider[]) => {
        if (selectedLocations.value.length === 0) {
          presiders.value.length = 0;
          selectedPresiders.value.length = 0;
          areAllPresidersChecked.value = false;
          return;
        }

        areAllPresidersChecked.value = true;
        selectedPresiders.value = data.map((p) => p.value);
        presiders.value = [...data];
      };

      const showAllPresiders = () => {
        isPresiderModalVisible.value = true;
      };

      const resetPresiders = () => {
        areAllPresidersChecked.value = true;
        selectedPresiders.value = presiders.value.map((p) => p.value);
        isPresiderModalVisible.value = false;
        filterByPresiders();
      };

      const loadActivities = (data: Activity[]) => {
        if (selectedLocations.value.length == 0) {
          activities.value.length = 0;
          selectedActivities.value.length = 0;
          areAllActivitiesChecked.value = false;
          return;
        }

        areAllActivitiesChecked.value = true;
        selectedActivities.value = data.map((a) => a.value);
        activities.value = [...data];
      };

      const showAllActivities = () => {
        isActivitiesModalVisible.value = true;
      };

      const resetActivities = () => {
        areAllActivitiesChecked.value = true;
        selectedActivities.value = activities.value.map((a) => a.value);
        isActivitiesModalVisible.value = false;
        filterByActivities();
      };

      const toggleRight = () => {
        showRightMenu.value = !showRightMenu.value;
        showLeftMenu.value = false;
        menuActive.value = showRightMenu.value;
        sizeChange++;
      };

      const toggleLeft = () => {
        showLeftMenu.value = !showLeftMenu.value;
        showRightMenu.value = false;
        menuActive.value = showLeftMenu.value;
        sizeChange++;
      };

      const getMonthlySchedule = async (currentMonth) => {
        currentCalendarDate = currentMonth;

        const locationIds =
          selectedLocations.value.length > 0
            ? selectedLocations.value.join(',')
            : '';
        const year = currentMonth.getFullYear();
        const month = String(currentMonth.getMonth() + 1).padStart(2, '0');

        const { schedule, presiders, activities } =
          await dashboardService.getMonthlySchedule(year, +month, locationIds);

        allEvents = [...schedule];
        filteredEvents.value = [...schedule];

        loadPresiders(presiders);
        loadActivities(activities);
      };

      const onLocationChecked = () => {
        isMySchedule.value = selectedLocations.value.length === 0;
        getMonthlySchedule(currentCalendarDate);
      };

      const onSelectAllPresiders = () => {
        if (areAllPresidersChecked.value) {
          selectedPresiders.value = presiders.value.map((p) => p.value);
        } else {
          selectedPresiders.value.splice(0);
        }
        filterByPresiders();
      };

      const onPreciderChecked = () => {
        areAllPresidersChecked.value =
          selectedPresiders.value.length === presiders.value.length;
        filterByPresiders();
      };

      const filterByPresiders = () => {
        if (selectedPresiders.value.length) {
          filteredEvents.value = allEvents.filter(
            (event) =>
              selectedPresiders.value.includes(
                String(event.assignment.judgeId)
              ) || selectedPresiders.value.includes(String(event.judgeId))
          );
        } else {
          filteredEvents.value = [];
        }
      };

      const onActivityChecked = () => {
        areAllActivitiesChecked.value =
          selectedActivities.value.length === activities.value.length;
        filterByActivities();
      };

      const onSelectAllActivities = () => {
        if (areAllActivitiesChecked.value) {
          selectedActivities.value = activities.value.map((p) => p.value);
        } else {
          selectedActivities.value.splice(0);
        }
        filterByActivities();
      };

      const filterByActivities = () => {
        if (selectedActivities.value.length > 0) {
          filteredEvents.value = allEvents.filter(
            (event) =>
              selectedActivities.value.includes(
                event.assignment.activityCode
              ) ||
              selectedActivities.value.includes(
                event.assignment.activityAm?.activityCode
              ) ||
              selectedActivities.value.includes(
                event.assignment.activityPm?.activityCode
              )
          );
        } else {
          filteredEvents.value = [];
        }
      };

      const getFirstNItemsFromList = <T,>(
        items: Ref<T[]>,
        limit = 7
      ): Ref<T[]> => {
        return computed(() => {
          return items.value.length > limit
            ? items.value.slice(0, limit)
            : items.value;
        });
      };

      const getLocations = getFirstNItemsFromList(locations);
      const getPresiders = getFirstNItemsFromList(presiders);
      const getActivities = getFirstNItemsFromList(activities);

      return {
        toggleLeft,
        toggleRight,
        showLeftMenu,
        showRightMenu,
        selectedLocations,
        locations,
        onLocationChecked,
        showAllLocations,
        areAllPresidersChecked,
        onSelectAllPresiders,
        selectedPresiders,
        presiders,
        onPreciderChecked,
        showAllPresiders,
        areAllActivitiesChecked,
        onSelectAllActivities,
        selectedActivities,
        activities,
        onActivityChecked,
        showAllActivities,
        sittingActivities,
        nonSittingActivities,
        menuActive,
        arEvents: filteredEvents,
        getMonthlySchedule,
        sizeChange,
        isMySchedule,
        showLocationModal: isLocationModalVisible,
        onResetLocations,
        isPresiderModalVisible,
        resetPresiders,
        isActivitiesModalVisible,
        resetActivities,
        getLocations,
        getPresiders,
        getActivities,
        onFilterLocations,
      };
    },
  });
</script>
