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

    private void OnEnable()
    {
        EarthquakeEvents.OnEarthquakeStart += OnEarthquakeStarted;
        EarthquakeEvents.OnEarthquakeStop += OnEarthquakeEnded;
    }

    private void OnDisable()
    {
        EarthquakeEvents.OnEarthquakeStart -= OnEarthquakeStarted;
        EarthquakeEvents.OnEarthquakeStop -= OnEarthquakeEnded;
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

        QuestManager.Instance?.CompleteStep(talkQuestId);
    }

    private void HandleIntroDialogueEnded()
    {
        if (!_waitingToStartEarthquake) return;

        _waitingToStartEarthquake = false;

        if (dialogueManager != null)
            dialogueManager.OnDialogueEnded -= HandleIntroDialogueEnded;

        EarthquakeManager.Instance?.StartEarthquake();
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