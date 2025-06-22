using System.Collections.Generic;
using UnityEngine;

public static class AStar
{
    private class Node
    {
        public Vector2Int Position;
        public int GCost; // Cost from the start node
        public int HCost; // Heuristic cost to the end node
        public int FCost => GCost + HCost;
        public Node Parent;

        public Node(Vector2Int position)
        {
            Position = position;
        }
    }

    public static List<Vector2Int> Search(MazeRuntimeGrid grid, Vector2Int startPos, Vector2Int endPos)
    {
        Node startNode = new Node(startPos);
        Node endNode = new Node(endPos);

        List<Node> openSet = new List<Node> { startNode };
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost || (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode.Position);

            if (currentNode.Position == endNode.Position)
            {
                return RetracePath(startNode, currentNode);
            }

            foreach (var direction in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int neighborPosition = currentNode.Position + direction;
                
                // IsWall also handles bounds checking
                if (grid.IsWall(neighborPosition) || closedSet.Contains(neighborPosition))
                {
                    continue;
                }

                int newMovementCostToNeighbor = currentNode.GCost + 1;
                Node neighborNode = new Node(neighborPosition) { Parent = currentNode };

                bool inOpenSet = false;
                foreach(var openNode in openSet)
                {
                    if(openNode.Position == neighborPosition)
                    {
                        inOpenSet = true;
                        neighborNode = openNode;
                        break;
                    }
                }

                if (newMovementCostToNeighbor < neighborNode.GCost || !inOpenSet)
                {
                    neighborNode.GCost = newMovementCostToNeighbor;
                    neighborNode.HCost = GetManhattanDistance(neighborNode.Position, endNode.Position);
                    neighborNode.Parent = currentNode;

                    if (!inOpenSet)
                    {
                        openSet.Add(neighborNode);
                    }
                }
            }
        }

        return null; // No path found
    }

    private static List<Vector2Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node currentNode = endNode;

        while (currentNode != startNode)
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