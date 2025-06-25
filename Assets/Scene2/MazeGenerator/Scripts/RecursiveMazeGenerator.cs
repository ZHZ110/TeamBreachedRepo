using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//<summary>
//Pure recursive maze generation.
//Use carefully for large mazes.
//</summary>
public class RecursiveMazeGenerator : BasicMazeGenerator
{
    public RecursiveMazeGenerator(int rows, int columns) : base(rows, columns)
    {

    }

    public override void GenerateMaze()
    {
        // Create start and end rooms first
        CreateStartRoom(3, 3); // 3x3 room at start
        CreateEndRoom(3, 3);   // 3x3 room at end

        // Generate maze starting from edge of start room
        VisitCell(1, 4, Direction.Start); // Start from right edge of start room

        // Connect rooms to maze
        ConnectRoomsToMaze();

        // Ensure all perimeter walls are created (no openings)
        CreatePerimeterWalls();
    }

    private void ConnectRoomsToMaze()
    {
        // Connect start room to maze - remove wall between room and maze
        GetMazeCell(1, 2).WallRight = false; // Remove right wall of start room
        GetMazeCell(1, 3).WallLeft = false; // Remove left wall of maze cell

        // Connect end room to maze - remove wall between maze and room  
        int endRoomStartRow = RowCount - 3;
        int endRoomStartCol = ColumnCount - 3;

        GetMazeCell(endRoomStartRow + 1, endRoomStartCol - 1).WallRight = false; // Remove right wall of maze cell
        GetMazeCell(endRoomStartRow + 1, endRoomStartCol).WallLeft = false; // Remove left wall of end room
    }

    private void CreateStartRoom(int roomWidth, int roomHeight)
    {
        // Create an open 3x3 room in the bottom-left corner
        for (int row = 0; row < roomHeight && row < RowCount; row++)
        {
            for (int col = 0; col < roomWidth && col < ColumnCount; col++)
            {
                MazeCell cell = GetMazeCell(row, col);
                cell.IsVisited = true; // Mark as visited so maze generation skips it

                // Remove internal walls within the room
                if (col < roomWidth - 1)
                {
                    cell.WallRight = false; // Remove right wall between room cells
                }
                if (row < roomHeight - 1)
                {
                    cell.WallFront = false; // Remove front wall between room cells
                }
                if (col > 0)
                {
                    cell.WallLeft = false; // Remove left wall between room cells
                }
                if (row > 0)
                {
                    cell.WallBack = false; // Remove back wall between room cells
                }
            }
        }
    }

    private void CreateEndRoom(int roomWidth, int roomHeight)
    {
        // Create an open 3x3 room in the top-right corner
        int startRow = RowCount - roomHeight;
        int startCol = ColumnCount - roomWidth;

        for (int row = startRow; row < RowCount; row++)
        {
            for (int col = startCol; col < ColumnCount; col++)
            {
                MazeCell cell = GetMazeCell(row, col);
                cell.IsVisited = true; // Mark as visited so maze generation skips it

                // Remove internal walls within the room
                if (col < ColumnCount - 1)
                {
                    cell.WallRight = false; // Remove right wall between room cells
                }
                if (row < RowCount - 1)
                {
                    cell.WallFront = false; // Remove front wall between room cells
                }
                if (col > startCol)
                {
                    cell.WallLeft = false; // Remove left wall between room cells
                }
                if (row > startRow)
                {
                    cell.WallBack = false; // Remove back wall between room cells
                }
            }
        }

        // Set goal in center of end room
        GetMazeCell(startRow + 1, startCol + 1).IsGoal = true; // Center of 3x3 room
    }

    private void CreatePerimeterWalls()
    {
        // Force all perimeter walls to exist - no openings in the outer boundary

        // Top and bottom edges
        for (int col = 0; col < ColumnCount; col++)
        {
            GetMazeCell(0, col).WallBack = true;               // Top edge
            GetMazeCell(RowCount - 1, col).WallFront = true;   // Bottom edge
        }

        // Left and right edges  
        for (int row = 0; row < RowCount; row++)
        {
            GetMazeCell(row, 0).WallLeft = true;               // Left edge
            GetMazeCell(row, ColumnCount - 1).WallRight = true; // Right edge
        }
    }

    private void VisitCell(int row, int column, Direction moveMade)
    {
        Direction[] movesAvailable = new Direction[4];
        int movesAvailableCount = 0;

        do
        {
            movesAvailableCount = 0;

            //check move right
            if (column + 1 < ColumnCount && !GetMazeCell(row, column + 1).IsVisited)
            {
                movesAvailable[movesAvailableCount] = Direction.Right;
                movesAvailableCount++;
            }
            else if (!GetMazeCell(row, column).IsVisited && moveMade != Direction.Left)
            {
                GetMazeCell(row, column).WallRight = true;
            }
            //check move forward
            if (row + 1 < RowCount && !GetMazeCell(row + 1, column).IsVisited)
            {
                movesAvailable[movesAvailableCount] = Direction.Front;
                movesAvailableCount++;
            }
            else if (!GetMazeCell(row, column).IsVisited && moveMade != Direction.Back)
            {
                GetMazeCell(row, column).WallFront = true;
            }
            //check move left
            if (column > 0 && column - 1 >= 0 && !GetMazeCell(row, column - 1).IsVisited)
            {
                movesAvailable[movesAvailableCount] = Direction.Left;
                movesAvailableCount++;
            }
            else if (!GetMazeCell(row, column).IsVisited && moveMade != Direction.Right)
            {
                GetMazeCell(row, column).WallLeft = true;
            }
            //check move backward
            if (row > 0 && row - 1 >= 0 && !GetMazeCell(row - 1, column).IsVisited)
            {
                movesAvailable[movesAvailableCount] = Direction.Back;
                movesAvailableCount++;
            }
            else if (!GetMazeCell(row, column).IsVisited && moveMade != Direction.Front)
            {
                GetMazeCell(row, column).WallBack = true;
            }

            if (movesAvailableCount == 0 && !GetMazeCell(row, column).IsVisited)
            {
                GetMazeCell(row, column).IsGoal = true;
            }

            GetMazeCell(row, column).IsVisited = true;

            if (movesAvailableCount > 0)
            {
                switch (movesAvailable[Random.Range(0, movesAvailableCount)])
                {
                    case Direction.Start:
                        break;
                    case Direction.Right:
                        VisitCell(row, column + 1, Direction.Right);
                        break;
                    case Direction.Front:
                        VisitCell(row + 1, column, Direction.Front);
                        break;
                    case Direction.Left:
                        VisitCell(row, column - 1, Direction.Left);
                        break;
                    case Direction.Back:
                        VisitCell(row - 1, column, Direction.Back);
                        break;
                }
            }

        } while (movesAvailableCount > 0);
    }
}