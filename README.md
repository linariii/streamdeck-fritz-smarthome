
# Fritz Homeautomation for Elgato Stream Deck

[![CI](https://github.com/linariii/streamdeck-fritz-smarthome/actions/workflows/CI.yml/badge.svg)](https://github.com/linariii/streamdeck-fritz-smarthome/actions/workflows/CI.yml) [![CD](https://github.com/linariii/streamdeck-fritz-smarthome/actions/workflows/CD.yml/badge.svg)](https://github.com/linariii/streamdeck-fritz-smarthome/actions/workflows/CD.yml)

## Actions
* Power Usage
	* Displays the amount of watt taken from the outlet
	* requires supported FR!TZ Dect devices (e.g.: FRITZ!Dect 200, FRITZ!DECT 210)
	* data is refresh every five minutes 
	* button push > no action
* Outlet
	* Displays the state of the outlet (on/off)
	* requires supported FR!TZ Dect devices (e.g.: FRITZ!Dect 200, FRITZ!DECT 210)
	* state is refreshed every minute
	* button push > toggle state
* Temperature
	* Displays the current temperature
	 * requires supported FR!TZ Dect devices (e.g.: FRITZ!Dect 200, FRITZ!Dect 210, FRITZ!Dect 301, FRITZ!Dect 440, ...)
	 * data is refresh every five minutes
	 * button push > no action
 * Humidity
	 * Displays the current humidity
	 * requires supported FR!TZ Dect devices (e.g.: FRITZ!Dect 440)
	 * data is refresh every five minutes
	 * button push > no action
 * Battery
	 * Display the current batter charge of FR!TZ Dect devices that require a battery
	 * requires supported FR!TZ Dect devices (e.g.: FRITZ!Dect 301, FRITZ!Dect 440, ...)
	 * data is refresh every five minutes
	 * button push > no action

## Support
 - Supports Windows: Yes
 - Supports Mac: No

## Dependencies
* Uses [StreamDeck-Tools](https://github.com/BarRaider/streamdeck-tools) by BarRaider: [![NuGet](https://img.shields.io/nuget/v/streamdeck-tools.svg?style=flat)](https://www.nuget.org/packages/streamdeck-tools)
* Uses [Easy-PI](https://github.com/BarRaider/streamdeck-easypi) by BarRaider - Provides seamless integration with the Stream Deck PI (Property Inspector) 
* Uses [fritz-homeautomation-csharp](https://github.com/linariii/fritz-homeautomation-csharp) [![NuGet](https://img.shields.io/nuget/v/Fritz.HomeAutomation.svg?style=flat)](https://www.nuget.org/packages/Fritz.HomeAutomation/)
