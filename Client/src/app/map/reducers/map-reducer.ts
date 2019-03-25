import { MapState, MapStateRecord } from './map-state';
import { Action } from '@ngrx/store';
import { PayloadAction } from '../../shared/models/payload-action';
import { LocationInfo } from '../../shared/models/location-info';
import { MapActions } from '../actions/map.actions';
import { List } from 'immutable';

const initialState: MapState = (new MapStateRecord() as unknown) as MapState;

export function mapReducer(state: MapState = initialState, action: Action) {
  return {
    locations: locations(state.locations, action),

    locationLoaded: locationLoaded(state.locationLoaded, action),
  };
}

export function locations(state = initialState.locations, action: PayloadAction<any>): List<LocationInfo> {
  switch (action.type) {
    case MapActions.STORE_LOCATION_DATA:
      const location = action.payload as LocationInfo;
      if (location) {
        if (!state) {
          return List([location]);
        }

        const currentLocations = state.toArray();
        const foundIndex = currentLocations.findIndex((e: LocationInfo) => e.locationId === location.locationId);
        if (foundIndex !== -1) {
          currentLocations[foundIndex] = location;
        } else {
          currentLocations.push(location);
        }
        return List(currentLocations);
      }
      return state;
    default:
      return state;
  }
}

export function locationLoaded(state = initialState.locationLoaded, action: PayloadAction<any>): boolean {
  switch (action.type) {
    case MapActions.GET_LOCATION_DATA:
    case MapActions.REFRESH_LOCATION_DATA:
      return false;
    case MapActions.STORE_LOCATION_DATA:
      return true;
    default:
      return state;
  }
}