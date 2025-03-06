# How to use
- Add ManualSyncProxy to the same GameObject as the VRCPickup with VRCObjectSync
- Drag the VRCPickup and VRCObjectSync components (or GameObject from the heirarchy) onto the VRCPickup and VRCObjectSync variables on the ManualSyncProxy component.
- Configure the desired variables to your liking.
- Create or modify the Manual Sync script to use supported functions as applicable.
- Drag the target script GameObject from the heirarchy onto the Manual Sync Script variable on the ManualSyncProxy component.

Supported functions for Manual Sync scripts:
- OnDrop = _proxyOnDrop
- OnPickup = _proxyOnPickup
- OnPickupUseDown = _proxyOnPickupUseDown
- OnPickupUseUp = _proxyOnPickupUseUp

# Description
This is a script designed to allow the best of both worlds for syncing VRCPickups in VRChat and save time making new pickup scripts with overlapping functionality. ManualSyncProxy has additional customization to include things like 
- Respawning after being dropped and unused for a set time
- Cooldown time for any repeated use
- Timing forgiveness to queue the next use intuitively on cooldown
- Automatic fire
- Network ownership management for the target Manual Sync script to keep up with the VRCPickup's network ownership

I've been making pickup based scripts for a while and I've found myself repeatedly implementing these features in them. My goal being to up the intuitive quality for pickups across multiple VRChat worlds and events. ManualSyncProxy is intended to simplify the Manual Sync pickup scripts that need to be created, streamlining them to on their unique functionality.

ManualSyncProxy does not include a target Manual Sync script example. I've not yet designed one that I feel is versatile enough to include as part of this repository.

# Why
VRChat has two kinds of Sync modes for scripts: Continuous and Manual. These two Sync modes cannot be mixed together on the same GameObject.
- Continuous: Periodically updates variables. Great for syncing positions of things, hense why VRCObejectSync uses it.
- Manual: Updates variables immediately and only when told to do so. This is great for when players cause an event to happen, like pushing a button. This is great for when pickups have literally any functionality that other players should see that relies on pressing the Use button.
The solution is a script like ManualSyncProxy, which can forward the local functions of the Pickup instantly over to a separate GameObject that can use the Manual Sync mode. This technique offers the best of both worlds, provided you know how to create or edit scripts.
