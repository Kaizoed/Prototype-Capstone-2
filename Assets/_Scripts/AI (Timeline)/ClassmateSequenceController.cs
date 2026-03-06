using UnityEngine;
using UnityEngine.AI;
using ShakySurvival.Earthquake;

public class ClassmateSequenceController : MonoBehaviour
{
    public enum ClassmateState
    {
        Idle,
        Talking,
        EarthquakeCrouch,
        Fallen,
        WaitingForHelp,
        HelpedUp,
        RunningOutside,
        Finished
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueData introDialogue;
    [SerializeField] private Transform outsideDestination;

    [Header("Animator Parameters")]
    [SerializeField] private string earthquakeCrouchParam = "EarthquakeCrouch";
    [SerializeField] private string fallenParam = "Fallen";
    [SerializeField] private string helpUpTrigger = "HelpUp";
    [SerializeField] private string walkParam = "IsWalking";

    [Header("Quest IDs")]
    [SerializeField] private string talkQuestId = "TalkToClassmate";
    [SerializeField] private string helpQuestId = "HelpClassmate";

    [Header("Timing")]
    [SerializeField] private float earthquakeStartDelay = 2f;
    [SerializeField] private float runOutsideDelay = 1.2f;
    [SerializeField] private float reachDistance = 0.8f;

    public ClassmateState CurrentState { get; private set; } = ClassmateState.Idle;

    private bool _waitingToStartEarthquake;
    private bool _earthquakeEnded;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        EarthquakeEvents.OnEarthquakeStart += OnEarthquakeStarted;
        EarthquakeEvents.OnEarthquakeStop += OnEarthquakeEnded;
    }

    private void OnDisable()
    {
        EarthquakeEvents.OnEarthquakeStart -= OnEarthquakeStarted;
        EarthquakeEvents.OnEarthquakeStop -= OnEarthquakeEnded;

        CancelInvoke(nameof(StartDelayedEarthquakeSequence));
        CancelInvoke(nameof(RunOutside));

        if (dialogueManager != null)
            dialogueManager.OnDialogueEnded -= HandleIntroDialogueEnded;
    }

    private void Start()
    {
        if (agent != null)
            agent.isStopped = true;
    }

    private void Update()
    {
        if (CurrentState == ClassmateState.RunningOutside && agent != null && outsideDestination != null)
        {
            if (!agent.pathPending && agent.remainingDistance <= reachDistance)
            {
                animator?.SetBool(walkParam, false);
                CurrentState = ClassmateState.Finished;
            }
        }
    }

    public void HandlePlayerInteract(GameObject interactor)
    {
        switch (CurrentState)
        {
            case ClassmateState.Idle:
                StartConversationAndEarthquake();
                break;

            case ClassmateState.WaitingForHelp:
                HelpClassmate();
                break;
        }
    }

    private void StartConversationAndEarthquake()
    {
        CurrentState = ClassmateState.Talking;

        if (dialogueManager != null && introDialogue != null)
        {
            _waitingToStartEarthquake = true;
            dialogueManager.OnDialogueEnded += HandleIntroDialogueEnded;
            dialogueManager.StartDialogue(introDialogue);
        }
        else
        {
            // Fallback: no dialogue assigned, still continue sequence
            Invoke(nameof(StartDelayedEarthquakeSequence), earthquakeStartDelay);
        }
    }

    private void HandleIntroDialogueEnded()
    {
        if (!_waitingToStartEarthquake) return;

        _waitingToStartEarthquake = false;

        if (dialogueManager != null)
            dialogueManager.OnDialogueEnded -= HandleIntroDialogueEnded;

        Invoke(nameof(StartDelayedEarthquakeSequence), earthquakeStartDelay);
    }

    private void StartDelayedEarthquakeSequence()
    {
        // Complete "TalkToClassmate" only when the delayed earthquake sequence actually begins
        QuestManager.Instance?.CompleteStep(talkQuestId);

        _earthquakeEnded = false;
        EarthquakeManager.Instance?.StartEarthquake();
    }

    private void OnEarthquakeStarted()
    {
        if (CurrentState == ClassmateState.Talking || CurrentState == ClassmateState.Idle)
        {
            CurrentState = ClassmateState.EarthquakeCrouch;
        }

        animator?.SetBool(earthquakeCrouchParam, true);

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    public void OnWallExplosionHit()
    {
        if (CurrentState == ClassmateState.Fallen || CurrentState == ClassmateState.WaitingForHelp)
            return;

        CurrentState = ClassmateState.Fallen;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.SetBool(earthquakeCrouchParam, false);
            animator.SetBool(fallenParam, true);
            animator.SetBool(walkParam, false);
        }
    }

    private void OnEarthquakeEnded()
    {
        _earthquakeEnded = true;

        if (CurrentState == ClassmateState.Fallen)
        {
            CurrentState = ClassmateState.WaitingForHelp;
        }
        else if (CurrentState == ClassmateState.EarthquakeCrouch)
        {
            animator?.SetBool(earthquakeCrouchParam, false);
            CurrentState = ClassmateState.Idle;
        }
    }

    private void HelpClassmate()
    {
        if (!_earthquakeEnded) return;

        CurrentState = ClassmateState.HelpedUp;

        if (animator != null)
        {
            animator.SetBool(fallenParam, false);
            animator.SetTrigger(helpUpTrigger);
        }

        QuestManager.Instance?.CompleteStep(helpQuestId);

        Invoke(nameof(RunOutside), runOutsideDelay);
    }

    private void RunOutside()
    {
        if (agent == null || outsideDestination == null)
        {
            CurrentState = ClassmateState.Finished;
            return;
        }

        CurrentState = ClassmateState.RunningOutside;

        animator?.SetBool(walkParam, true);

        agent.isStopped = false;
        agent.SetDestination(outsideDestination.position);
    }
}