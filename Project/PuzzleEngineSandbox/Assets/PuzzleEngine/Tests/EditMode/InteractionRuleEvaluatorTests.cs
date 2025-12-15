using NUnit.Framework;
using PuzzleEngine.Runtime.Core;
using UnityEngine;

[TestFixture]
public class InteractionRuleEvaluatorTests
{
    private InteractionRuleSO CreateRule(
        AdjacencyMode adjacency,
        CascadeMode cascade = CascadeMode.OnlySelectedPair)
    {
        var rule = ScriptableObject.CreateInstance<InteractionRuleSO>();
        rule.adjacencyMode = adjacency;
        rule.cascadeMode = cascade;
        return rule;
    }

    [Test]
    public void Anywhere_allows_any_two_distinct_cells()
    {
        var rule = CreateRule(AdjacencyMode.Anywhere);

        var a = new Vector2Int(0, 0);
        var b = new Vector2Int(5, 5);

        Assert.IsTrue(InteractionRuleEvaluator.IsSelectionAllowed(a, b, rule));
    }

    [Test]
    public void Orthogonal_only_allows_4_neighbors()
    {
        var rule = CreateRule(AdjacencyMode.Orthogonal);
        var center = new Vector2Int(3, 3);

        // Orthogonal neighbours
        Assert.IsTrue(InteractionRuleEvaluator.IsSelectionAllowed(center, center + Vector2Int.right, rule));
        Assert.IsTrue(InteractionRuleEvaluator.IsSelectionAllowed(center, center + Vector2Int.left, rule));
        Assert.IsTrue(InteractionRuleEvaluator.IsSelectionAllowed(center, center + Vector2Int.up, rule));
        Assert.IsTrue(InteractionRuleEvaluator.IsSelectionAllowed(center, center + Vector2Int.down, rule));

        // Diagonals should be rejected
        Assert.IsFalse(InteractionRuleEvaluator.IsSelectionAllowed(center, center + new Vector2Int(1, 1), rule));
        Assert.IsFalse(InteractionRuleEvaluator.IsSelectionAllowed(center, center + new Vector2Int(-1, 1), rule));
    }

    [Test]
    public void OrthogonalAndDiagonal_allows_8_neighbors()
    {
        var rule = CreateRule(AdjacencyMode.OrthogonalAndDiagonal);
        var center = new Vector2Int(3, 3);

        // Orthogonal neighbours
        Assert.IsTrue(InteractionRuleEvaluator.IsSelectionAllowed(center, center + Vector2Int.right, rule));
        Assert.IsTrue(InteractionRuleEvaluator.IsSelectionAllowed(center, center + Vector2Int.left, rule));
        Assert.IsTrue(InteractionRuleEvaluator.IsSelectionAllowed(center, center + Vector2Int.up, rule));
        Assert.IsTrue(InteractionRuleEvaluator.IsSelectionAllowed(center, center + Vector2Int.down, rule));

        // Diagonals should also be accepted
        Assert.IsTrue(InteractionRuleEvaluator.IsSelectionAllowed(center, center + new Vector2Int(1, 1), rule));
        Assert.IsTrue(InteractionRuleEvaluator.IsSelectionAllowed(center, center + new Vector2Int(-1, -1), rule));
    }

    [Test]
    public void GetNeighborCoords_orthogonal_returns_4_neighbors_inside_grid()
    {
        var rule = CreateRule(AdjacencyMode.Orthogonal);
        var grid = new GridModel(5, 5);
        var center = new Vector2Int(2, 2);

        var neighbors = InteractionRuleEvaluator
            .GetNeighborCoords(center, grid, rule);

        int count = 0;
        foreach (var n in neighbors)
        {
            count++;
            int manhattan = Mathf.Abs(n.x - center.x) + Mathf.Abs(n.y - center.y);
            Assert.AreEqual(1, manhattan, "Orthogonal neighbour must be distance 1 in manhattan metric.");
            Assert.IsTrue(grid.IsInside(n.x, n.y));
        }

        Assert.AreEqual(4, count);
    }

    [Test]
    public void GetNeighborCoords_anywhere_returns_8_neighbors_inside_grid()
    {
        var rule = CreateRule(AdjacencyMode.Anywhere);
        var grid = new GridModel(5, 5);
        var center = new Vector2Int(2, 2);

        var neighbors = InteractionRuleEvaluator
            .GetNeighborCoords(center, grid, rule);

        int count = 0;
        foreach (var n in neighbors)
        {
            count++;
            Assert.IsTrue(grid.IsInside(n.x, n.y));
        }

        // 8 neighbours in a 3x3 around the center
        Assert.AreEqual(8, count);
    }
}