using System;
using UnityEngine;

public class TraversalInteractionComponent : MonoBehaviour
{
    public void OnInteract(ITerrainInteraction interaction)
    {
        if (interaction == null) return;

        switch (interaction.CollisionType)
        {
            case CollisionType.Entered:

                OnEnterInteraction(interaction);
                break;
            case CollisionType.Exited:

                OnExitInteraction(interaction);
                break;
            case CollisionType.Stayed:

                interactedThisFrame = true;
                lastInteraction = interaction;
                OnStayInteraction(interaction);
                break;
        }
    }

    private void FixedUpdate()
    {
        AttemptEntryExitFromStay();
    }

    private bool interactedThisFrame = false;
    private bool interactedLastFrame = false;
    private ITerrainInteraction lastInteraction;

    private void AttemptEntryExitFromStay()
    {
        bool entered = interactedThisFrame && !interactedLastFrame;
        bool exited = !interactedThisFrame && interactedLastFrame;

        if (lastInteraction != null)
        {
            if (entered)
            {
                lastInteraction.CollisionType = CollisionType.Entered;
                Debug.Log("entered");
                OnEnterInteraction(lastInteraction);
            }
            else if (exited)
            {
                lastInteraction.CollisionType = CollisionType.Exited;
                Debug.Log("exited");
                OnEnterInteraction(lastInteraction);
            }
        }

        interactedLastFrame = interactedThisFrame;
        interactedThisFrame = false;
    }


    public event Action<ITerrainInteraction> OnInteractionEnter;
    public event Action<ITerrainInteraction> OnInteractionExit;
    public event Action<ITerrainInteraction> OnInteractionStay;
    private void OnEnterInteraction(ITerrainInteraction interaction) => OnInteractionEnter?.Invoke(interaction);
    private void OnExitInteraction(ITerrainInteraction interaction) => OnInteractionExit?.Invoke(interaction);
    private void OnStayInteraction(ITerrainInteraction interaction) => OnInteractionStay?.Invoke(interaction);

}



