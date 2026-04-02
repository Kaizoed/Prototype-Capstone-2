using System.Collections;
using UnityEngine;

namespace ShakySurvival.Player
{
    /// <summary>
    /// Manages the immersive equip/unequip sequence for the player's hard hat.
    /// 
    /// Because the player model is headless and shadows are disabled, there is no
    /// character animation to play. Instead, we fake the "putting on / taking off" action
    /// by combining three simultaneous tricks:
    ///   1. A downward camera dip   – simulates the player looking down to grab the hat.
    ///   2. A full-screen blink     – hides the exact frame the hat model pops on/off.
    ///   3. An audio sting           – the "clunk + strap" sells the physical contact.
    ///
    /// The blink begins fading in at the SAME TIME as the camera dips. By the time the
    /// screen is fully black the camera is at its lowest point, the hat is toggled, and
    /// the sound plays. The camera then recovers upward while the blink fades back out.
    ///
    /// All rotations are applied to the camera's LOCAL space so they layer on top of
    /// whatever PlayerLook / PlayerStagger are doing, rather than fighting them.
    /// </summary>
    public class HardHatImmersiveManager : MonoBehaviour
    {
        // ====================================================================
        // EXPOSED VARIABLES
        // ====================================================================

        [Header("Components")]

        [SerializeField, Tooltip("Reference to the player camera (or camera root) transform.")]
        private Transform playerCamera;

        [SerializeField, Tooltip("The 3D hard-hat brim model parented under the camera.")]
        private GameObject hardHatBrim;

        [SerializeField, Tooltip("A full-screen black UI panel with a CanvasGroup (alpha 0 at rest).")]
        private CanvasGroup blinkOverlay;

        [SerializeField, Tooltip("AudioSource used to play the equip sound.")]
        private AudioSource audioSource;

        [SerializeField, Tooltip("The clunk/strap sound clip played at the moment of contact.")]
        private AudioClip equipSound;

        [Header("Settings")]

        [SerializeField, Tooltip("Key used to trigger equip / unequip.")]
        private KeyCode equipKey = KeyCode.H;

        [SerializeField, Tooltip("How many degrees the camera dips downward on the local X-axis.")]
        private float cameraDipAngle = 15f;

        [SerializeField, Tooltip("Total duration of the full equip/unequip sequence (seconds).")]
        private float animationDuration = 0.75f;

        [Header("State")]

        [SerializeField, Tooltip("Does the player currently own a hard hat? Gate for the action.")]
        private bool hasHardHat;

        [SerializeField, Tooltip("Is the hard hat currently equipped (visible on the camera)?")]
        private bool isEquipped;

        // Private flag – prevents the coroutine from being started multiple times.
        private bool isAnimating;

        // ====================================================================
        // UNITY LIFECYCLE
        // ====================================================================

        private void Start()
        {
            // Make sure the blink overlay starts fully transparent.
            if (blinkOverlay != null)
            {
                blinkOverlay.alpha = 0f;

                // The overlay should NOT intercept clicks or block raycasts at rest.
                blinkOverlay.blocksRaycasts = false;
                blinkOverlay.interactable  = false;
            }

            // Sync the brim visibility with the initial equipped state.
            if (hardHatBrim != null)
            {
                hardHatBrim.SetActive(isEquipped);
            }
        }

        private void Update()
        {
            // Listen for the equip key only when the player has a hat and isn't
            // mid-sequence already.
            if (Input.GetKeyDown(equipKey) && hasHardHat && !isAnimating)
            {
                StartCoroutine(ImmersiveEquipSequence());
            }
        }

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Call this from other systems (e.g. pick-up, inventory) to let the
        /// player know they now own a hard hat and can equip it.
        /// </summary>
        public void GiveHardHat()
        {
            hasHardHat = true;
        }

        /// <summary>
        /// Revoke the hard hat. If it's currently equipped, instantly hides the brim.
        /// </summary>
        public void RemoveHardHat()
        {
            hasHardHat = false;

            if (isEquipped && hardHatBrim != null)
            {
                hardHatBrim.SetActive(false);
                isEquipped = false;
            }
        }

        /// <summary>
        /// Returns true while the equip/unequip animation is playing.
        /// Useful for other scripts that may want to block input during this.
        /// </summary>
        public bool IsAnimating => isAnimating;

        /// <summary>
        /// Returns whether the hard hat is currently equipped.
        /// </summary>
        public bool IsEquipped => isEquipped;

        // ====================================================================
        // THE IMMERSIVE EQUIP COROUTINE
        // ====================================================================

        /// <summary>
        /// Orchestrates the full equip / unequip illusion in three beats:
        ///   Phase 1  – "Close eyes & dip"   (first 30% of the duration)
        ///   Contact  – Toggle hat, play SFX  (instant, at full darkness)
        ///   Phase 2  – "Open eyes & recover" (remaining 70% of the duration)
        ///
        /// The overlay fade and the camera dip happen SIMULTANEOUSLY within each
        /// phase, giving the impression of a single fluid head-bob rather than
        /// two separate effects.
        /// </summary>
        private IEnumerator ImmersiveEquipSequence()
        {
            isAnimating = true;

            // ------------------------------------------------------------------
            // Snapshot the camera's current LOCAL rotation so we can additively
            // apply our dip on top of it, and perfectly restore it afterwards.
            // This is critical – PlayerLook writes to cameraRoot.localRotation
            // every frame, so we work with the EULER representation and add our
            // offset rather than overwriting the quaternion.
            // ------------------------------------------------------------------
            Quaternion originalLocalRotation = playerCamera.localRotation;

            // Pre-calculate phase durations.
            float phase1Duration = animationDuration * 0.30f; // 30% – closing eyes
            float phase2Duration = animationDuration * 0.70f; // 70% – opening eyes

            // ==================================================================
            // PHASE 1: Closing Eyes & Dipping Camera  (30% of total time)
            // ------------------------------------------------------------------
            // Both the blink overlay alpha and the camera pitch change are driven
            // by the same normalised 't' value so they stay perfectly in sync.
            // ==================================================================
            float elapsed = 0f;

            while (elapsed < phase1Duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / phase1Duration);

                // Use SmoothStep for a pleasant ease-in / ease-out curve.
                float smooth = Mathf.SmoothStep(0f, 1f, t);

                // --- Blink: fade from transparent to fully black ---
                blinkOverlay.alpha = smooth;

                // --- Camera dip: rotate downward on local X-axis ---
                // We READ the current local rotation each frame (which PlayerLook
                // may have updated), then ADDITIVELY apply our dip offset. This
                // means mouse-look still works during the animation if desired,
                // and shake/stagger effects layer correctly.
                playerCamera.localRotation = originalLocalRotation
                    * Quaternion.Euler(cameraDipAngle * smooth, 0f, 0f);

                yield return null;
            }

            // Snap to exact end-of-phase values to avoid floating-point drift.
            blinkOverlay.alpha = 1f;
            playerCamera.localRotation = originalLocalRotation
                * Quaternion.Euler(cameraDipAngle, 0f, 0f);

            // ==================================================================
            // THE CONTACT – screen is fully black, nobody can see a thing
            // ------------------------------------------------------------------
            // This is the sleight-of-hand moment. We:
            //   1. Play the satisfying equip sound (auditory confirmation).
            //   2. Instantly toggle the hat brim model on or off.
            //   3. Flip the equipped boolean.
            // Because the screen is 100% black, the pop-on/pop-off is invisible.
            // ==================================================================

            // Play the equip / unequip sound.
            if (audioSource != null && equipSound != null)
            {
                audioSource.PlayOneShot(equipSound);
            }

            // Toggle the hat brim visibility.
            if (hardHatBrim != null)
            {
                hardHatBrim.SetActive(!isEquipped);
            }

            // Flip state.
            isEquipped = !isEquipped;

            // A tiny dramatic pause while the screen is black.
            // This gives the player a micro-moment to register the sound before
            // the "eyes" start opening again.
            yield return new WaitForSeconds(0.05f);

            // ==================================================================
            // PHASE 2: Opening Eyes & Recovering Camera  (70% of total time)
            // ------------------------------------------------------------------
            // Same dual-channel approach: overlay alpha and camera pitch both
            // interpolate back to their resting states in lockstep.
            // The longer duration here feels natural – humans open their eyes
            // more slowly than they close them when bracing for contact.
            // ==================================================================
            elapsed = 0f;

            while (elapsed < phase2Duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / phase2Duration);

                float smooth = Mathf.SmoothStep(0f, 1f, t);

                // --- Blink: fade from fully black back to transparent ---
                blinkOverlay.alpha = 1f - smooth;

                // --- Camera recover: rotate back up to original pitch ---
                // 'smooth' goes 0→1, so we interpolate from full dip back to zero.
                float remainingDip = cameraDipAngle * (1f - smooth);
                playerCamera.localRotation = originalLocalRotation
                    * Quaternion.Euler(remainingDip, 0f, 0f);

                yield return null;
            }

            // Snap to exact rest values.
            blinkOverlay.alpha = 0f;
            playerCamera.localRotation = originalLocalRotation;

            // ==================================================================
            // CLEANUP
            // ==================================================================
            isAnimating = false;
        }
    }
}
