using System.Collections.Generic;
using UnityEngine;

public static class AStar
{
    private class Node
    {
        public Vector2Int Position;
        public int GCost; // Distance from starting node
        public int HCost; // Heuristic distance to end node
        public int FCost => GCost + HCost;
        public Node Parent;

        public Node(Vector2Int position)
        {
            Position = position;
        }
    }

    public static List<Vector2Int> FindPath(MazeRuntimeGrid grid, Vector2Int startPos, Vector2Int endPos)
    {
        Node startNode = new Node(startPos);
        Node endNode = new Node(endPos);

        List<Node> openList = new List<Node> { startNode };
        HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();

        startNode.GCost = 0;
        startNode.HCost = GetManhattanDistance(startNode.Position, endNode.Position);

        while (openList.Count > 0)
        {
            Node currentNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].FCost < currentNode.FCost || (openList[i].FCost == currentNode.FCost && openList[i].HCost < currentNode.HCost))
                {
                    currentNode = openList[i];
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode.Position);

            if (currentNode.Position == endNode.Position)
            {
                return RetracePath(startNode, currentNode);
            }

            foreach (Vector2Int direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int neighborPos = currentNode.Position + direction;

                if (grid.IsWall(neighborPos) || closedList.Contains(neighborPos))
                {
                    continue;
                }

                int newMovementCostToNeighbor = currentNode.GCost + 1;
                Node neighborNode = new Node(neighborPos)
                {
                    GCost = newMovementCostToNeighbor,
                    HCost = GetManhattanDistance(neighborPos, endNode.Position),
                    Parent = currentNode
                };

                if (!openList.Exists(node => node.Position == neighborPos))
                {
                    openList.Add(neighborNode);
                }
            }
        }

        return new List<Vector2Int>();
    }

    private static List<Vector2Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node currentNode = endNode;

        while (currentNode != null && currentNode.Position != startNode.Position)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }
        path.Add(startNode.Position);
        path.Reverse();
        return path;
    }

    private static int GetManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}