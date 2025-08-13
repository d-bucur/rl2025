## week 5
- add item selection menu
- event handlers for different game states? InputEventHandler component, add and remove as needed.
- ranged scrolls and targeting
- fx for items, maybe projectiles too?
- enemies shouldn't spawn in a certain range from start
  
## extra
- log history
- bug: pressing keys during path movement
- blood under corpses
- aggro nearby enemies when one is alerted
- handle confusion applied to hero
- add keyboard targeting
- billboard don't work on android chrome. They work on android firefox...

## idea bin
- confusion has change to make enemies hit themselves
- in game health bars
- camera rotation is buggy
- character shaders:
  - render wall tiles with partial alpha when overlapping characters
  - or add a radial fade effect to mesh shader around character positions
  - both require postprocessing framebuffers
- display turn order as a number on characters
- biomes with different tiles
- air scroll: push enemies and damage everything they hit
- chain lighting
- resurrect enemy as minion

## code refactoring and cleanup
- no way for an action processor to send a message back (like action invalid, retry your turn).
- refactor actions to have standard naming. Maybe use standardized processors for each action? ie. map Action -> ActionProcessorSystem

## discarded?
- show vision colors in minimap?
