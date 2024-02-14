/*
Pseudo Random Number Generators and Voronoi Noise function created with the help of these tutorials: 
https://www.ronja-tutorials.com/post/024-white-noise/
https://www.ronja-tutorials.com/post/028-voronoi-noise/
Voronoi noise is a method of partitioning space into a number of distinct regions. A voronoi noise diagram is constructed by starting from a set of points over some 
surface, known as seeds. For each point in space (or every pixel on our suface), the voronoi diagram identifies which seed it is closest to.
This process divides the surface into voronoi cells, where every location/pixel within a cell is closest to the seed that "owns" that cell than any other.
The borders of cells are equidistant between the two nearest seeds. The width of these borders can be controlled.
Voronoi noise is often used to create organic and natural textures, and I felt it could be leveraged to generate the patterns viewed across the surface of a star.
*/

#include "Assets/ActualProject/Helpers/Utility.hlsl"

float3 voronoiNoise(float2 uv, float _BorderWidth)
{
    float2 baseCell = floor(uv);

    //first pass to find the closest cell
    float2 closestCell;
    float2 toClosestCell;
    float minDistToCell = 10.0; //initialise to a value greater than the distance between any 2 cells in the 3x3 neighbourhood

    /*We examine the 3x3 neighbourhood of cells around our current cell. Note that we needn't implement any logic for neighbours that are out of bounds.
    For instance, saw we are currently at cell (-1, -1) and are looking at its bottom left neighbour. This would technically be at location (-2, -2). Though
    this cell does not actually exist within the boundaries of our texture, it is still a perfectly valid cell location for the distance calculations that follow.*/
    for(int i = -1; i <= 1; i++)
    {
        for(int j = -1; j <= 1; j++)
        {
            float2 cell = baseCell + float2(i, j); //centre of the current neighbouring cell
            float2 cellPos = cell + Hash22(cell); //actual position of the cell we're interested in - some random offset from the cell's centre
            float2 toCell = cellPos - uv; //vector from our current uv coordinate to the current cell's position.
            float dist = length(toCell);
            if(dist < minDistToCell)
            {
                minDistToCell = dist; //update min distance
                closestCell = cell; //set the cell that our current uv coordinate is closest to 
                toClosestCell = toCell; //set the vector connecting our current uv coordinate to the closest cell
            }
        }
    }

    //We carry out a second pass over the 3x3 neighbourhood of cells to compute the distance to the borders.
    /*Iterate through the neighbours and calculate the distance to the border by calculating the midpoint connecting the 
    nearest cell*/
    float minEdgeDist = 10.0;
    for(int i = -1; i <= 1; i++)
    {
        for(int j = -1; j <= 1; j++)
        {

            //As before, calculate the vector from our current uv coordinate to the current cell's position.
            float2 cell = baseCell + float2(i, j);  
            float2 cellPos = cell + Hash22(cell);
            float2 toCell = cellPos - uv; 

            //check if the current cell IS the closest cell 
            float2 diffToClosestCell = abs(closestCell - cell); 
            bool isClosest = diffToClosestCell.x + diffToClosestCell.y < 0.1; //check if < 0.1 instead of checking for equality between the two cells to account for floating poitn precision issues
            if(!isClosest)
            {
                float2 toCentre = (toClosestCell + toCell) * 0.5; //vector from the current uv coordinate to the midpoint of the line connecting our two cells.
                float2 cellDiff = normalize(toCell - toClosestCell); //unit vector from the closest cell to the current neighbouring cell
                float edgeDist = dot(toCentre, cellDiff); //by projecting toCentre onto the direction of cellDiff, we get the distance from the current uv position to the edge
                minEdgeDist = min(minEdgeDist, edgeDist); //update the minimum edge distance
            }
        }
    }
    float random = Hash21(closestCell);
    return float3(minDistToCell, random, minEdgeDist/_BorderWidth);
}

float3 voronoiNoise3D(float3 value, float _BorderWidth)
{
    float3 baseCell = floor(value);

    //first pass to find the closest cell
    float3 closestCell;
    float3 toClosestCell;
    float minDistToCell = 10.0; //initialise to a value greater than the distance between any 2 cells in the 3x3 neighbourhood

    /*We examine the 3x3 neighbourhood of cells around our current cell. Note that we needn't implement any logic for neighbours that are out of bounds.
    For instance, saw we are currently at cell (-1, -1) and are looking at its bottom left neighbour. This would technically be at location (-2, -2). Though
    this cell does not actually exist within the boundaries of our texture, it is still a perfectly valid cell location for the distance calculations that follow.*/
    for(int i = -1; i <= 1; i++)
    {
        for(int j = -1; j <= 1; j++)
        {
            for(int k = -1; k <= 1; k++)
            {
                float3 cell = baseCell + float3(i, j, k); //centre of the current neighbouring cell
                float3 cellPos = cell + Hash33(cell); //actual position of the cell we're interested in - some random offset from the cell's centre
                float3 toCell = cellPos - value; //vector from our current uv coordinate to the current cell's position.
                float dist = length(toCell);
                if(dist < minDistToCell)
                {
                    minDistToCell = dist; //update min distance
                    closestCell = cell; //set the cell that our current uv coordinate is closest to 
                    toClosestCell = toCell; //set the vector connecting our current uv coordinate to the closest cell
                }
            }

        }
    }

    //We carry out a second pass over the 3x3 neighbourhood of cells to compute the distance to the borders.
    /*Iterate through the neighbours and calculate the distance to the border by calculating the midpoint connecting the 
    nearest cell*/
    float minEdgeDist = 10.0;
    for(int i = -1; i <= 1; i++)
    {
        for(int j = -1; j <= 1; j++)
        {
            for(int k = -1; k <= 1; k++)
            {
                                        //As before, calculate the vector from our current uv coordinate to the current cell's position.
                float3 cell = baseCell + float3(i, j, k);  
                float3 cellPos = cell + Hash33(cell);
                float3 toCell = cellPos - value; 

                //check if the current cell IS the closest cell 
                float3 diffToClosestCell = abs(closestCell - cell); 
                bool isClosest = diffToClosestCell.x + diffToClosestCell.y < 0.1; //check if < 0.1 instead of checking for equality between the two cells to account for floating poitn precision issues
                if(!isClosest)
                {
                    float3 toCentre = (toClosestCell + toCell) * 0.5; //vector from the current uv coordinate to the midpoint of the line connecting our two cells.
                    float3 cellDiff = normalize(toCell - toClosestCell); //unit vector from the closest cell to the current neighbouring cell
                    float edgeDist = dot(toCentre, cellDiff); //by projecting toCentre onto the direction of cellDiff, we get the distance from the current uv position to the edge
                    minEdgeDist = min(minEdgeDist, edgeDist); //update the minimum edge distance
                }
            }

        }
    }
    float random = Hash31(closestCell);
    return float3(minDistToCell, random, minEdgeDist/_BorderWidth);
}