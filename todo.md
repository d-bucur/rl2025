- event handlers for different game states?
- log history
- revisit enemy and room generation
- in game health bars

## Pathfinding
- improve cache usage
- stop auto move after one turn if enemies in sight
- incremental cache produces slightly different paths that full rebuild (not sure if this is solvable)

## Other
- display turn order as a number on characters
- show vision colors in minimap?
- character shaders:
  - render wall tiles with partial alpha when overlapping characters
  - or add a radial fade effect to mesh shader around character positions
  - both require postprocessing framebuffers