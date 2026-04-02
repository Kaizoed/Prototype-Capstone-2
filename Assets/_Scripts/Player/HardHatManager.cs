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
                blinkOverlay.interactable = false;
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

        public void GiveHardHat()
        {
            hasHardHat = true;
        }

        public void RemoveHardHat()
        {
            hasHardHat = false;

            if (isEquipped && hardHatBrim != null)
            {
                hardHatBrim.SetActive(false);
                isEquipped = false;
            }
        }

        public bool IsAnimating => isAnimating;
        public bool IsEquipped => isEquipped;

        // ====================================================================
        // THE IMMERSIVE EQUIP COROUTINE
        // ====================================================================

        private IEnumerator ImmersiveEquipSequence()
        {
            isAnimating = true;

            Quaternion originalLocalRotation = playerCamera.localRotation;

            float phase1Duration = animationDuration * 0.30f;
            float phase2Duration = animationDuration * 0.70f;

            float elapsed = 0f;

            while (elapsed < phase1Duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / phase1Duration);
                float smooth = Mathf.SmoothStep(0f, 1f, t);

                blinkOverlay.alpha = smooth;

                playerCamera.localRotation = originalLocalRotation
                    * Quaternion.Euler(cameraDipAngle * smooth, 0f, 0f);

                yield return null;
            }

            blinkOverlay.alpha = 1f;
            playerCamera.localRotation = originalLocalRotation
                * Quaternion.Euler(cameraDipAngle, 0f, 0f);

            // ==================================================================
            // THE CONTACT – screen is fully black, nobody can see a thing
            // ==================================================================

            if (audioSource != null && equipSound != null)
            {
                audioSource.PlayOneShot(equipSound);
            }

            if (hardHatBrim != null)
            {
                hardHatBrim.SetActive(!isEquipped);
            }

            // Flip state.
            isEquipped = !isEquipped;

            // Complete hardhat objective when equipped
            if (isEquipped && TutorialObjectiveUI.Instance != null)
            {
                TutorialObjectiveUI.Instance.CompleteObjective("hardhat");
            }

            yield return new WaitForSeconds(0.05f);

            elapsed = 0f;

            while (elapsed < phase2Duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / phase2Duration);
                float smooth = Mathf.SmoothStep(0f, 1f, t);

                blinkOverlay.alpha = 1f - smooth;

                float remainingDip = cameraDipAngle * (1f - smooth);
                playerCamera.localRotation = originalLocalRotation
                    * Quaternion.Euler(remainingDip, 0f, 0f);

                yield return null;
            }

            blinkOverlay.alpha = 0f;
            playerCamera.localRotation = originalLocalRotation;

            isAnimating = false;
        }
    }
}