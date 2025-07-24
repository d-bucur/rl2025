- camera turn tiles transparent
- will need proper turn resolution at some point: https://journal.stuffwithstuff.com/2014/07/15/a-turn-based-game-loop/

## action dispatch attempts
- simple one calling Execute(EntityStore) on each actions
- attempt a more complicated one where Tags/Components are added to execute actions and dumb systems listen for these all the time (overengineered for ecs?)
- adding components just to trigger events is probably an antipattern. An alternative is to use events/signals directly

- attempted one where action dispatcher calls systems directly through an action->system dictionary, but friflo systems are more limited than bevy: can't pass around parameters or pipe data between them, so can't actually pass around the action to execute. If I have to add components so they can be picked up by systems later I can just do the above approach and avoid custom scheduling of systems
