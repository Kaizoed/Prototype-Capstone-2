namespace ShakySurvival.Cover
{
    /// <summary>
    /// Represents the player's current state in the cover system.
    /// </summary>
    public enum CoverState
    {
        // Player is not in cover and can move freely.
        Idle,
        
        // Player is transitioning into cover. Input is locked.
        Entering,
        
        // Player is hidden under cover. Limited look, no movement.
        Hidden,
        
        // Player is transitioning out of cover. Input is locked.
        Exiting
    }
}
