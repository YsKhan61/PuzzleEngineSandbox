using UnityEngine;

namespace PuzzleEngine.Runtime.Core
{
    /// <summary>
    /// How strictly we constrain which two cells the player
    /// is allowed to select as an interaction pair.
    /// </summary>
    public enum AdjacencyMode
    {
        Anywhere,               // like Merge Mansion: any 2 cells on the board
        Orthogonal,             // 4-neighbour only
        OrthogonalAndDiagonal   // 8-neighbour (orthogonal + diagonals)
    }

    /// <summary>
    /// How far the merge "reaction" propagates once a valid pair is chosen.
    /// </summary>
    public enum CascadeMode
    {
        OnlySelectedPair,       // only the chosen pair is merged
        SelectedPairAndNeighbors, // chosen pair + its neighbours get a single pass
        GlobalCascade           // run full-board simulation (your current behaviour)
    }
    
    [CreateAssetMenu(
        fileName = "InteractionRule",
        menuName = "PuzzleEngine/Interaction Rule")]
    public class InteractionRuleSO : ScriptableObject
    {
        public AdjacencyMode adjacencyMode = AdjacencyMode.Anywhere;
        public CascadeMode cascadeMode = CascadeMode.GlobalCascade;
    }
}
