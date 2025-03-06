using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace DrakenStark
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ManualSyncProxy : UdonSharpBehaviour
    {
        [Header("Foundational Components")]
        [SerializeField] private VRCPickup _vRCPickup = null;
        [SerializeField] private VRCObjectSync _vRCObjectSync = null;
        [SerializeField] private UdonSharpBehaviour _manualSyncScript = null;

        [Header("Reliability")]
        [Tooltip("Ensure Ownership: After the initial attempt to transfer network ownership to the picking up player (required for the player to update synced variables), this feature will check once a second while the player continues to hold the pickup until network ownership transfer is successful.\n" +
            "Usually the initial transfer works, otherwise the second attempt catches it. This mitigates having to account multiple causes of failure individually in a simple and lightweight way.\n" +
            "Recommended to be enabled.")]
        [SerializeField] private bool _ensureOwnership = true;

        [Header("Respawning")]
        [Tooltip("Dropped Respawn: After being dropped, respawns the pickup so it doesn't wind up being hidden or lost.\n" +
            "Recommended to be enabled.")]
        [SerializeField] private bool _droppedRespawn = true;
        [Tooltip("Dropped Time: If Dropped Respawn is enabled, this will be the time that a pickup will respawn after being dropped and left alone.\n" +
            "Can be set to zero for an instant respawn when dropped.")]
        [SerializeField] private float _droppedTimeout = 10f;
        private bool _droppedTimingOut = false;
        private uint _droppedIterations = 0;

        
        [Header("Intuitive Design")]
        [Tooltip("Fully Automatic Fire: If enabled, the use action will repeatedly fire for the player as they hold the use button down with respect to any cooldown period and queue window.\n" +
            "For this function to work, cooldown must be greater than 0.")]
        [SerializeField] bool _fullyAutoFire = false;
        [Tooltip("Cooldown: Prevents local repeat use actions within a time period measured in seconds." +
            "Recommended minimum: 0.25\n" +
            "Set to 0 if you do not wish to have a cooldown. (Not recommended.)\n" +
            "There can be multiple reasons to use a cooldown:\n" +
            "- Prevents players from rate limiting themselves and desyncing their local efforts from others around them.\n" +
            "- Can provide a level of fairness from what is generally regarded as human feasible versus tool assisted spamming with legal to use macros or maliciously abusable accessibility tools.\n" +
            "- Ensures the intensity of network traffic that could be created through legitimate means will be less likely to cause conjestion for the entire instance if all players are maximizing their effectiveness.\n" +
            "- May allow for animations or other interoperability to be appreciated between use actions.")]
        [SerializeField] float _cooldown = 0.30f;
        private bool _isCooled = true;
        private bool _coolingDown = false;
        private uint _cooldownIterations = 0;
        [Tooltip("Queue Window: Provided a cooldown time period, this is the window of time (in seconds) leading to when the cooldown ends that queues a use action immediately when the cooldown ends.\n" +
            "This feature provides the player room for error on optimal use action timing for gameplay that leans more on player intention rather than punishing players by ignoring their input if they are even slightly too early with their repeat use actions.\n" +
            "This feature can also allow a player to maximize their firerate without using a macro or other autofiring utilities. Their trigger pulls would ideally always fall within this timing to optimize, match, and respect the cooldown time.\n" +
            "Recommended: 0.15 (It is not common to have a reaction time faster than 0.25, about half of that for anticipated repeat actions, so just a smidge higher should be fine. Feel free to be more lenient.)" +
            "Set this to 0 to disable this feature.")]
        [SerializeField] float _queueWindow = 0.15f;
        private bool _queueWindowOpening = false;
        private uint _queueWindowIterations = 0;
        private bool _queueWindowOpen = false;
        private bool _queuedFire = false;

        private bool _useFilter = false; //This internal variable prevents input doubling, which can be an issue that is seemingly random.

        [Header("Input Debugger")]
        [SerializeField] private GameObject _inputIndicator = null;

        public override void OnDrop()
        {
            _manualSyncScript.SendCustomEvent("_proxyOnDrop");

            if (_droppedRespawn)
            {
                _droppedTimingOut = true;
                _droppedIterations++;
                SendCustomEventDelayedSeconds(nameof(_timedout), _droppedTimeout);
            }

            if (Utilities.IsValid(_inputIndicator))
            {
                _inputIndicator.SetActive(false);
            }
        }
        public void _timedout()
        {
            _droppedIterations--;
            if (_droppedIterations == 0 && _droppedTimingOut)
            {
                _vRCObjectSync.Respawn();
            }
        }

        public override void OnPickupUseUp()
        {
            if (_useFilter)
            {
                _useFilter = false;
                _manualSyncScript.SendCustomEvent("_proxyOnPickupUseUp");

                if (_fullyAutoFire && _queueWindowOpen)
                {
                    _queuedFire = true;
                }
            }

            if (Utilities.IsValid(_inputIndicator))
            {
                _inputIndicator.SetActive(false);
            }
        }

        public override void OnPickupUseDown()
        {
            if (!_useFilter || _fullyAutoFire)
            {
                _useFilter = true;

                if (_cooldown > 0f)
                {
                    //If there is a cooldown, it must be cooled before being able to immediately fire and then start the cooling process.
                    if (_isCooled)
                    {
                        _isCooled = false;
                        //Debug.LogWarning("Blaster is ready to fire, firing.");

                        //If fired while cooled, fire immediately.
                        _manualSyncScript.SendCustomEvent("_proxyOnPickupUseDown");
                        _startCoolingDown();
                    }
                    else if (_queueWindowOpen)
                    {
                        _queuedFire = true;
                        //Debug.LogWarning("Blaster shot queued for cooldown.");
                    }
                }
                else
                {
                    //There is no cooldown enabled.
                    _manualSyncScript.SendCustomEvent("_proxyOnPickupUseDown");
                }
            }

            if (Utilities.IsValid(_inputIndicator))
            {
                _inputIndicator.SetActive(true);
            }
        }

        private void _startCoolingDown()
        {
            //Start Cooling down.
            if (_queueWindow > 0f)
            {
                //Check if there's a valid QueueWindow.
                if (_queueWindow < _cooldown)
                {
                    _queueWindowOpen = false;
                    _coolingDown = false;

                    _queueWindowIterations++;
                    _queueWindowOpening = true;
                    SendCustomEventDelayedSeconds(nameof(_openQueueWindow), _queueWindow);
                }
                else
                {
                    //QueueWindow is the same or larger than the cooldown time. Immediately enable the QueueWindow.
                    _queueWindowOpen = true;
                    _queueWindowOpening = false;

                    _cooldownIterations++;
                    _coolingDown = true;
                    SendCustomEventDelayedSeconds(nameof(_cooledDown), _cooldown);
                }
            }
            else
            {
                //There is no QueueWindow, wait for cooldown.
                _queueWindowOpen = false;
                _queueWindowOpening = false;

                _cooldownIterations++;
                _coolingDown = true;
                SendCustomEventDelayedSeconds(nameof(_cooledDown), _cooldown);
            }
        }

        public void _openQueueWindow()
        {
            _queueWindowIterations--;
            if (_queueWindowOpening && _queueWindowIterations == 0)
            {
                _queueWindowOpening = false;

                _queueWindowOpen = true;
                //Debug.LogWarning("Blaster queue window is now open.");


                _cooldownIterations++;
                _coolingDown = true;
                SendCustomEventDelayedSeconds(nameof(_cooledDown), _cooldown - _queueWindow);
            }
        }

        public void _cooledDown()
        {
            _cooldownIterations--;
            if (_coolingDown && _cooldownIterations == 0)
            {
                _coolingDown = false;

                _queueWindowOpen = false;
                //Debug.LogWarning("Blaster cooldown completed.");

                if (_queuedFire || (_useFilter && _fullyAutoFire))
                {
                    _queuedFire = false;

                    _manualSyncScript.SendCustomEvent("_proxyOnPickupUseDown");
                    _startCoolingDown();
                }
                else
                {
                    _isCooled = true;
                }

            }
        }

        public override void OnPickup()
        {
            _droppedTimingOut = false;

            Networking.SetOwner(Networking.LocalPlayer, _manualSyncScript.gameObject);
            if (_ensureOwnership)
            {
                //Ownership requests can fail, start checking periodically to ensure it succeeds.
                SendCustomEventDelayedSeconds(nameof(_doubleCheckOwnership), 1);
            }

            _manualSyncScript.SendCustomEvent("_proxyOnPickup");
        }

        public void _doubleCheckOwnership()
        {
            //Ownership requests can fail, try periodically until it succeeds as long as the Cannon is held.
            if (Utilities.IsValid(_vRCPickup.currentPlayer) && _vRCPickup.currentPlayer == Networking.LocalPlayer && !Networking.IsOwner(_manualSyncScript.gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, _manualSyncScript.gameObject);
                SendCustomEventDelayedSeconds(nameof(_doubleCheckOwnership), 1);
                return;
            }
        }

        public void _dropRespawn()
        {
            //Drop local only to prevent malicious remote use. Drop and Respawn will both be ignored if another player is holding the pickup.
            _vRCPickup.Drop();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Respawn));
        }

        public void Respawn()
        {
            _vRCObjectSync.Respawn();
        }

        public void _enableInteract()
        {
            _vRCPickup.pickupable = true;
        }
        public void _disableInteract()
        {
            _vRCPickup.pickupable = false;
        }
    }
}