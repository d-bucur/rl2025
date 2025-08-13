## week 5
- fx for items, maybe projectiles too?
- event handlers for different game states? InputEventHandler component, add and remove as needed.
  
## extra
- log history
- bug: pressing keys during path movement
- blood under corpses
- aggro nearby enemies when one is alerted
- handle confusion applied to hero
- add keyboard targeting
- billboard don't work on android chrome. They work on android firefox...

## idea bin
- in game health bars
- camera rotation is buggy
- character shaders:
  - render wall tiles with partial alpha when overlapping characters
  - or add a radial fade effect to mesh shader around character positions
  - both require postprocessing framebuffers
- display turn order as a number on characters
- biomes with different tiles
- arena allocator with frame lifetime
- rogue selection (at least sprite)

## code refactoring and cleanup
- no way for an action processor to send a message back (like action invalid, retry your turn).
- refactor actions to have standard naming. Maybe use standardized processors for each action? ie. map Action -> ActionProcessorSystem

## item ideas
- air scroll: push enemies and damage everything they hit
- chain lighting
- self buff: double energy gain, lose some HP
- resurrect enemy as minion
- full map exploration
- area vision
- dig through map

## discarded?
- show vision colors in minimap?
