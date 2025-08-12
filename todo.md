- more stable enemy generation
- in game health bars
- log history
- event handlers for different game states? InputEventHandler component, add and remove as needed.
- move entity spawners to new file

## Other
- bug: pressing keys during path movement
- blood under corpses
- camera rotation is buggy
- display turn order as a number on characters
- aggro nearby enemies when one is alerted
- show vision colors in minimap?
- biomes with different tiles
- character shaders:
  - render wall tiles with partial alpha when overlapping characters
  - or add a radial fade effect to mesh shader around character positions
  - both require postprocessing framebuffers

- refactor actions to have standard naming. Maybe use standardized processors for each action? ie. map Action -> ActionProcessorSystem
- no way for an action processor to send a message back (like action invalid, retry your turn)