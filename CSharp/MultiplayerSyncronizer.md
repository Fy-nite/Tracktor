Class:   MultiplayerSynchronizer
Inherits:   Node <   Object

Synchronizes properties from the multiplayer authority to the remote peers.

Description

By default, MultiplayerSynchronizer synchronizes configured properties to all peers.

Visibility can be handled directly with set_visibility_for() or as-needed with add_visibility_filter() and update_visibility().

MultiplayerSpawners will handle nodes according to visibility of synchronizers as long as the node at root_path was spawned by one.

Internally, MultiplayerSynchronizer uses MultiplayerAPI.object_configuration_add() to notify synchronization start passing the Node at root_path as the object and itself as the configuration, and uses MultiplayerAPI.object_configuration_remove() to notify synchronization end in a similar way.

Note: Synchronization is not supported for Object type properties, like Resource. Properties that are unique to each peer, like the instance IDs of Objects (see Object.get_instance_id()) or RIDs, will also not work in synchronization.

Properties

floatdelta_interval[default: 0.0]boolpublic_visibility[default: true]SceneReplicationConfigreplication_configfloatreplication_interval[default: 0.0]NodePathroot_path[default: NodePath("..")]VisibilityUpdateModevisibility_update_mode[default: 0]

Methods

voidadd_visibility_filter(filter: Callable)boolget_visibility_for(peer: int) constvoidremove_visibility_filter(filter: Callable)voidset_visibility_for(peer: int, visible: bool)voidupdate_visibility(for_peer: int = 0)

Signals

● delta_synchronized()
Emitted when a new delta synchronization state is received by this synchronizer after the properties have been updated.

● synchronized()
Emitted when a new synchronization state is received by this synchronizer after the properties have been updated.

● visibility_changed(for_peer: int)
Emitted when visibility of for_peer is updated. See update_visibility().

Enumerations

enum VisibilityUpdateMode:

● VISIBILITY_PROCESS_IDLE = 0
Visibility filters are updated during process frames (see Node.NOTIFICATION_INTERNAL_PROCESS).
● VISIBILITY_PROCESS_PHYSICS = 1
Visibility filters are updated during physics frames (see Node.NOTIFICATION_INTERNAL_PHYSICS_PROCESS).
● VISIBILITY_PROCESS_NONE = 2
Visibility filters are not updated automatically, and must be updated manually by calling update_visibility().


Property Descriptions


● floatdelta_interval [default: 0.0]set_delta_interval(value) setterget_delta_interval() getter

Time interval between delta synchronizations. Used when the replication is set to SceneReplicationConfig.REPLICATION_MODE_ON_CHANGE. If set to 0.0 (the default), delta synchronizations happen every network process frame.


● boolpublic_visibility [default: true]set_visibility_public(value) setteris_visibility_public() getter

Whether synchronization should be visible to all peers by default. See set_visibility_for() and add_visibility_filter() for ways of configuring fine-grained visibility options.


● SceneReplicationConfigreplication_configset_replication_config(value) setterget_replication_config() getter

Resource containing which properties to synchronize.


● floatreplication_interval [default: 0.0]set_replication_interval(value) setterget_replication_interval() getter

Time interval between synchronizations. Used when the replication is set to SceneReplicationConfig.REPLICATION_MODE_ALWAYS. If set to 0.0 (the default), synchronizations happen every network process frame.


● NodePathroot_path [default: NodePath("..")]set_root_path(value) setterget_root_path() getter

Node path that replicated properties are relative to.

If root_path was spawned by a MultiplayerSpawner, the node will be also be spawned and despawned based on this synchronizer visibility options.


● VisibilityUpdateModevisibility_update_mode [default: 0]set_visibility_update_mode(value) setterget_visibility_update_mode() getter

Specifies when visibility filters are updated.


Method Descriptions


● void add_visibility_filter(filter: Callable)

Adds a peer visibility filter for this synchronizer.

filter should take a peer ID int and return a bool.


● bool get_visibility_for(peer: int) const

Queries the current visibility for peer peer.


● void remove_visibility_filter(filter: Callable)

Removes a peer visibility filter from this synchronizer.


● void set_visibility_for(peer: int, visible: bool)

Sets the visibility of peer to visible. If peer is 0, the value of public_visibility will be updated instead.


● void update_visibility(for_peer: int = 0)

Updates the visibility of for_peer according to visibility filters. If for_peer is 0 (the default), all peers' visibilties are updated.

