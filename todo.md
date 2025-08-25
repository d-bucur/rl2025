## week 7
- bug: np xp increase from spells??
- difficulty increase
- gear

## week 8 sharing phase
- tune camera distances
- native builds?

## public itch release
- proper intro menu, restart after death
- visualize player stats, like xp to next level, and UI overhaul
- view status on hover
- fx for items, maybe projectiles too?
- add LoS check for abilities
- bug: follow doesn't work very well. Enemies skipping last tile in path?
- bug: fireball hover effect not updating
- bug: fix camera rotation
- bug: resurrecting on top of enemies bugged
- biomes with different tiles

## extra
- blood under corpses
- in game health bars
- aggro nearby enemies when one is alerted (maybe generic sound system?)
- character shaders:
  - render wall tiles with partial alpha when overlapping characters
  - or add a radial fade effect to mesh shader around character positions
  - both require postprocessing framebuffers
- save/load
- billboard don't work on android chrome. They work on android firefox...

## refactoring
- refactor: separate systems and components, module always at top
- somewhat modular files like BehaviorTree and Tween should be moved to an Engine folder
- object pool and use for textures
- arena allocator with frame lifetime

## discarded?
- show vision colors in minimap?
- log history
- add keyboard targeting

## item ideas
- full map exploration
- chain lighting
- air scroll: push enemies and damage everything they hit
- area vision
- dig through map
