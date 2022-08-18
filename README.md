# SwitchableProperties

### Control multiple SimHub properties using multiple binds.


<a href="http://www.youtube.com/watch?feature=player_embedded&v=8D7W_UCHs4c" target="_blank">
 <img src="http://img.youtube.com/vi/8D7W_UCHs4c/mqdefault.jpg" alt="Watch the video" width="576" height="324" />
</a>

Video kindly created by LeecarL


This is a small plugin that allows the user to create properties that can change values according to binds (keyboard, steering wheel button etc.)

## Example Practical Usages

### First use case - Rotary switch pages
Using the first bind type, you can assign a button/key to a specific property value. This can be used to assign pages in a dash to buttons or rotary switch positions. Currently SimHub supports 4 actions for widget page swapping, but you can create as many binds as you like with the plugin.

![20200501_171817_Original](https://user-images.githubusercontent.com/18599017/183436266-d405da41-c9ee-400d-8015-61da09ab9fab.png)

### Second use case - Dash + & - page cycling
Using the second bind type (Cycler binds), you can assign a button/key to cycle through the set of values that are created from the first bind type. This can be done in either direction to accomodate + and - buttons, and you can set up multiple (in case of multiple wheel rims, button boxes, voice commands etc.)

![dash+-](https://user-images.githubusercontent.com/18599017/183439063-d1d3986d-2844-4e4d-b3cf-aca2d0c27038.png)

### Third use case - Override pages (Brake Magic, Quali, Race Start etc.)
Using the third bind type (Toggle binds), you can assign a button/key to toggle the property to a specific value. When this bind is activated again, the property will return to it's last value. This can be used to assign buttons to temporarily display a specific page, while returning to the previous page when re-activated. This is ideal for creating dash pages such as the Mercedes F1 "Brake Magic" (displays brake temperatures more prominently), qualifying, race launch etc.

![racestart](https://user-images.githubusercontent.com/18599017/183440164-5efff47a-c88d-4af7-a725-233aa87945ad.png)

## Basic setup and usage

For example, if you wanted to create a tabular menu system in a dash/overlay, where each menu tab could be accessed via buttons or touch screen presses, this could be achieved with the following:

- Create a property in the plugin
- Create binds for each menu tab specifying a memorable bind name, and a value
- Connect these binds to an input (keyboard press, button press etc.)
- Enter Dashstudio with your chosen dash
- On each widget you wish to connect to the property, enable the "Fx" of the Visible property
- In the "Fx" of the Visible property, a simple check for the property matching a desired bind name is written, for example, "SwitchableProperties.Page1 == 'tyres'". In this case, the property name is "Page 1", and the property value to match against is "tyres"
- Repeat for all widgets, connecting each to a related bind.

You should now have a functional menu system, where each page will be made visible when the property matches it's respective value.
