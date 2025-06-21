using System.Collections.Generic;

namespace MineLauncher.BlockBench;

public class CubeFaceGroup
{
    public CubeFace North { get; set; }
    public CubeFace Front => North;

    public CubeFace South { get; set; }
    public CubeFace Back => South;

    public CubeFace West { get; set; }
    public CubeFace Right => West;

    public CubeFace East { get; set; }
    public CubeFace Left => East;

    public CubeFace Up { get; set; }

    public CubeFace Down { get; set; }

    public IEnumerable<CubeFace> AllFaces()
    {
        yield return Front;
        yield return Back;
        yield return Right;
        yield return Left;
        yield return Up;
        yield return Down;
    }
}