using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EcholocationSystem : MonoBehaviour
{
    [Header("Echolocation Settings")]
    public KeyCode echolocationKey = KeyCode.C;
    public float waveStartSize = 0.5f;
    public float waveMaxSize = 20f;
    public float waveSpeed = 10f;
    public float waveDuration = 2f;
    public float waveSpacing = 0.3f;

    [Header("Colors")]
    public Color correctPathColor = Color.green;
    public Color wrongPathColor = Color.red;

    [Header("Maze Settings")]
    public float cellSize = 5f;
    public float detectionAngle = 90f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip echolocationSound;

    [Header("Sonar Wave Settings")]
    public bool useEnhancedSonar = true;
    public LayerMask wallLayerMask = -1;
    public AudioClip wallHitSound;

    private Camera playerCamera;
    private MazeSpawner mazeSpawner;
    private bool isEcholocating = false;

    // The solution path data
    private bool[,] pathToGoal;
    private Vector2Int[,] nextDirection;
    private int mazeRows, mazeCols;
    private Vector2Int actualStartPos;
    private Vector2Int goalPos;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<Camera>();
        }

        mazeSpawner = FindFirstObjectByType<MazeSpawner>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        StartCoroutine(CalculatePathAfterGeneration());
    }

    IEnumerator CalculatePathAfterGeneration()
    {
        yield return new WaitForSeconds(0.5f);

        if (mazeSpawner != null)
        {
            mazeRows = mazeSpawner.Rows;
            mazeCols = mazeSpawner.Columns;
            cellSize = mazeSpawner.CellWidth;

            CalculateSolutionPath();
        }
    }

    void CalculateSolutionPath()
    {
        pathToGoal = new bool[mazeRows, mazeCols];
        nextDirection = new Vector2Int[mazeRows, mazeCols];

        // Let's be more careful about finding the actual start position
        Vector3 playerWorldPos = transform.position;
        actualStartPos = WorldToGrid(playerWorldPos);

        //Debug.Log($"=== PATH CALCULATION DEBUG ===");
        //Debug.Log($"Maze size: {mazeRows}x{mazeCols}");
        //Debug.Log($"Cell size: {cellSize}");
        //Debug.Log($"Player world position: {playerWorldPos}");
        //Debug.Log($"Calculated grid position: {actualStartPos}");

        // Validate and adjust start position if needed
        actualStartPos = ValidateGridPosition(actualStartPos);
        //Debug.Log($"Validated start position: {actualStartPos}");

        // Goal is center of end room
        goalPos = new Vector2Int(mazeRows - 2, mazeCols - 2);
        //Debug.Log($"Goal position (grid): {goalPos}");

        // Check if start position has valid connections
        DebugCellConnections(actualStartPos, "START CELL");

        List<Vector2Int> solutionPath = FindPathAStar(actualStartPos, goalPos);

        if (solutionPath != null && solutionPath.Count > 0)
        {
            //Debug.Log($"Found solution path with {solutionPath.Count} cells");
            //Debug.Log("=== SOLUTION PATH ===");

            for (int i = 0; i < solutionPath.Count; i++)
            {
                Vector2Int current = solutionPath[i];
                pathToGoal[current.x, current.y] = true;

                if (i < solutionPath.Count - 1)
                {
                    Vector2Int next = solutionPath[i + 1];
                    nextDirection[current.x, current.y] = next - current;
                }

                //Debug.Log($"Path step {i}: ({current.x},{current.y})");
            }
        }
        else
        {
            //Debug.LogError("No solution path found!");
            // Try to find any path to anywhere reachable
            DebugReachableCells();
        }

        //Debug.Log($"Solution path calculated! Path cells marked: {CountPathCells()}");
        //Debug.Log("=== END PATH CALCULATION ===");
    }

    Vector2Int ValidateGridPosition(Vector2Int gridPos)
    {
        // Clamp to valid bounds
        gridPos.x = Mathf.Clamp(gridPos.x, 0, mazeRows - 1);
        gridPos.y = Mathf.Clamp(gridPos.y, 0, mazeCols - 1);

        // Check if this position is actually accessible (not a wall)
        var mazeGenerator = GetMazeGenerator();
        if (mazeGenerator != null)
        {
            try
            {
                var cell = mazeGenerator.GetMazeCell(gridPos.x, gridPos.y);
                // If we can read the cell, the position is valid
                //Debug.Log($"Cell at {gridPos} is valid");
                return gridPos;
            }
            catch
            {
                //Debug.LogWarning($"Cell at {gridPos} is invalid, trying nearby cells");

                // Try nearby cells if the calculated position is invalid
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        Vector2Int testPos = new Vector2Int(gridPos.x + dx, gridPos.y + dy);
                        if (testPos.x >= 0 && testPos.x < mazeRows &&
                            testPos.y >= 0 && testPos.y < mazeCols)
                        {
                            try
                            {
                                var testCell = mazeGenerator.GetMazeCell(testPos.x, testPos.y);
                                //Debug.Log($"Using nearby valid cell: {testPos}");
                                return testPos;
                            }
                            catch { continue; }
                        }
                    }
                }
            }
        }

        return gridPos; // Fallback to original if nothing else works
    }

    void DebugCellConnections(Vector2Int gridPos, string label)
    {
        var mazeGenerator = GetMazeGenerator();
        if (mazeGenerator == null) return;

        try
        {
            var cell = mazeGenerator.GetMazeCell(gridPos.x, gridPos.y);
            //Debug.Log($"=== {label} CONNECTIONS ===");
            //Debug.Log($"Position: {gridPos}");
            //Debug.Log($"Walls - Front:{cell.WallFront}, Right:{cell.WallRight}, Back:{cell.WallBack}, Left:{cell.WallLeft}");

            // Check each direction
            Vector2Int[] directions = {
                new Vector2Int(1, 0),   // North
                new Vector2Int(0, 1),   // East
                new Vector2Int(-1, 0),  // South
                new Vector2Int(0, -1)   // West
            };
            string[] dirNames = { "North", "East", "South", "West" };

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int neighbor = gridPos + directions[i];
                bool canMove = CanMoveBetweenCells(gridPos, neighbor);
                //Debug.Log($"Can move {dirNames[i]} to {neighbor}: {canMove}");
            }
        }
        catch (System.Exception e)
        {
            //Debug.LogError($"Error debugging cell {gridPos}: {e.Message}");
        }
    }

    void DebugReachableCells()
    {
        //Debug.Log("=== CHECKING REACHABLE CELLS ===");
        var reachable = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();

        queue.Enqueue(actualStartPos);
        reachable.Add(actualStartPos);

        Vector2Int[] directions = {
            new Vector2Int(1, 0), new Vector2Int(0, 1),
            new Vector2Int(-1, 0), new Vector2Int(0, -1)
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighbor = current + direction;

                if (neighbor.x >= 0 && neighbor.x < mazeRows &&
                    neighbor.y >= 0 && neighbor.y < mazeCols &&
                    !reachable.Contains(neighbor) &&
                    CanMoveBetweenCells(current, neighbor))
                {
                    reachable.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        //Debug.Log($"Found {reachable.Count} reachable cells from start position");
        //Debug.Log($"Goal {goalPos} is reachable: {reachable.Contains(goalPos)}");

        if (!reachable.Contains(goalPos))
        {
            //Debug.LogError("GOAL IS NOT REACHABLE FROM START POSITION!");
            // Find the closest reachable cell to the goal
            float minDist = float.MaxValue;
            Vector2Int closestToGoal = actualStartPos;
            foreach (Vector2Int cell in reachable)
            {
                float dist = Vector2Int.Distance(cell, goalPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestToGoal = cell;
                }
            }
            //Debug.Log($"Closest reachable cell to goal: {closestToGoal} (distance: {minDist})");
        }
    }

    List<Vector2Int> FindPathAStar(Vector2Int start, Vector2Int goal)
    {
        var openSet = new List<Vector2Int>();
        var closedSet = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float>();
        var fScore = new Dictionary<Vector2Int, float>();

        openSet.Add(start);
        gScore[start] = 0;
        fScore[start] = Vector2Int.Distance(start, goal);

        Vector2Int[] directions = {
            new Vector2Int(1, 0),   // North
            new Vector2Int(0, 1),   // East
            new Vector2Int(-1, 0),  // South
            new Vector2Int(0, -1)   // West
        };

        while (openSet.Count > 0)
        {
            Vector2Int current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (fScore.ContainsKey(openSet[i]) &&
                    (!fScore.ContainsKey(current) || fScore[openSet[i]] < fScore[current]))
                {
                    current = openSet[i];
                }
            }

            if (current == goal)
            {
                var path = new List<Vector2Int>();
                Vector2Int pathNode = current;

                while (cameFrom.ContainsKey(pathNode))
                {
                    path.Add(pathNode);
                    pathNode = cameFrom[pathNode];
                }
                path.Add(start);
                path.Reverse();

                //Debug.Log($"A* found path from {start} to {goal} with {path.Count} steps");
                return path;
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighbor = current + direction;

                if (neighbor.x < 0 || neighbor.x >= mazeRows || neighbor.y < 0 || neighbor.y >= mazeCols)
                    continue;

                if (closedSet.Contains(neighbor))
                    continue;

                if (!CanMoveBetweenCells(current, neighbor))
                    continue;

                float tentativeGScore = gScore[current] + 1;

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
                else if (gScore.ContainsKey(neighbor) && tentativeGScore >= gScore[neighbor])
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + Vector2Int.Distance(neighbor, goal);
            }
        }

        //Debug.LogError("A* failed to find path!");
        return null;
    }

    bool CanMoveBetweenCells(Vector2Int from, Vector2Int to)
    {
        if (mazeSpawner == null) return false;

        var mazeGenerator = GetMazeGenerator();
        if (mazeGenerator == null) return false;

        try
        {
            var fromCell = mazeGenerator.GetMazeCell(from.x, from.y);
            Vector2Int direction = to - from;

            bool canMove = false;

            if (direction == new Vector2Int(1, 0)) // Moving north
                canMove = !fromCell.WallFront;
            else if (direction == new Vector2Int(0, 1)) // Moving east
                canMove = !fromCell.WallRight;
            else if (direction == new Vector2Int(-1, 0)) // Moving south
                canMove = !fromCell.WallBack;
            else if (direction == new Vector2Int(0, -1)) // Moving west
                canMove = !fromCell.WallLeft;

            return canMove;
        }
        catch (System.Exception e)
        {
            //Debug.LogError($"Error checking cells ({from.x},{from.y}) to ({to.x},{to.y}): {e.Message}");
            return false;
        }
    }

    BasicMazeGenerator GetMazeGenerator()
    {
        var field = typeof(MazeSpawner).GetField("mMazeGenerator",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(mazeSpawner) as BasicMazeGenerator;
    }

    int CountPathCells()
    {
        if (pathToGoal == null) return 0;
        int count = 0;
        for (int i = 0; i < mazeRows; i++)
        {
            for (int j = 0; j < mazeCols; j++)
            {
                if (pathToGoal[i, j]) count++;
            }
        }
        return count;
    }

    void Update()
    {
        if (Input.GetKeyDown(echolocationKey) && !isEcholocating)
        {
            StartEcholocation();
        }
    }

    void StartEcholocation()
    {
        if (pathToGoal == null)
        {
            //Debug.LogWarning("Solution path not calculated yet!");
            return;
        }

        isEcholocating = true;

        if (audioSource != null && echolocationSound != null)
        {
            audioSource.PlayOneShot(echolocationSound);
        }

        bool onCorrectPath = IsOnCorrectPath();
        Color waveColor = onCorrectPath ? correctPathColor : wrongPathColor;

        StartCoroutine(CreateWaveSequence(waveColor));

        //Debug.Log($"Echolocation: On correct path = {onCorrectPath}");
    }

    bool IsOnCorrectPath()
    {
        Vector3 worldPos = transform.position;
        Vector2Int currentGridPos = WorldToGrid(worldPos);

        //Debug.Log($"=== ECHOLOCATION CHECK ===");
        //Debug.Log($"Player world pos: {worldPos}");
        //Debug.Log($"Player grid pos: {currentGridPos}");

        if (currentGridPos.x < 0 || currentGridPos.x >= mazeRows ||
            currentGridPos.y < 0 || currentGridPos.y >= mazeCols)
        {
            //Debug.Log("Player outside maze bounds");
            return false;
        }

        // Check if current position is on the solution path
        bool onSolutionPath = pathToGoal[currentGridPos.x, currentGridPos.y];
        //Debug.Log($"Current position on solution path: {onSolutionPath}");

        if (onSolutionPath)
        {
            //Debug.Log("On solution path - giving green regardless of direction");
            return true;
        }

        // If not on solution path, use the smart direction checking
        return IsFacingValidDirection(currentGridPos);
    }

    bool IsFacingValidDirection(Vector2Int gridPos)
    {
        Vector3 playerForward = playerCamera.transform.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        Vector2Int playerGridDirection = WorldDirectionToGrid(playerForward);

        //Debug.Log($"Player facing world direction: {playerForward}");
        //Debug.Log($"Player facing grid direction: {playerGridDirection}");

        Vector2Int targetCell = gridPos + playerGridDirection;

        if (targetCell.x < 0 || targetCell.x >= mazeRows ||
            targetCell.y < 0 || targetCell.y >= mazeCols)
        {
            //Debug.Log("Facing towards maze boundary - red");
            return false;
        }

        if (!CanMoveBetweenCells(gridPos, targetCell))
        {
            //Debug.Log("Facing towards wall - red");
            return false;
        }

        // PRIORITY 1: If facing towards the solution path, always green
        if (pathToGoal[targetCell.x, targetCell.y])
        {
            //Debug.Log("Facing towards solution path cell - green");
            return true;
        }

        // PRIORITY 2: If facing towards a dead end, always red
        if (IsDeadEndDirection(gridPos, playerGridDirection))
        {
            //Debug.Log("Facing towards dead end - red");
            return false;
        }

        // PRIORITY 3: Check if the direction leads towards the solution path eventually
        if (DirectionLeadsToSolutionPath(targetCell, gridPos))
        {
            //Debug.Log("Direction leads towards solution path - green");
            return true;
        }

        // PRIORITY 4: If in a forced corridor (only 2 directions), be forgiving
        if (IsInForcedCorridor(gridPos))
        {
            //Debug.Log("In forced corridor, not facing dead end - green");
            return true;
        }

        //Debug.Log("Direction doesn't lead to solution path - red");
        return false;
    }

    Vector2Int WorldDirectionToGrid(Vector3 worldDirection)
    {
        Vector3 absDir = new Vector3(Mathf.Abs(worldDirection.x), 0, Mathf.Abs(worldDirection.z));

        if (absDir.z > absDir.x)
        {
            return worldDirection.z > 0 ? new Vector2Int(1, 0) : new Vector2Int(-1, 0);
        }
        else
        {
            return worldDirection.x > 0 ? new Vector2Int(0, 1) : new Vector2Int(0, -1);
        }
    }

    bool DirectionLeadsToSolutionPath(Vector2Int startCell, Vector2Int fromCell)
    {
        // Use a simple breadth-first search to see if we can reach the solution path
        // from this direction within a reasonable distance
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<(Vector2Int pos, int distance)>();

        queue.Enqueue((startCell, 0));
        visited.Add(startCell);

        Vector2Int[] directions = {
            new Vector2Int(1, 0), new Vector2Int(0, 1),
            new Vector2Int(-1, 0), new Vector2Int(0, -1)
        };

        int maxSearchDistance = 5; // Don't search too far

        while (queue.Count > 0)
        {
            var (currentPos, distance) = queue.Dequeue();

            // If we reached the solution path, success!
            if (pathToGoal[currentPos.x, currentPos.y])
            {
                //Debug.Log($"Found solution path at {currentPos} after {distance} steps");
                return true;
            }

            // Don't search too far
            if (distance >= maxSearchDistance)
                continue;

            // Explore neighbors
            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighbor = currentPos + direction;

                // Check bounds
                if (neighbor.x < 0 || neighbor.x >= mazeRows ||
                    neighbor.y < 0 || neighbor.y >= mazeCols)
                    continue;

                // Don't go back to where we came from immediately
                if (neighbor == fromCell && distance == 0)
                    continue;

                // Skip if already visited
                if (visited.Contains(neighbor))
                    continue;

                // Check if we can move there
                if (!CanMoveBetweenCells(currentPos, neighbor))
                    continue;

                visited.Add(neighbor);
                queue.Enqueue((neighbor, distance + 1));
            }
        }

        //Debug.Log($"No path to solution found from {startCell}");
        return false;
    }

    bool IsDeadEndDirection(Vector2Int currentPos, Vector2Int direction)
    {
        Vector2Int targetCell = currentPos + direction;

        if (targetCell.x < 0 || targetCell.x >= mazeRows ||
            targetCell.y < 0 || targetCell.y >= mazeCols)
        {
            return true;
        }

        var mazeGenerator = GetMazeGenerator();
        if (mazeGenerator == null) return true;

        try
        {
            var cell = mazeGenerator.GetMazeCell(targetCell.x, targetCell.y);

            int openings = 0;
            if (!cell.WallFront) openings++;
            if (!cell.WallRight) openings++;
            if (!cell.WallBack) openings++;
            if (!cell.WallLeft) openings++;

            //Debug.Log($"Target cell ({targetCell.x},{targetCell.y}) has {openings} openings");

            return openings <= 1;
        }
        catch
        {
            return true;
        }
    }

    bool IsInForcedCorridor(Vector2Int gridPos)
    {
        var mazeGenerator = GetMazeGenerator();
        if (mazeGenerator == null) return false;

        try
        {
            var cell = mazeGenerator.GetMazeCell(gridPos.x, gridPos.y);

            int openDirections = 0;
            if (!cell.WallFront) openDirections++;
            if (!cell.WallRight) openDirections++;
            if (!cell.WallBack) openDirections++;
            if (!cell.WallLeft) openDirections++;

            //Debug.Log($"Current cell has {openDirections} open directions");
            return openDirections == 2;
        }
        catch (System.Exception e)
        {
            //Debug.LogError($"Error checking corridor at ({gridPos.x},{gridPos.y}): {e.Message}");
            return false;
        }
    }

    // IMPROVED coordinate conversion - let's double-check this
    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        // Be very explicit about the conversion
        float gridX_float = worldPos.z / cellSize;
        float gridY_float = worldPos.x / cellSize;

        int gridX = Mathf.RoundToInt(gridX_float);
        int gridY = Mathf.RoundToInt(gridY_float);

        //Debug.Log($"World pos {worldPos} -> floats ({gridX_float:F2}, {gridY_float:F2}) -> grid ({gridX}, {gridY})");

        gridX = Mathf.Clamp(gridX, 0, mazeRows - 1);
        gridY = Mathf.Clamp(gridY, 0, mazeCols - 1);

        return new Vector2Int(gridX, gridY);
    }

    Vector3 GridToWorldDirection(Vector2Int gridDirection)
    {
        return new Vector3(gridDirection.y, 0, gridDirection.x);
    }

    IEnumerator CreateWaveSequence(Color waveColor)
    {
        for (int i = 0; i < 3; i++)
        {
            CreateSoundWave(waveColor);
            yield return new WaitForSeconds(waveSpacing);
        }

        yield return new WaitForSeconds(waveDuration);
        isEcholocating = false;
    }

    void CreateSoundWave(Color waveColor)
    {
        GameObject waveObject = new GameObject("EcholocationWave");
        waveObject.transform.position = transform.position + Vector3.up * 1.0f; // Higher up near whale's mouth

        EcholocationWave wave = waveObject.AddComponent<EcholocationWave>();
        wave.Initialize(waveStartSize, waveMaxSize, waveSpeed, waveDuration, waveColor);
    }

    public void SetGoal(Transform goal)
    {
        //Debug.Log($"✓ SimpleEcholocation goal set to: {goal.name} at position {goal.position}");
        if (pathToGoal == null)
        {
            StartCoroutine(CalculatePathAfterGeneration());
        }
    }

    void OnDrawGizmos()
    {
        if (pathToGoal == null) return;

        // Draw the solution path
        Gizmos.color = Color.yellow;
        for (int i = 0; i < mazeRows; i++)
        {
            for (int j = 0; j < mazeCols; j++)
            {
                if (pathToGoal[i, j])
                {
                    Vector3 worldPos = new Vector3(j * cellSize, 0.5f, i * cellSize);
                    Gizmos.DrawCube(worldPos, Vector3.one * 0.5f);

                    Vector2Int dir = nextDirection[i, j];
                    if (dir != Vector2Int.zero)
                    {
                        Vector3 dirWorld = GridToWorldDirection(dir);
                        Gizmos.color = Color.red;
                        Gizmos.DrawRay(worldPos, dirWorld * 2f);
                        Gizmos.color = Color.yellow;
                    }
                }
            }
        }

        // Draw player's current grid position
        Vector2Int playerGrid = WorldToGrid(transform.position);
        if (playerGrid.x >= 0 && playerGrid.x < mazeRows && playerGrid.y >= 0 && playerGrid.y < mazeCols)
        {
            Vector3 playerGridWorld = new Vector3(playerGrid.y * cellSize, 1f, playerGrid.x * cellSize);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(playerGridWorld, Vector3.one * 2f);
        }

        // Draw start and goal positions
        Vector3 startWorldPos = new Vector3(actualStartPos.y * cellSize, 2f, actualStartPos.x * cellSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startWorldPos, 1f);

        Vector3 goalWorldPos = new Vector3(goalPos.y * cellSize, 2f, goalPos.x * cellSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(goalWorldPos, 1f);
    }
}