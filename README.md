# SwitchableProperties

### Control multiple SimHub properties using multiple binds.


<a href="http://www.youtube.com/watch?feature=player_embedded&v=8D7W_UCHs4c" target="_blank">
 <img src="http://img.youtube.com/vi/8D7W_UCHs4c/mqdefault.jpg" alt="Watch the video" width="576" height="324" />
</a>

Video kindly created by LeecarL


This is a small plugin that allows the user to create properties that can change values according to binds (keyboard, steering wheel button etc.)

For example, if you wanted to create a tabular menu system in a dash/overlay, where each menu tab could be accessed via buttons or touch screen presses, this could be achieved with the following:

- Create a property in the plugin
- Create binds for each menu tab specifying a memorable bind name, and a value
- Connect these binds to an input (keyboard press, button press etc.)
- Restart SimHub, or change game (re-initialise the plugin)
- Enter Dashstudio with your chosen dash
- On each widget you wish to connect to the property, enable the "Fx" of the Visible property
- In the "Fx" of the Visible property, a simple check for the property matching a desired bind name is written, for example, "SwitchableProperties.Page1 == 'tyres'". In this case, the property name is "Page 1", and the property value to match against is "tyres"
- Repeat for all widgets, connecting each to a related bind.

You should now have a functional menu system, where each page will be made visible when the property matches it's respective value.
