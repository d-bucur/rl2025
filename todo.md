## week 6
- levelling
- refactor: separate systems and components, module always at top
- add LoS check for abilities
- save/load

## week 5
- fx for items, maybe projectiles too?
- event handlers for different game states. InputEventHandler component, add and remove as needed.

## extra
- bug: follow doesn't work very well. Enemies skipping last tile in path?
- bug: fireball hover effect not updating
- view status on hover
- in game health bars
- blood under corpses
- aggro nearby enemies when one is alerted (maybe generic sound system?)
- billboard don't work on android chrome. They work on android firefox...
- object pool and use for textures
- bug: resurrecting on top of enemies bugged

## idea bin
- rogue selection (at least sprite)
- character shaders:
  - render wall tiles with partial alpha when overlapping characters
  - or add a radial fade effect to mesh shader around character positions
  - both require postprocessing framebuffers
- camera rotation is buggy
- biomes with different tiles
- arena allocator with frame lifetime
- add keyboard targeting
- somewhat modular files like BehaviorTree and Tween should be moved to an Engine folder

## item ideas
- full map exploration
- chain lighting
- air scroll: push enemies and damage everything they hit
- area vision
- dig through map

## discarded?
- show vision colors in minimap?
- log history
