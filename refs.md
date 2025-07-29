## Turn processing
- https://journal.stuffwithstuff.com/2014/07/15/a-turn-based-game-loop/

## Procedural generation:
- tunnel digging: https://rogueliketutorials.com/tutorials/tcod/v2/part-3/
- cave gen: https://code.tutsplus.com/generate-random-cave-levels-using-cellular-automata--gamedev-9664t
- https://tutsplus.github.io/jsfiddle-demos/generate_random_cave_levels_using_cellular_automata_example_1/index.html
- more: https://www.gridsagegames.com/blog/2014/06/mapgen-tunneling-algorithm/

## FOV
- https://www.roguebasin.com/index.php?title=Field_of_Vision
- https://www.roguebasin.com/index.php?title=Comparative_study_of_field_of_view_algorithms_for_2D_grid_based_worlds
- http://www.adammil.net/blog/v125_Roguelike_Vision_Algorithms.html

## Action dispatch attempts
- simple one calling Execute(EntityStore) on each actions
- attempt a more complicated one where Tags/Components are added to execute actions and dumb systems listen for these all the time (overengineered for ecs?)
- adding components just to trigger events is probably an antipattern. An alternative is to use events/signals directly
- attempted one where action dispatcher calls systems directly through an action->system dictionary, but friflo systems are more limited than bevy: can't pass around parameters or pipe data between them, so can't actually pass around the action to execute. If I have to add components so they can be picked up by systems later I can just do the above approach and avoid custom scheduling of systems

## More articles
- https://www.roguebasin.com/index.php?title=Articles