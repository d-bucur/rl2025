## week 5
- add item selection menu
- event handlers for different game states? InputEventHandler component, add and remove as needed.
- ranged scrolls and targeting
  
## extra
- log history
- bug: pressing keys during path movement
- blood under corpses
- handle confusion applied to hero

## idea bin
- confusion has change to make enemies hit themselves
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
- no way for an action processor to send a message back (like action invalid, retry your turn).
- refactor actions to have standard naming. Maybe use standardized processors for each action? ie. map Action -> ActionProcessorSystem