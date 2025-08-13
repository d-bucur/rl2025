## week 5
- add item selection menu
- event handlers for different game states? InputEventHandler component, add and remove as needed.
- ranged scrolls and targeting
  
## extra
- more stable enemy generation
- log history
- bug: pressing keys during path movement
- blood under corpses

## idea bin
- in game health bars
- camera rotation is buggy
- character shaders:
  - render wall tiles with partial alpha when overlapping characters
  - or add a radial fade effect to mesh shader around character positions
  - both require postprocessing framebuffers
- display turn order as a number on characters
- aggro nearby enemies when one is alerted
- show vision colors in minimap?
- biomes with different tiles

## code refactoring and cleanup
- move entity spawners to new file
- refactor actions to have standard naming. Maybe use standardized processors for each action? ie. map Action -> ActionProcessorSystem
- no way for an action processor to send a message back (like action invalid, retry your turn)