import json
from typing import Tuple
import httpx

# LMAOOOOO
OPENWEATHERMAP_API_KEY = 'e762d6c90e1f1670093b64960d9c7463'
TOMORROW_API_KEY = 'ax2VNNEZxM0YM07kqGQS4QAdu5nReQ33'
WEATHER_API_KEY = '97e5c007431143b496101324220110'
WEATHERBIT_API_KEY = 'ce4866e8bd9e4db3b43b542ee573cc2d'

LONGEST_NAME_LEN = 58
def main():
    city_name = get_city()
    #city_name = 'London'

    print("---> weatherbitapi:")
    call_weatherbit_API(city_name)
    print("---> weatherapi:")
    call_weather_API(city_name)
    print("---> tomorrowapi")
    call_tomorrow_API(city_name)

def get_city():
    city_name = input("Enter city name: ")
    length = len(city_name)
    if (length > LONGEST_NAME_LEN) or (length < 1):
        print("Invalid name, try again.")
        return get_city()
    return city_name.capitalize()

# -> float, float
def get_geocoordinates_of(city_name) -> Tuple[int,int]:
    with httpx.Client() as client:
        LIMIT = 3
        params = {'q':city_name, 'limit':LIMIT, 'appid':OPENWEATHERMAP_API_KEY}
        geo_url = 'https://api.openweathermap.org/geo/1.0/direct'
        response = client.get(geo_url, params=params)
        status = response.status_code
        if status != 200:
            print("Status code:", status)
            return 0,0

        city_list = json.loads(response.text)
        cities = len(city_list)
        #print('Candidate cities:', cities)
        if cities < 1:
            return 0,0

        first_city = city_list[0]
        return first_city['lat'], first_city['lon']

def call_tomorrow_API(city_name):
    lat, lon = get_geocoordinates_of(city_name)
    if lat == 0 and lon == 0:
        return
    print("lat {} lon {}".format(lat,lon))
    with httpx.Client() as client:
        params = {
            'location':'{},{}'.format(lat, lon),
            'apikey':TOMORROW_API_KEY,
            'fields':'temperature,temperatureApparent,humidity,windSpeed,windGust,rainIntensity,cloudBase,cloudCover,visibility,weatherCode',
            'timesteps':'current',
            }
        url = 'https://api.tomorrow.io/v4/timelines'
        response = client.get(url, params=params)
        status = response.status_code
        if status != 200:
            print("Status code:", status)
            return
        data_map = json.loads(response.text)['data']['timelines'][0]
        values = data_map['intervals'][0]['values']
        print("Temperature {} C feels like {} C".format(values['temperature'], values['temperatureApparent']))
        print("Wind speeds: {} kph with gusts up to {} kph".format(values['windSpeed'], values['windGust']))
        base = values['cloudBase']
        if base is None:
            base = float('NaN')
        print("Cloud coverage: {}% with cloud base at {} m".format(values['cloudCover'], base*1000))

def call_weatherbit_API(city_name):
    with httpx.Client() as client:
        params = {'key':WEATHERBIT_API_KEY, 'city':city_name}
        weather_url = 'https://api.weatherbit.io/v2.0/current'
        response = client.get(weather_url, params=params)
        status = response.status_code
        if status != 200:
            if status == 401:
                print("Status 401")
            print("Status:", status)
            return

        json_load = json.loads(response.text)
        if 'data' in json_load:
            data_map = json_load['data'][0]
            print('{} {}   Last observation time: {}'.format(data_map['city_name'], data_map['country_code'], data_map['ob_time']))
            print('Temperature: %.1f C'%data_map['temp'])
            print('Sunrise: %s  Sunset: %s'%(data_map['sunrise'], data_map['sunset']))
            print('Description: {}      Cloud Coverage {}%'.format(data_map['weather']['description'],data_map['clouds']))
            print('Snow:', data_map['snow'])

def call_weather_API(city_name):
    with httpx.Client() as client:
        params = {'key':WEATHER_API_KEY, 'q':city_name, 'aqi':'no'}
        httpx_url = 'https://api.weatherapi.com/v1/current.json'
        response = client.get(httpx_url, params=params)
        status = response.status_code
        if status != 200:
            if status == 400:
                print("Not found but says bad request")
            print("Status code:", status)
            return
        json_dictionary = json.loads(response.text)
        location_dict = json_dictionary['location']
        print(location_dict['name'], location_dict['country'], location_dict['localtime'])
        current = json_dictionary['current']
        if 'text' in current:
            print(current['text'])
        temp   = 'Temperature: %.1f C feels like %.1f;  %.1f F feels like %.1f'
        temp   = temp%(current['temp_c'],current['feelslike_c'],current['temp_f'],current['feelslike_f'])
        print(temp)
        winds  = 'Winds:       %.1f kph;    %.1f mph'
        winds  = winds%(current['wind_kph'], current['wind_mph'])
        print(winds)
        precip = 'Rain:        %.1f mm;    %.1f inches'
        precip = precip%(current['precip_mm'], current['precip_in'])
        print(precip)

if __name__=="__main__":
    main()
