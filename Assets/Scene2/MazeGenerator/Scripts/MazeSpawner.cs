using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//<summary>
//Game object, that creates maze and instantiates it in scene
//</summary>
public class MazeSpawner : MonoBehaviour
{
    public enum MazeGenerationAlgorithm
    {
        PureRecursive,
    }
    public MazeGenerationAlgorithm Algorithm = MazeGenerationAlgorithm.PureRecursive;
    public bool FullRandom = false;
    public int RandomSeed = 12345;
    public GameObject Floor = null;
    public GameObject Wall = null;
    //public GameObject Pillar = null;
    public int Rows = 5;
    public int Columns = 5;
    public float CellWidth = 5;
    public float CellHeight = 5;
    public bool AddGaps = true;
    public GameObject GoalPrefab = null;

    [Header("Geyser Settings")]
    public GameObject GeyserPrefab = null;
    [Range(0f, 1f)]
    public float GeyserSpawnChance = 0.3f; // 30% chance to spawn geyser in each door opening
    public float GeyserHeight = 0.5f; // Height above ground to spawn geysers

    [Header("Rock Obstacle Settings")]
    public GameObject RockPrefab = null;
    [Range(0f, 1f)]
    public float RockSpawnChance = 0.4f; // 40% chance to spawn rock in valid alcoves
    public float RockHeight = 0f; // Height above ground to spawn rocks

    private BasicMazeGenerator mMazeGenerator = null;
    private List<Vector2Int> occupiedPositions = new List<Vector2Int>(); // Track occupied positions

    void Start()
    {
        if (!FullRandom)
        {
            Random.seed = RandomSeed;
        }
        switch (Algorithm)
        {
            case MazeGenerationAlgorithm.PureRecursive:
                mMazeGenerator = new RecursiveMazeGenerator(Rows, Columns);
                break;

        }
        mMazeGenerator.GenerateMaze();

        // First pass: spawn floors and walls
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                float x = column * (CellWidth + (AddGaps ? .2f : 0));
                float z = row * (CellHeight + (AddGaps ? .2f : 0));
                MazeCell cell = mMazeGenerator.GetMazeCell(row, column);
                GameObject tmp;

                // Spawn floor
                tmp = Instantiate(Floor, new Vector3(x, 0, z), Quaternion.Euler(0, 0, 0)) as GameObject;
                tmp.transform.parent = transform;

                // Spawn walls
                if (cell.WallRight)
                {
                    tmp = Instantiate(Wall, new Vector3(x + CellWidth / 2, 0, z) + Wall.transform.position, Quaternion.Euler(0, 90, 0)) as GameObject;// right
                    tmp.transform.parent = transform;
                }
                if (cell.WallFront)
                {
                    tmp = Instantiate(Wall, new Vector3(x, 0, z + CellHeight / 2) + Wall.transform.position, Quaternion.Euler(0, 0, 0)) as GameObject;// front
                    tmp.transform.parent = transform;
                }
                if (cell.WallLeft)
                {
                    tmp = Instantiate(Wall, new Vector3(x - CellWidth / 2, 0, z) + Wall.transform.position, Quaternion.Euler(0, 270, 0)) as GameObject;// left
                    tmp.transform.parent = transform;
                }
                if (cell.WallBack)
                {
                    tmp = Instantiate(Wall, new Vector3(x, 0, z - CellHeight / 2) + Wall.transform.position, Quaternion.Euler(0, 180, 0)) as GameObject;// back
                    tmp.transform.parent = transform;
                }

                // Spawn goal
                if (cell.IsGoal && GoalPrefab != null)
                {
                    tmp = Instantiate(GoalPrefab, new Vector3(x, 1, z), Quaternion.Euler(0, 0, 0)) as GameObject;
                    tmp.transform.parent = transform;
                }
            }
        }

        // Second pass: spawn rocks in alcoves (priority)
        if (RockPrefab != null)
        {
            SpawnRocksInAlcoves();
        }

        // Third pass: spawn geysers in door openings (avoid rock positions)
        if (GeyserPrefab != null)
        {
            SpawnGeysersInDoorways();
        }
    }

    void SpawnGeysersInDoorways()
    {
        // Define start and end room boundaries
        int startRoomSize = 3;
        int endRoomStartRow = Rows - startRoomSize;
        int endRoomStartCol = Columns - startRoomSize;

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                // Skip if we're in the start room (bottom-left 3x3)
                if (row < startRoomSize && column < startRoomSize)
                {
                    continue;
                }

                // Skip if we're in the end room (top-right 3x3)
                if (row >= endRoomStartRow && column >= endRoomStartCol)
                {
                    continue;
                }

                float x = column * (CellWidth + (AddGaps ? .2f : 0));
                float z = row * (CellHeight + (AddGaps ? .2f : 0));
                MazeCell cell = mMazeGenerator.GetMazeCell(row, column);

                // Check for door openings (missing walls) and spawn geysers

                // Right opening (door to the right)
                if (!cell.WallRight && column + 1 < Columns)
                {
                    // Don't spawn if the target position would be in a room
                    Vector3 targetPos = new Vector3(x + CellWidth / 2, GeyserHeight, z);
                    Vector2Int geyserGridPos = new Vector2Int(row, column); // This represents the doorway position

                    if (!IsPositionInRoom(targetPos, row, column + 0.5f, startRoomSize, endRoomStartRow, endRoomStartCol) &&
                       !IsPositionOccupied(geyserGridPos) &&
                       !IsNearStartRoomConnection(row, column))
                    {
                        if (Random.Range(0f, 1f) < GeyserSpawnChance)
                        {
                            GameObject geyser = Instantiate(GeyserPrefab, targetPos, Quaternion.identity) as GameObject;
                            geyser.transform.parent = transform;
                            occupiedPositions.Add(geyserGridPos);
                        }
                    }
                }

                // Front opening (door to the front)
                if (!cell.WallFront && row + 1 < Rows)
                {
                    // Don't spawn if the target position would be in a room
                    Vector3 targetPos = new Vector3(x, GeyserHeight, z + CellHeight / 2);
                    Vector2Int geyserGridPos = new Vector2Int(row, column);

                    if (!IsPositionInRoom(targetPos, row + 0.5f, column, startRoomSize, endRoomStartRow, endRoomStartCol) &&
                       !IsPositionOccupied(geyserGridPos) &&
                       !IsNearStartRoomConnection(row, column))
                    {
                        if (Random.Range(0f, 1f) < GeyserSpawnChance)
                        {
                            GameObject geyser = Instantiate(GeyserPrefab, targetPos, Quaternion.identity) as GameObject;
                            geyser.transform.parent = transform;
                            occupiedPositions.Add(geyserGridPos);
                        }
                    }
                }

                // Left opening (door to the left) - only check if we're not at the leftmost edge
                if (!cell.WallLeft && column > 0)
                {
                    // Only spawn if the left neighbor also doesn't have a right wall (to avoid duplicates)
                    MazeCell leftNeighbor = mMazeGenerator.GetMazeCell(row, column - 1);
                    if (!leftNeighbor.WallRight)
                    {
                        Vector3 targetPos = new Vector3(x - CellWidth / 2, GeyserHeight, z);
                        Vector2Int geyserGridPos = new Vector2Int(row, column - 1);

                        if (!IsPositionInRoom(targetPos, row, column - 0.5f, startRoomSize, endRoomStartRow, endRoomStartCol) &&
                           !IsPositionOccupied(geyserGridPos) &&
                           !IsNearStartRoomConnection(row, column))
                        {
                            if (Random.Range(0f, 1f) < GeyserSpawnChance)
                            {
                                GameObject geyser = Instantiate(GeyserPrefab, targetPos, Quaternion.identity) as GameObject;
                                geyser.transform.parent = transform;
                                occupiedPositions.Add(geyserGridPos);
                            }
                        }
                    }
                }

                // Back opening (door to the back) - only check if we're not at the back edge
                if (!cell.WallBack && row > 0)
                {
                    // Only spawn if the back neighbor also doesn't have a front wall (to avoid duplicates)
                    MazeCell backNeighbor = mMazeGenerator.GetMazeCell(row - 1, column);
                    if (!backNeighbor.WallFront)
                    {
                        Vector3 targetPos = new Vector3(x, GeyserHeight, z - CellHeight / 2);
                        Vector2Int geyserGridPos = new Vector2Int(row - 1, column);

                        if (!IsPositionInRoom(targetPos, row - 0.5f, column, startRoomSize, endRoomStartRow, endRoomStartCol) &&
                           !IsPositionOccupied(geyserGridPos) &&
                           !IsNearStartRoomConnection(row, column))
                        {
                            if (Random.Range(0f, 1f) < GeyserSpawnChance)
                            {
                                GameObject geyser = Instantiate(GeyserPrefab, targetPos, Quaternion.identity) as GameObject;
                                geyser.transform.parent = transform;
                                occupiedPositions.Add(geyserGridPos);
                            }
                        }
                    }
                }
            }
        }
    }

    bool IsPositionInRoom(Vector3 position, float row, float column, int startRoomSize, int endRoomStartRow, int endRoomStartCol)
    {
        // Check if position is in start room (bottom-left 3x3)
        if (row < startRoomSize && column < startRoomSize)
        {
            return true;
        }

        // Check if position is in end room (top-right 3x3)
        if (row >= endRoomStartRow && column >= endRoomStartCol)
        {
            return true;
        }

        return false;
    }

    void SpawnRocksInAlcoves()
    {
        // Define start and end room boundaries
        int startRoomSize = 3;
        int endRoomStartRow = Rows - startRoomSize;
        int endRoomStartCol = Columns - startRoomSize;

        // Create list of all potential positions and shuffle them
        List<Vector2Int> potentialPositions = new List<Vector2Int>();

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                // Skip if we're in the start room (bottom-left 3x3)
                if (row < startRoomSize && column < startRoomSize)
                {
                    continue;
                }

                // Skip if we're in the end room (top-right 3x3)
                if (row >= endRoomStartRow && column >= endRoomStartCol)
                {
                    continue;
                }

                MazeCell cell = mMazeGenerator.GetMazeCell(row, column);

                // Check if this cell can have a rock placed in it
                Vector2Int rockSpawnPos = GetRockSpawnPosition(row, column, cell);
                if (rockSpawnPos.x != -1 && rockSpawnPos.y != -1)
                {
                    potentialPositions.Add(rockSpawnPos);
                }
            }
        }

        // Shuffle the list to randomize spawn order
        for (int i = 0; i < potentialPositions.Count; i++)
        {
            Vector2Int temp = potentialPositions[i];
            int randomIndex = Random.Range(i, potentialPositions.Count);
            potentialPositions[i] = potentialPositions[randomIndex];
            potentialPositions[randomIndex] = temp;
        }

        // Now try to spawn rocks from the shuffled list
        int rocksSpawned = 0;
        int maxRocks = Mathf.Max(1, (Rows * Columns) / 50); // Limit based on maze size

        foreach (Vector2Int rockSpawnPos in potentialPositions)
        {
            // Stop if we've spawned enough rocks
            if (rocksSpawned >= maxRocks)
            {
                break;
            }

            // Check if position is too close to existing rocks
            if (IsTooCloseToExistingRock(rockSpawnPos))
            {
                continue;
            }

            if (Random.Range(0f, 1f) < RockSpawnChance)
            {
                float x = rockSpawnPos.y * (CellWidth + (AddGaps ? .2f : 0));
                float z = rockSpawnPos.x * (CellHeight + (AddGaps ? .2f : 0));
                Vector3 rockPos = new Vector3(x, RockHeight, z);
                GameObject rock = Instantiate(RockPrefab, rockPos, Quaternion.identity) as GameObject;
                rock.transform.parent = transform;

                // Mark this position as occupied
                occupiedPositions.Add(rockSpawnPos);
                rocksSpawned++;
            }
        }
    }

    bool IsTooCloseToExistingRock(Vector2Int newRockPos)
    {
        int minDistance = 2; // Reduced minimum distance between rocks (was 3)

        foreach (Vector2Int existingRock in occupiedPositions)
        {
            // Calculate Manhattan distance (|x1-x2| + |y1-y2|)
            int distance = Mathf.Abs(newRockPos.x - existingRock.x) + Mathf.Abs(newRockPos.y - existingRock.y);
            if (distance < minDistance)
            {
                return true; // Too close to existing rock
            }
        }

        return false; // Not too close to any existing rock
    }

    Vector2Int GetRockSpawnPosition(int row, int column, MazeCell cell)
    {
        // Simplified approach: Look for any position where:
        // 1. Rock can be pushed forward (into an open space or dead end)
        // 2. Player can access the rock from behind
        // 3. Don't worry about complex puzzle logic - just make it pushable

        // Check if player can push rock forward (north)
        if (CanPushRockForward(row, column))
        {
            return new Vector2Int(row, column);
        }

        // Check if player can push rock backward (south) 
        if (CanPushRockBackward(row, column))
        {
            return new Vector2Int(row, column);
        }

        // Check if player can push rock right (east)
        if (CanPushRockRight(row, column))
        {
            return new Vector2Int(row, column);
        }

        // Check if player can push rock left (west)
        if (CanPushRockLeft(row, column))
        {
            return new Vector2Int(row, column);
        }

        return new Vector2Int(-1, -1); // No valid position found
    }

    bool CanPushRockForward(int row, int column)
    {
        // Player pushes rock north (forward)
        // Need: player behind rock (south), rock can move forward (north)
        if (row <= 0 || row >= Rows - 1) return false;

        try
        {
            MazeCell rockCell = mMazeGenerator.GetMazeCell(row, column);
            MazeCell playerCell = mMazeGenerator.GetMazeCell(row + 1, column); // Behind rock (south)
            MazeCell targetCell = mMazeGenerator.GetMazeCell(row - 1, column); // Target position (north)

            // Rock should be accessible from player side and pushable to target
            // Target should be accessible (not a wall-surrounded cell)
            return !rockCell.WallFront &&  // Player can reach rock from south
                   !rockCell.WallBack &&   // Rock can move to target north
                   !playerCell.WallBack &&  // Player has access to reach rock
                   IsTargetCellAccessible(row - 1, column); // Target has some connectivity
        }
        catch
        {
            return false;
        }
    }

    bool CanPushRockBackward(int row, int column)
    {
        // Player pushes rock south (backward)
        // Need: player behind rock (north), rock can move backward (south)
        if (row <= 0 || row >= Rows - 1) return false;

        try
        {
            MazeCell rockCell = mMazeGenerator.GetMazeCell(row, column);
            MazeCell playerCell = mMazeGenerator.GetMazeCell(row - 1, column); // Behind rock (north)
            MazeCell targetCell = mMazeGenerator.GetMazeCell(row + 1, column); // Target position (south)

            // Rock should be accessible from player side and pushable to target
            return !rockCell.WallBack &&   // Player can reach rock from north
                   !rockCell.WallFront &&  // Rock can move to target south
                   !playerCell.WallFront && // Player has access to reach rock
                   IsTargetCellAccessible(row + 1, column); // Target has some connectivity
        }
        catch
        {
            return false;
        }
    }

    bool CanPushRockRight(int row, int column)
    {
        // Player pushes rock east (right)
        // Need: player behind rock (west), rock can move right (east)
        if (column <= 0 || column >= Columns - 1) return false;

        try
        {
            MazeCell rockCell = mMazeGenerator.GetMazeCell(row, column);
            MazeCell playerCell = mMazeGenerator.GetMazeCell(row, column - 1); // Behind rock (west)
            MazeCell targetCell = mMazeGenerator.GetMazeCell(row, column + 1); // Target position (east)

            // Rock should be accessible from player side and pushable to target
            return !rockCell.WallLeft &&   // Player can reach rock from west
                   !rockCell.WallRight &&  // Rock can move to target east
                   !playerCell.WallRight && // Player has access to reach rock
                   IsTargetCellAccessible(row, column + 1); // Target has some connectivity
        }
        catch
        {
            return false;
        }
    }

    bool CanPushRockLeft(int row, int column)
    {
        // Player pushes rock west (left)
        // Need: player behind rock (east), rock can move left (west)
        if (column <= 0 || column >= Columns - 1) return false;

        try
        {
            MazeCell rockCell = mMazeGenerator.GetMazeCell(row, column);
            MazeCell playerCell = mMazeGenerator.GetMazeCell(row, column + 1); // Behind rock (east)
            MazeCell targetCell = mMazeGenerator.GetMazeCell(row, column - 1); // Target position (west)

            // Rock should be accessible from player side and pushable to target
            return !rockCell.WallRight &&  // Player can reach rock from east
                   !rockCell.WallLeft &&   // Rock can move to target west
                   !playerCell.WallLeft &&  // Player has access to reach rock
                   IsTargetCellAccessible(row, column - 1); // Target has some connectivity
        }
        catch
        {
            return false;
        }
    }

    bool IsTargetCellAccessible(int row, int column)
    {
        // Check if the target cell has at least one open connection (not completely walled in)
        // This prevents rocks from being pushed into dead corners
        if (row < 0 || row >= Rows || column < 0 || column >= Columns) return false;

        try
        {
            MazeCell targetCell = mMazeGenerator.GetMazeCell(row, column);

            // Target should have at least one open side (not completely surrounded by walls)
            int openSides = 0;
            if (!targetCell.WallFront) openSides++;
            if (!targetCell.WallBack) openSides++;
            if (!targetCell.WallLeft) openSides++;
            if (!targetCell.WallRight) openSides++;

            // Need at least 2 open sides so rock doesn't get stuck in a corner
            return openSides >= 2;
        }
        catch
        {
            return false;
        }
    }

    bool IsHorizontalPattern1(int row, int column)
    {
        // Pattern: [#][#][#][#]
        //          [#][A][R][P] <- current row
        //          [#][#][X][#]
        if (row < 1 || row >= Rows - 1 || column < 2 || column >= Columns - 1) return false;

        try
        {
            MazeCell rockCell = mMazeGenerator.GetMazeCell(row, column);     // R position
            MazeCell alcoveCell = mMazeGenerator.GetMazeCell(row, column - 1); // A position  
            MazeCell playerCell = mMazeGenerator.GetMazeCell(row, column + 1); // P position
            MazeCell blockedCell = mMazeGenerator.GetMazeCell(row + 1, column); // X position

            // Rock should be accessible from player side, blocked from alcove side
            // Alcove should be a dead end (3 walls)
            // Blocked area should have limited access (rock is blocking the path)
            return !rockCell.WallRight && // Rock accessible from player
                   rockCell.WallLeft &&   // Rock blocks alcove access initially
                   !playerCell.WallLeft && // Player can reach rock
                   alcoveCell.WallLeft && alcoveCell.WallBack && alcoveCell.WallFront && // Alcove has 3 walls
                   blockedCell.WallBack; // X area is currently blocked
        }
        catch
        {
            return false;
        }
    }

    bool IsHorizontalPattern2(int row, int column)
    {
        // Pattern: [#][#][X][#] 
        //          [#][A][R][P] <- current row
        //          [#][#][#][#]
        if (row < 1 || row >= Rows - 1 || column < 2 || column >= Columns - 1) return false;

        try
        {
            MazeCell rockCell = mMazeGenerator.GetMazeCell(row, column);     // R position
            MazeCell alcoveCell = mMazeGenerator.GetMazeCell(row, column - 1); // A position
            MazeCell playerCell = mMazeGenerator.GetMazeCell(row, column + 1); // P position  
            MazeCell blockedCell = mMazeGenerator.GetMazeCell(row - 1, column); // X position

            return !rockCell.WallRight && // Rock accessible from player
                   rockCell.WallLeft &&   // Rock blocks alcove access initially
                   !playerCell.WallLeft && // Player can reach rock
                   alcoveCell.WallLeft && alcoveCell.WallBack && alcoveCell.WallFront && // Alcove has 3 walls
                   blockedCell.WallFront; // X area is currently blocked
        }
        catch
        {
            return false;
        }
    }

    bool IsHorizontalPattern3(int row, int column)
    {
        // Pattern: [#][#][#][#]
        //          [P][R][A][#] <- current row
        //          [#][X][#][#]
        if (row < 1 || row >= Rows - 1 || column < 1 || column >= Columns - 2) return false;

        try
        {
            MazeCell rockCell = mMazeGenerator.GetMazeCell(row, column);     // R position
            MazeCell alcoveCell = mMazeGenerator.GetMazeCell(row, column + 1); // A position
            MazeCell playerCell = mMazeGenerator.GetMazeCell(row, column - 1); // P position
            MazeCell blockedCell = mMazeGenerator.GetMazeCell(row + 1, column); // X position

            return !rockCell.WallLeft &&  // Rock accessible from player
                   rockCell.WallRight &&  // Rock blocks alcove access initially
                   !playerCell.WallRight && // Player can reach rock
                   alcoveCell.WallRight && alcoveCell.WallBack && alcoveCell.WallFront && // Alcove has 3 walls
                   blockedCell.WallBack; // X area is currently blocked
        }
        catch
        {
            return false;
        }
    }

    bool IsHorizontalPattern4(int row, int column)
    {
        // Pattern: [#][X][#][#]
        //          [P][R][A][#] <- current row
        //          [#][#][#][#]
        if (row < 1 || row >= Rows - 1 || column < 1 || column >= Columns - 2) return false;

        try
        {
            MazeCell rockCell = mMazeGenerator.GetMazeCell(row, column);     // R position
            MazeCell alcoveCell = mMazeGenerator.GetMazeCell(row, column + 1); // A position
            MazeCell playerCell = mMazeGenerator.GetMazeCell(row, column - 1); // P position
            MazeCell blockedCell = mMazeGenerator.GetMazeCell(row - 1, column); // X position

            return !rockCell.WallLeft &&  // Rock accessible from player
                   rockCell.WallRight &&  // Rock blocks alcove access initially
                   !playerCell.WallRight && // Player can reach rock
                   alcoveCell.WallRight && alcoveCell.WallBack && alcoveCell.WallFront && // Alcove has 3 walls
                   blockedCell.WallFront; // X area is currently blocked
        }
        catch
        {
            return false;
        }
    }

    bool IsVerticalPattern1(int row, int column)
    {
        // Pattern: [#][#][#]
        //          [#][A][#]
        //          [#][R][X] <- current row (rock position), X to the right
        //          [#][P][#]
        // IMPORTANT: X should ONLY be accessible through R, not directly from P

        if (row < 2 || row >= Rows - 1 || column < 1 || column >= Columns - 1) return false;

        try
        {
            MazeCell rockCell = mMazeGenerator.GetMazeCell(row, column);     // R position
            MazeCell alcoveCell = mMazeGenerator.GetMazeCell(row - 1, column); // A position (above rock)
            MazeCell playerCell = mMazeGenerator.GetMazeCell(row + 1, column); // P position (below rock)
            MazeCell xCell = mMazeGenerator.GetMazeCell(row, column + 1); // X position (right of rock)

            // Debug logging
            Debug.Log($"Checking Vertical Pattern 1 at ({row},{column}):");
            Debug.Log($"Rock: Front={rockCell.WallFront}, Back={rockCell.WallBack}, Left={rockCell.WallLeft}, Right={rockCell.WallRight}");
            Debug.Log($"Alcove: Front={alcoveCell.WallFront}, Back={alcoveCell.WallBack}, Left={alcoveCell.WallLeft}, Right={alcoveCell.WallRight}");
            Debug.Log($"Player: Front={playerCell.WallFront}, Back={playerCell.WallBack}, Left={playerCell.WallLeft}, Right={playerCell.WallRight}");
            Debug.Log($"X: Front={xCell.WallFront}, Back={xCell.WallBack}, Left={xCell.WallLeft}, Right={xCell.WallRight}");

            // Key validation: Player should NOT be able to reach X directly
            // Check if player can reach X without going through rock
            bool playerCanReachXDirectly = !playerCell.WallRight; // Player can go right to X

            if (playerCanReachXDirectly)
            {
                Debug.Log("REJECTED: Player can reach X directly without pushing rock");
                return false;
            }

            // For this pattern:
            // - Rock should be open to player (below) and alcove (above)
            // - Rock should block access to X (wall to the right)
            // - Alcove should be a dead end (3 walls) but open to rock
            // - Player should be accessible from somewhere
            // - X should have limited access (rock is blocking the main path)
            // - Player should NOT have direct access to X

            bool rockAccessibleFromPlayer = !rockCell.WallFront; // Player is below rock
            bool rockCanEnterAlcove = !rockCell.WallBack; // Alcove is above rock
            bool rockBlocksX = rockCell.WallRight; // X is to the right of rock
            bool alcoveIsDeadEnd = alcoveCell.WallLeft && alcoveCell.WallRight && alcoveCell.WallBack;
            bool alcoveOpenToRock = !alcoveCell.WallFront; // Rock is below alcove
            bool playerAccessible = !playerCell.WallBack; // Player can reach rock
            bool xHasLimitedAccess = xCell.WallLeft; // X is blocked from rock side

            Debug.Log($"Conditions: rockAccessible={rockAccessibleFromPlayer}, canEnterAlcove={rockCanEnterAlcove}, blocksX={rockBlocksX}, alcoveDeadEnd={alcoveIsDeadEnd}, alcoveOpen={alcoveOpenToRock}, playerAccess={playerAccessible}, xLimited={xHasLimitedAccess}");

            bool isValid = rockAccessibleFromPlayer && rockCanEnterAlcove && rockBlocksX &&
                          alcoveIsDeadEnd && alcoveOpenToRock && playerAccessible && xHasLimitedAccess;

            if (isValid)
            {
                Debug.Log("ACCEPTED: Valid rock puzzle pattern found!");
            }

            return isValid;
        }
        catch
        {
            return false;
        }
    }

    bool IsVerticalPattern2(int row, int column)
    {
        // Pattern: [#][#][#]
        //          [#][A][#]
        //          [#][R][X] <- current row
        //          [#][P][#]
        if (row < 2 || row >= Rows - 1 || column < 1 || column >= Columns - 1) return false;

        try
        {
            MazeCell rockCell = mMazeGenerator.GetMazeCell(row, column);     // R position
            MazeCell alcoveCell = mMazeGenerator.GetMazeCell(row - 1, column); // A position
            MazeCell playerCell = mMazeGenerator.GetMazeCell(row + 1, column); // P position
            MazeCell blockedCell = mMazeGenerator.GetMazeCell(row, column + 1); // X position

            return !rockCell.WallFront && // Rock accessible from player
                   rockCell.WallBack &&  // Rock blocks alcove access initially
                   !playerCell.WallBack && // Player can reach rock
                   alcoveCell.WallLeft && alcoveCell.WallBack && alcoveCell.WallRight && // Alcove has 3 walls
                   blockedCell.WallLeft; // X area is currently blocked
        }
        catch
        {
            return false;
        }
    }

    bool IsVerticalPattern3(int row, int column)
    {
        // Pattern: [#][P][#]
        //          [X][R][#] <- current row
        //          [#][A][#]
        //          [#][#][#]
        if (row < 1 || row >= Rows - 2 || column < 1 || column >= Columns - 1) return false;

        try
        {
            MazeCell rockCell = mMazeGenerator.GetMazeCell(row, column);     // R position
            MazeCell alcoveCell = mMazeGenerator.GetMazeCell(row + 1, column); // A position
            MazeCell playerCell = mMazeGenerator.GetMazeCell(row - 1, column); // P position
            MazeCell blockedCell = mMazeGenerator.GetMazeCell(row, column - 1); // X position

            return !rockCell.WallBack &&  // Rock accessible from player
                   rockCell.WallFront &&  // Rock blocks alcove access initially
                   !playerCell.WallFront && // Player can reach rock
                   alcoveCell.WallLeft && alcoveCell.WallFront && alcoveCell.WallRight && // Alcove has 3 walls
                   blockedCell.WallRight; // X area is currently blocked
        }
        catch
        {
            return false;
        }
    }

    bool IsVerticalPattern4(int row, int column)
    {
        // Pattern: [#][P][#]
        //          [#][R][X] <- current row
        //          [#][A][#]
        //          [#][#][#]
        if (row < 1 || row >= Rows - 2 || column < 1 || column >= Columns - 1) return false;

        try
        {
            MazeCell rockCell = mMazeGenerator.GetMazeCell(row, column);     // R position
            MazeCell alcoveCell = mMazeGenerator.GetMazeCell(row + 1, column); // A position
            MazeCell playerCell = mMazeGenerator.GetMazeCell(row - 1, column); // P position
            MazeCell blockedCell = mMazeGenerator.GetMazeCell(row, column + 1); // X position

            return !rockCell.WallBack &&  // Rock accessible from player
                   rockCell.WallFront &&  // Rock blocks alcove access initially
                   !playerCell.WallFront && // Player can reach rock
                   alcoveCell.WallLeft && alcoveCell.WallFront && alcoveCell.WallRight && // Alcove has 3 walls
                   blockedCell.WallLeft; // X area is currently blocked
        }
        catch
        {
            return false;
        }
    }

    bool IsPositionOccupied(Vector2Int gridPosition)
    {
        return occupiedPositions.Contains(gridPosition);
    }

    bool IsNearStartRoomConnection(int row, int column)
    {
        // The start room connects to the maze at specific cells
        // Based on ConnectRoomsToMaze(): cells (1,2) and (1,3) have their walls removed
        // We need to avoid spawning geysers near this connection

        // Check if we're at or adjacent to the start room connection points
        // Connection is at row 1, between columns 2 and 3
        if ((row == 1 && (column == 2 || column == 3)) || // Exact connection cells
           (row == 0 && (column == 2 || column == 3)) || // Above connection
           (row == 2 && (column == 2 || column == 3)) || // Below connection
           (row == 1 && (column == 1 || column == 4)))
        { // Left/right of connection
            return true;
        }

        return false;
    }
}